// Keep messages visible until the user has had time to read and act on them.
document.addEventListener('DOMContentLoaded', () => {
    const sidebar=document.getElementById('sidebar'), toggle=document.getElementById('sidebarToggle');
    if (sidebar && toggle) {
        const mobile = window.matchMedia('(max-width:768px)');
        let collapsed = false;
        try { collapsed = localStorage.getItem('dko.navigationCollapsed') === 'true'; } catch {}
        function render() {
            const visible = mobile.matches ? sidebar.classList.contains('open') : !collapsed;
            document.body.classList.toggle('navigation-collapsed', !mobile.matches && collapsed);
            sidebar.inert = !visible;
            toggle.setAttribute('aria-expanded', String(visible));
            toggle.setAttribute('aria-label', window.dkoText(visible ? 'Hide navigation' : 'Show navigation'));
            toggle.title = window.dkoText(visible ? 'Hide navigation' : 'Show navigation');
        }
        function closeMobile() {
            if (!mobile.matches || !sidebar.classList.contains('open')) return;
            const restoreFocus = sidebar.contains(document.activeElement);
            sidebar.classList.remove('open'); render();
            if (restoreFocus) toggle.focus();
        }
        toggle.addEventListener('click', () => {
            if (mobile.matches) sidebar.classList.toggle('open');
            else { collapsed = !collapsed; try { localStorage.setItem('dko.navigationCollapsed', String(collapsed)); } catch {} }
            render();
        });
        document.addEventListener('keydown', e => { if (e.key === 'Escape') closeMobile(); });
        document.addEventListener('click', e => { if (!sidebar.contains(e.target) && !toggle.contains(e.target)) closeMobile(); });
        mobile.addEventListener('change', () => { sidebar.classList.remove('open'); render(); });
        render();
    }
    document.querySelectorAll('.sidebar-nav a').forEach(link=>{ if(link.pathname===location.pathname){link.classList.add('active');link.setAttribute('aria-current','page');} });
    document.querySelectorAll('input:not([type="hidden"]),select,textarea').forEach(input=>{ const label=input.closest('.form-group')?.querySelector('label'); if(label && input.id && !label.htmlFor) label.htmlFor=input.id; });
});
function getAntiForgeryToken(){return document.querySelector('input[name="__RequestVerificationToken"]')?.value||'';}
window.addEventListener('click',e=>{document.querySelectorAll('[id$="Modal"]').forEach(modal=>{if(e.target===modal)modal.style.display='none';});});
