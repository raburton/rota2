window.showModal = (id) => {
    const el = document.getElementById(id);
    if (!el) return;
    if (!el._bsModal) el._bsModal = new bootstrap.Modal(el);
    el._bsModal.show();
};

window.hideModal = (id) => {
    const el = document.getElementById(id);
    if (!el) return;
    if (el._bsModal) el._bsModal.hide();
};

window.showModalElement = (el, options) => {
    if (!el) return;
    // If options provided, pass them to the Modal constructor
    if (!el._bsModal) el._bsModal = options ? new bootstrap.Modal(el, options) : new bootstrap.Modal(el);
    el._bsModal.show();
};

window.hideModalElement = (el) => {
    if (!el) return;
    if (el._bsModal) el._bsModal.hide();
};

window.initTooltipById = (id) => {
    const el = document.getElementById(id);
    if (!el) return;
    if (!el._bsTooltip) el._bsTooltip = new bootstrap.Tooltip(el);
};

window.initTooltips = () => {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        if (!tooltipTriggerEl._bsTooltip) new bootstrap.Tooltip(tooltipTriggerEl)
    })
};

window.setCookie = (name, value, days) => {
    let expires = "";
    if (days) {
        const date = new Date();
        date.setTime(date.getTime() + (days*24*60*60*1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/";
};

window.deleteCookie = (name) => {
    document.cookie = name + "=; Max-Age=0; path=/";
};

window.postJsonWithCredentials = async (url, payload) => {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(payload)
    });
    if (!res.ok) {
        return { ok: false, status: res.status, body: null };
    }
    const body = await res.text();
    return { ok: true, status: res.status, body: body };
};

window.postEmptyWithCredentials = async (url) => {
    const res = await fetch(url, {
        method: 'POST',
        credentials: 'include'
    });
    return { ok: res.ok, status: res.status };
};
