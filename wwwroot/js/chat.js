(() => {
    const form = document.getElementById('questionForm'), input = document.getElementById('questionText'), photo = document.getElementById('cropPhoto');
    const status = document.getElementById('chatStatus'), send = document.getElementById('sendButton');
    let fromVoice = false;
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;
    photo.addEventListener('change', () => { document.getElementById('photoLabel').textContent = photo.files[0]?.name || ''; });
    document.querySelectorAll('[data-question]').forEach(button => button.addEventListener('click', () => { input.value = button.dataset.question; input.focus(); }));
    form.addEventListener('submit', async event => {
        event.preventDefault(); if (send.disabled) return;
        if (!input.value.trim() && !photo.files.length) { status.textContent = 'Add a question or photo first.'; input.focus(); return; }
        if (photo.files[0]?.size > 5 * 1024 * 1024) { status.textContent = 'Choose a photo under 5 MB.'; return; }
        send.disabled = true; status.textContent = 'Preparing your question for review…';
        try {
            let url = '/Chat/Ask', body, headers = { RequestVerificationToken: token() };
            if (photo.files.length) { url = '/Chat/UploadImage'; body = new FormData(); body.append('image', photo.files[0]); body.append('userPrompt', input.value); }
            else if (fromVoice) { url = '/Chat/UploadVoice'; body = new FormData(); body.append('transcribedText', input.value); }
            else { body = JSON.stringify({ queryText: input.value }); headers['Content-Type'] = 'application/json'; }
            const response = await fetch(url, { method: 'POST', headers, body });
            if (response.redirected) { window.location.assign('/Account/Login'); return; }
            if (response.status === 429) throw new Error('Please wait a minute before sending another question.');
            if (response.status === 400) { const error = await response.json().catch(() => null); throw new Error(error?.message || 'Your session expired or the request is invalid. Refresh and try again.'); }
            if (!response.ok) throw new Error('The request could not be completed. Your text is still here; please try again.');
            const result = await response.json(); if (!result.success) throw new Error(result.message || 'Please try again.');
            window.location.reload();
        } catch (error) { status.textContent = error.message; } finally { send.disabled = false; }
    });
    const voice = document.getElementById('voiceButton'), Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!Recognition) { voice.disabled = true; voice.title = 'Voice input is not supported in this browser. You can type instead.'; }
    else {
        const recognition = new Recognition(); recognition.lang = document.documentElement.dataset.advisoryLanguage === 'Hindi' ? 'hi-IN' : document.documentElement.dataset.advisoryLanguage === 'English' ? 'en-IN' : 'gu-IN';
        recognition.onresult = event => { input.value = event.results[0][0].transcript.slice(0, 4000); fromVoice = true; status.textContent = 'Check the transcript, then send for review.'; };
        recognition.onerror = () => { status.textContent = 'Voice input was unavailable. Check microphone access or type your question.'; };
        recognition.onend = () => { voice.disabled = false; voice.textContent = 'Voice input'; };
        voice.addEventListener('click', () => { try { recognition.start(); voice.disabled = true; voice.textContent = 'Listening…'; } catch { status.textContent = 'Voice input is already active.'; } });
    }
    document.querySelectorAll('[data-rating]').forEach(button => button.addEventListener('click', async () => {
        const row = button.closest('[data-query]'), body = new FormData(); body.append('queryId', row.dataset.query); body.append('rating', button.dataset.rating);
        try { const response = await fetch('/Farmer/SubmitFeedback', { method: 'POST', headers: { RequestVerificationToken: token() }, body }); if (!response.ok || response.redirected) throw new Error(); row.querySelectorAll('button').forEach(b => b.textContent = +b.dataset.rating <= +button.dataset.rating ? '★' : '☆'); }
        catch { status.textContent = 'Rating could not be saved. Please sign in again or retry.'; }
    }));
})();
