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

window.showModalElement = (el) => {
    if (!el) return;
    if (!el._bsModal) el._bsModal = new bootstrap.Modal(el);
    el._bsModal.show();
};

window.hideModalElement = (el) => {
    if (!el) return;
    if (el._bsModal) el._bsModal.hide();
};
