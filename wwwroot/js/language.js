(() => {
    'use strict';
    const catalog = JSON.parse(document.getElementById('ui-translations')?.textContent || '{}');
    const lookup = Object.fromEntries(Object.entries(catalog).map(([key,value]) => [key.toLowerCase(),value]));
    window.dkoText = text => lookup[String(text).toLowerCase()] || text;
})();
