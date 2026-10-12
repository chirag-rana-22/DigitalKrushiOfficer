(() => {
    const workspace = document.getElementById('chatWorkspace');
    if (!workspace) return;
    const form = document.getElementById('questionForm'), input = document.getElementById('questionText');
    const picker = document.getElementById('cropPhoto'), previews = document.getElementById('photoPreviews');
    const status = document.getElementById('chatStatus'), send = document.getElementById('sendButton');
    const messages = document.getElementById('messages'), list = document.getElementById('conversationList');
    const jump = document.getElementById('jumpLatest'), older = document.getElementById('loadOlder');
    let conversationId = Number(workspace.dataset.conversationId) || null, files = [], pending = false, fromVoice = false;
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;
    const element = (tag, className, text) => { const node = document.createElement(tag); if (className) node.className = className; if (text != null) node.textContent = text; return node; };
    function resize() { input.style.height = 'auto'; input.style.height = Math.min(180, input.scrollHeight) + 'px'; }
    function latest(smooth = false) {
        const last = messages.querySelector('.thread-turn:last-child');
        if (last) messages.scrollTo({ top: messages.scrollTop + last.getBoundingClientRect().top - messages.getBoundingClientRect().top - 12, behavior: smooth ? 'smooth' : 'auto' });
        jump.hidden = true;
    }
    jump.addEventListener('click', () => latest(true));
    messages.addEventListener('scroll', () => { jump.hidden = messages.scrollHeight - messages.scrollTop - messages.clientHeight < 150; });
    input.addEventListener('input', () => { fromVoice = false; resize(); });
    input.addEventListener('keydown', event => { if (event.key === 'Enter' && !event.shiftKey && !event.isComposing && !pending) { event.preventDefault(); form.requestSubmit(); } });
    const historyOpen = open => { workspace.classList.toggle('history-open', open); document.getElementById('historyToggle').setAttribute('aria-expanded', String(open)); };
    document.getElementById('historyToggle').addEventListener('click', () => historyOpen(!workspace.classList.contains('history-open')));
    ['closeHistory', 'historyBackdrop'].forEach(id => document.getElementById(id).addEventListener('click', () => historyOpen(false)));
    document.addEventListener('keydown', event => { if (event.key === 'Escape') historyOpen(false); });
    workspace.addEventListener('click', event => { if (pending && event.target.closest('#conversationList a, #newChatLink')) { event.preventDefault(); status.textContent = 'Please wait for this message to finish before switching chats.'; } });
    let searchTimer, historyRequest;
    async function refreshHistory() {
        historyRequest?.abort(); historyRequest = new AbortController();
        try {
            const response = await fetch('/Chat/Conversations?search=' + encodeURIComponent(document.getElementById('historySearch').value), { signal: historyRequest.signal });
            if (!response.ok || response.redirected) throw new Error();
            const chats = await response.json(); list.replaceChildren();
            chats.forEach(chat => { const a = element('a', chat.id === conversationId ? 'selected' : ''); a.href = '/Chat/Index?conversationId=' + chat.id; a.dataset.conversation = chat.id; if (chat.id === conversationId) a.setAttribute('aria-current', 'page'); a.append(element('span', '', chat.title), element('small', '', new Date(chat.updatedAt).toLocaleDateString(undefined, { day: 'numeric', month: 'short' }))); list.append(a); });
            if (!chats.length) list.append(element('p', 'history-empty', 'No conversations found. Start a new chat.'));
        } catch (error) { if (error.name !== 'AbortError') status.textContent = 'Chat history could not refresh. Your saved messages are still available after reloading.'; }
    }
    document.getElementById('historySearch').addEventListener('input', () => { clearTimeout(searchTimer); searchTimer = setTimeout(refreshHistory, 250); });
    function renderPreviews() {
        previews.replaceChildren();
        files.forEach((item, index) => { const figure = element('figure', 'photo-preview'); const image = element('img'); image.src = item.url; image.alt = item.file.name; const remove = element('button', '', '×'); remove.type = 'button'; remove.setAttribute('aria-label', 'Remove photo ' + (index + 1)); remove.disabled = pending; remove.addEventListener('click', () => { URL.revokeObjectURL(item.url); files.splice(index, 1); renderPreviews(); }); figure.append(image, remove); previews.append(figure); });
        document.getElementById('photoLabel').textContent = files.length ? files.length + ' / 20 photos' : '';
    }
    function addFiles(incoming) {
        if (pending) return;
        const selected = Array.from(incoming);
        if (selected.some(file => file.size === 0 || file.size > 5 * 1024 * 1024)) { status.textContent = 'Each photo must be non-empty and no larger than 5 MB.'; return; }
        if (selected.some(file => !['image/jpeg', 'image/png', 'image/webp'].includes(file.type))) { status.textContent = 'Choose JPG, PNG or WebP photos.'; return; }
        if (files.length + selected.length > 20 || files.reduce((sum, item) => sum + item.file.size, 0) + selected.reduce((sum, file) => sum + file.size, 0) > 25 * 1024 * 1024) { status.textContent = 'Attach up to 20 photos and 25 MB per message. You can send more in your next message.'; return; }
        files.push(...selected.map(file => ({ file, url: URL.createObjectURL(file) }))); status.textContent = ''; renderPreviews();
    }
    document.getElementById('attachPhotos').addEventListener('click', () => { if (!pending) picker.click(); });
    picker.addEventListener('change', () => { addFiles(picker.files); picker.value = ''; });
    form.addEventListener('paste', event => { const images = Array.from(event.clipboardData?.items || []).filter(item => item.kind === 'file' && item.type.startsWith('image/')).map(item => item.getAsFile()).filter(Boolean); if (images.length) { event.preventDefault(); addFiles(images); } });
    form.addEventListener('dragover', event => { event.preventDefault(); form.classList.add('dragging'); });
    form.addEventListener('dragleave', event => { if (!form.contains(event.relatedTarget)) form.classList.remove('dragging'); });
    form.addEventListener('drop', event => { event.preventDefault(); form.classList.remove('dragging'); addFiles(event.dataTransfer.files); });
    function messageNode(message) {
        const article = element('article', 'thread-turn'); if (message.queryId) article.dataset.queryId = message.queryId;
        const question = element('div', 'user-message'); question.append(element('span', 'message-author', 'You'), element('p', '', message.queryText));
        if (message.imageUrls?.length) { const grid = element('div', 'message-photos'); message.imageUrls.forEach(url => { const link = element('a'); link.href = url; link.target = '_blank'; link.rel = 'noopener'; const img = element('img'); img.src = url; img.alt = 'Attached crop or soil photo'; link.append(img); grid.append(link); }); question.append(grid); }
        const assistant = element('div', 'assistant-message'), content = element('div', 'assistant-content'), heading = element('div', 'assistant-heading');
        heading.append(element('strong', '', 'Krushi AI'), element('span', 'answer-tag', message.pending ? 'Thinking…' : message.status === 'RetryNeeded' ? 'Temporarily unavailable' : message.status === 'AwaitingDetails' ? 'A little more detail' : message.approvalStatus === 'AIAnswered' ? 'AI answer' : message.status === 'Answered' ? 'Officer reviewed' : message.status === 'PendingApproval' ? 'Sent to officer' : message.status));
        const fallback = message.status === 'Rejected' ? 'The officer could not solve this query and has closed it as rejected. Read their reason below; you can send a follow-up with more details.' : message.status === 'NeedsRevision' ? 'The draft needs revision. Your officer is preparing a corrected answer.' : message.status === 'Unassigned' ? 'AI could not solve this reliably. No active verified officer is available; your question is saved in the assignment queue.' : 'This question needs expert help and has been sent to a Krushi Officer.';
        content.append(heading, element('div', 'answer-text' + (message.pending ? ' thinking' : ''), message.pending ? 'Looking into your question…' : message.aiAnswer || (message.publicSummary ? message.publicSummary + "\n\n" : "") + (message.notice || fallback)));
        if (message.officerReply && message.officerReply !== message.aiAnswer) content.append(element('p', 'message-author', 'Officer reply'), element('div', 'answer-text', message.officerReply));
        if (message.status === 'Answered') { const rating = element('div', 'rating'); rating.dataset.query = message.queryId; rating.append(element('span', '', 'Was this helpful?')); for (let i = 1; i <= 5; i++) { const button = element('button', '', (message.feedbackRating || 0) >= i ? '★' : '☆'); button.type = 'button'; button.dataset.rating = i; button.setAttribute('aria-label', 'Rate ' + i + ' out of 5'); rating.append(button); } content.append(rating); }
        assistant.append(element('span', 'assistant-avatar', '✳'), content); article.append(question, assistant); return article;
    }
    form.addEventListener('submit', async event => {
        event.preventDefault(); if (pending) return;
        const question = input.value.trim();
        if (!question && !files.length) { status.textContent = 'Add a question or photos first.'; input.focus(); return; }
        const shownQuestion = question || 'Please assess these ' + files.length + ' crop/soil photos together.';
        document.getElementById('chatWelcome')?.remove();
        const optimistic = messageNode({ queryText: shownQuestion, imageUrls: files.map(item => item.url), pending: true }); messages.append(optimistic); latest();
        pending = true; send.disabled = true; input.disabled = true; renderPreviews(); status.textContent = '';
        try {
            let url = '/Chat/Ask', body, headers = { RequestVerificationToken: token() };
            if (files.length) { url = '/Chat/UploadImage'; body = new FormData(); files.forEach(item => body.append('images', item.file)); body.append('userPrompt', question); if (conversationId) body.append('conversationId', conversationId); }
            else if (fromVoice) { url = '/Chat/UploadVoice'; body = new FormData(); body.append('transcribedText', question); if (conversationId) body.append('conversationId', conversationId); }
            else { body = JSON.stringify({ queryText: question, conversationId }); headers['Content-Type'] = 'application/json'; }
            const response = await fetch(url, { method: 'POST', headers, body });
            if (response.redirected || response.status === 401) throw new Error('Your session expired. Sign in again, then retry your message.');
            if (response.status === 429) throw new Error('Please wait a minute before sending another question.');
            if (response.status === 413) throw new Error('These photos are too large. Keep the total below 25 MB.');
            if (!response.ok) { const error = await response.json().catch(() => null); throw new Error(error?.message || 'Your message was not completed. Your text and photos are kept here so you can retry.'); }
            const result = await response.json(); if (!result.success || !result.entry) throw new Error('The response could not be read. Check your history before retrying.');
            optimistic.replaceWith(messageNode({ ...result.entry, notice: result.message })); conversationId = result.conversationId; workspace.dataset.conversationId = conversationId;
            window.history.replaceState(null, '', '/Chat/Index?conversationId=' + conversationId);
            files.forEach(item => URL.revokeObjectURL(item.url)); files = []; input.value = ''; fromVoice = false; resize(); renderPreviews(); requestAnimationFrame(() => latest());
            await refreshHistory();
        } catch (error) { optimistic.remove(); status.textContent = error.message || 'Connection failed. Please retry.'; }
        finally { pending = false; send.disabled = false; input.disabled = false; renderPreviews(); input.focus({ preventScroll: true }); }
    });
    older.addEventListener('click', async () => {
        const first = messages.querySelector('[data-query-id]'); if (!first || !conversationId) return;
        older.disabled = true;
        try { const response = await fetch('/Chat/Messages?conversationId=' + conversationId + '&beforeId=' + first.dataset.queryId); if (!response.ok || response.redirected) throw new Error(); const result = await response.json(); const height = messages.scrollHeight; result.messages.forEach(message => messages.insertBefore(messageNode(message), first)); older.hidden = !result.hasOlder; messages.scrollTop += messages.scrollHeight - height; }
        catch { status.textContent = 'Earlier messages could not load. Please try again.'; } finally { older.disabled = false; }
    });
    messages.addEventListener('click', async event => {
        const button = event.target.closest('[data-rating]'); if (!button) return;
        const row = button.closest('[data-query]'); if (row.dataset.saving) return; row.dataset.saving = 'true';
        const body = new FormData(); body.append('queryId', row.dataset.query); body.append('rating', button.dataset.rating);
        try { const response = await fetch('/Farmer/SubmitFeedback', { method: 'POST', headers: { RequestVerificationToken: token() }, body }); if (!response.ok || response.redirected) throw new Error(); row.querySelectorAll('button').forEach(b => b.textContent = +b.dataset.rating <= +button.dataset.rating ? '★' : '☆'); }
        catch { status.textContent = 'Rating could not be saved. Please retry.'; } finally { delete row.dataset.saving; }
    });
    document.querySelectorAll('[data-question]').forEach(button => button.addEventListener('click', () => { input.value = button.dataset.question; fromVoice = false; resize(); input.focus(); }));
    const voice = document.getElementById('voiceButton'), Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!Recognition) { voice.disabled = true; voice.title = 'Voice input is not supported in this browser. You can type instead.'; }
    else { const recognition = new Recognition(); recognition.lang = document.documentElement.dataset.advisoryLanguage === 'Hindi' ? 'hi-IN' : document.documentElement.dataset.advisoryLanguage === 'English' ? 'en-IN' : 'gu-IN';
        recognition.onresult = event => { if (pending) return; input.value = event.results[0][0].transcript.slice(0, 4000); fromVoice = true; resize(); status.textContent = 'Check the transcript, then send your question.'; };
        recognition.onerror = () => { status.textContent = 'Voice input was unavailable. Check microphone access or type your question.'; };
        recognition.onend = () => { voice.disabled = false; voice.setAttribute('aria-label', 'Use voice input'); };
        voice.addEventListener('click', () => { if (pending) return; try { recognition.start(); voice.disabled = true; voice.setAttribute('aria-label', 'Listening'); status.textContent = 'Listening…'; } catch { status.textContent = 'Voice input is already active.'; } });
    }
    window.addEventListener('pagehide', () => files.forEach(item => URL.revokeObjectURL(item.url)));
    requestAnimationFrame(() => { resize(); latest(); });
})();
