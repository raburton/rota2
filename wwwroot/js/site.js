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
