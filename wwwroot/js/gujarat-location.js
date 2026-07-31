/**
 * Gujarat Location Helper
 * Provides District + Village/City dropdown functionality for any form.
 * Usage: include this script, then call:
 *   initGujaratLocation(districtSelectId, villageSelectId, currentDistrict, currentVillage)
 */

(function () {
    const DATA_URL = '/js/gujarat-locations.json';

    let _cache = null;

    async function fetchLocations() {
        if (_cache) return _cache;
        try {
            const res = await fetch(DATA_URL);
            if (!res.ok) throw new Error('Failed to fetch location data');
            _cache = await res.json();
            return _cache;
        } catch (err) {
            console.error('[GujaratLocation] Error loading data:', err);
            return null;
        }
    }

    /**
     * Populate district dropdown from JSON data.
     * @param {string} districtSelectId
     * @param {string} currentValue - pre-selected district (optional)
     */
    async function populateDistricts(districtSelectId, currentValue) {
        const data = await fetchLocations();
        const sel = document.getElementById(districtSelectId);
        if (!sel || !data) return;

        const districts = Object.keys(data).sort();
        sel.innerHTML = '<option value="">-- Select District --</option>';
        districts.forEach(d => {
            const opt = document.createElement('option');
            opt.value = d;
            opt.textContent = d;
            if (d === currentValue) opt.selected = true;
            sel.appendChild(opt);
        });
    }

    /**
     * Populate village/city dropdown based on selected district.
     * @param {string} district - the selected district name
     * @param {string} villageSelectId
     * @param {string} currentValue - pre-selected village (optional)
     */
    async function populateVillages(district, villageSelectId, currentValue) {
        const data = await fetchLocations();
        const sel = document.getElementById(villageSelectId);
        if (!sel || !data) return;

        sel.innerHTML = '<option value="">-- Select Village / City --</option>';
        if (district && data[district]) {
            const villages = data[district].sort();
            villages.forEach(v => {
                const opt = document.createElement('option');
                opt.value = v;
                opt.textContent = v;
                if (v === currentValue) opt.selected = true;
                sel.appendChild(opt);
            });
        } else {
            sel.innerHTML = '<option value="">-- Select District First --</option>';
        }
    }

    /**
     * Main initializer. Wires up both dropdowns with change events.
     * @param {string} districtSelectId
     * @param {string} villageSelectId
     * @param {string} [currentDistrict] - pre-selected value (for edit forms)
     * @param {string} [currentVillage]  - pre-selected value (for edit forms)
     */
    async function initGujaratLocation(districtSelectId, villageSelectId, currentDistrict, currentVillage) {
        await populateDistricts(districtSelectId, currentDistrict);

        const districtSel = document.getElementById(districtSelectId);
        if (!districtSel) return;

        // If a district is pre-selected, populate villages immediately
        if (currentDistrict) {
            await populateVillages(currentDistrict, villageSelectId, currentVillage);
        }

        // Wire change event
        districtSel.addEventListener('change', async function () {
            await populateVillages(this.value, villageSelectId, '');
        });
    }

    // Expose globally
    window.initGujaratLocation = initGujaratLocation;
    window.populateVillages = function (district, villageSelectId, currentValue) {
        return populateVillages(district, villageSelectId, currentValue);
    };
})();
