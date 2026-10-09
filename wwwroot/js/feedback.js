async function submitFeedback(queryId, rating) {
    const row = document.getElementById('fb-' + queryId);
    if (!row || row.dataset.saving === 'true') return;
    let status = row.querySelector('[role="status"]');
    if (!status) {
        status = document.createElement('span');
        status.setAttribute('role', 'status');
        status.style.display = 'block';
        row.appendChild(status);
    }
    const buttons = row.querySelectorAll('button');
    row.dataset.saving = 'true';
    buttons.forEach(button => button.disabled = true);
    status.textContent = 'Saving your rating…';
    try {
        const body = new FormData();
        body.append('queryId', queryId);
        body.append('rating', rating);
        body.append('feedbackType', rating >= 4 ? 'Helpful' : 'NotHelpful');
        const response = await fetch('/Farmer/SubmitFeedback', {
            method: 'POST',
            headers: { RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]')?.value || '' },
            body
        });
        if (response.redirected || response.status === 401 || response.status === 403) {
            throw new Error('Your session has expired. Sign in again to save your rating.');
        }
        if (!response.ok) throw new Error('Your rating could not be saved. Please try again.');
        const result = await response.json();
        if (!result.success) throw new Error('Your rating could not be saved. Please try again.');
        buttons.forEach((button, index) => button.textContent = index < rating ? '⭐' : '☆');
        status.textContent = 'Thank you! Your rating was saved: ' + rating + '/5.';
    } catch (error) {
        status.textContent = error instanceof TypeError || error instanceof SyntaxError
            ? 'Unable to save your rating. Check your connection and try again.' : error.message;
    } finally {
        row.dataset.saving = 'false';
        buttons.forEach(button => button.disabled = false);
    }
}
