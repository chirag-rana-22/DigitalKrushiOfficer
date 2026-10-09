// Keep messages visible until the user has had time to read and act on them.
document.addEventListener('DOMContentLoaded', () => {
    const sidebar=document.getElementById('sidebar'), toggle=document.getElementById('sidebarToggle');
    const close=()=>{ sidebar?.classList.remove('open'); toggle?.setAttribute('aria-expanded','false'); };
    toggle?.addEventListener('click',()=>{ const opened=sidebar.classList.toggle('open'); toggle.setAttribute('aria-expanded',String(opened)); });
    document.addEventListener('keydown',e=>{ if(e.key==='Escape') close(); });
    document.addEventListener('click',e=>{ if(sidebar && !sidebar.contains(e.target) && !toggle?.contains(e.target)) close(); });
    document.querySelectorAll('.sidebar-nav a').forEach(link=>{ if(link.pathname===location.pathname){link.classList.add('active');link.setAttribute('aria-current','page');} });
    document.querySelectorAll('input:not([type="hidden"]),select,textarea').forEach(input=>{ const label=input.closest('.form-group')?.querySelector('label'); if(label && input.id && !label.htmlFor) label.htmlFor=input.id; });
});
function getAntiForgeryToken(){return document.querySelector('input[name="__RequestVerificationToken"]')?.value||'';}
window.addEventListener('click',e=>{document.querySelectorAll('[id$="Modal"]').forEach(modal=>{if(e.target===modal)modal.style.display='none';});});
