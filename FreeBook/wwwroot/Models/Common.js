// Shared helpers for the admin views. Loaded from _LayoutAdmin before the
// per-page script sections, so every page can use these functions.

function clearValidation(modal) {
    if (!modal) {
        return;
    }
    // Clear validation messages
    modal.querySelectorAll('.field-validation-error').forEach(el => el.textContent = '');
    modal.querySelectorAll('.field-validation-valid').forEach(el => el.textContent = '');
    // Remove input error classes
    modal.querySelectorAll('.input-validation-error').forEach(el => el.classList.remove('input-validation-error'));
    // Hide validation summary
    modal.querySelectorAll('.validation-summary-errors').forEach(el => el.classList.add('d-none'));
}

function confirmDelete() {
    // The lb* strings are declared by each view; fall back to Arabic when a page
    // does not declare them so the confirmation never shows "undefined".
    return Swal.fire({
        title: (typeof lbTitleDeleteMsg !== 'undefined') ? lbTitleDeleteMsg : 'هل انت متاكد ؟',
        text: (typeof lmTextMsgDelete !== 'undefined') ? lmTextMsgDelete : 'لن تتمكن من التراجع !',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: (typeof lbconfirmButtonText !== 'undefined') ? lbconfirmButtonText : 'Yes, delete it!',
        cancelButtonText: (typeof lbCancelButtonText !== 'undefined') ? lbCancelButtonText : 'الغاء'
    });
}
