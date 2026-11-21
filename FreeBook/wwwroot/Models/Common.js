// Shared helpers for the admin views. Loaded from _LayoutAdmin before the
// per-page script sections, so every page can use these functions.

/**
 * Apply localized DataTables strings. Call once per page after the view has
 * declared its dt* variables (see each .cshtml script section).
 * Falls back to English if a key is missing.
 */
function initDataTablesLanguage() {
    if (!window.jQuery || !jQuery.fn.dataTable) { return; }
    const dt = {
        search:      (typeof dtSearch      !== 'undefined') ? dtSearch      : 'Search:',
        lengthMenu:  (typeof dtLengthMenu  !== 'undefined') ? dtLengthMenu  : 'Show _MENU_ entries',
        info:        (typeof dtInfo        !== 'undefined') ? dtInfo        : 'Showing _START_ to _END_ of _TOTAL_ entries',
        infoEmpty:   (typeof dtInfoEmpty   !== 'undefined') ? dtInfoEmpty   : 'Showing 0 to 0 of 0 entries',
        infoFiltered:(typeof dtInfoFiltered!== 'undefined') ? dtInfoFiltered: '(filtered from _MAX_ total entries)',
        zeroRecords: (typeof dtZeroRecords !== 'undefined') ? dtZeroRecords : 'No matching records found',
        emptyTable:  (typeof dtEmptyTable  !== 'undefined') ? dtEmptyTable  : 'No data available in table',
        loadingRecords: (typeof dtLoadingRecords!=='undefined') ? dtLoadingRecords : 'Loading...',
        processing:  (typeof dtProcessing  !== 'undefined') ? dtProcessing  : 'Processing...',
        paginate: {
            first:    (typeof dtPaginateFirst    !== 'undefined') ? dtPaginateFirst    : 'First',
            previous: (typeof dtPaginatePrevious !== 'undefined') ? dtPaginatePrevious : 'Previous',
            next:     (typeof dtPaginateNext     !== 'undefined') ? dtPaginateNext     : 'Next',
            last:     (typeof dtPaginateLast     !== 'undefined') ? dtPaginateLast     : 'Last'
        },
        aria: {
            sortAscending:  (typeof dtAriaSortAscending  !== 'undefined') ? dtAriaSortAscending  : ': activate to sort column ascending',
            sortDescending: (typeof dtAriaSortDescending !== 'undefined') ? dtAriaSortDescending : ': activate to sort column descending'
        }
    };
    jQuery.extend(true, jQuery.fn.dataTable.defaults, { language: dt });
}

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
    // The lb* strings are declared by each view. If a view omits them, use a
    // complete fallback set in the page's language so the dialog is never
    // mixed-language or showing "undefined".
    const arabic = (document.documentElement.lang || '').toLowerCase().startsWith('ar');
    const fallback = arabic
        ? {
            title: 'هل أنت متأكد؟',
            text: 'لن تتمكن من التراجع!',
            confirm: 'نعم، احذفها',
            cancel: 'الغاء'
        }
        : {
            title: 'Are you sure?',
            text: 'You will not be able to undo this!',
            confirm: 'Yes, delete it',
            cancel: 'Cancel'
        };

    return Swal.fire({
        title: (typeof lbTitleDeleteMsg !== 'undefined' && lbTitleDeleteMsg) ? lbTitleDeleteMsg : fallback.title,
        text: (typeof lmTextMsgDelete !== 'undefined' && lmTextMsgDelete) ? lmTextMsgDelete : fallback.text,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: (typeof lbconfirmButtonText !== 'undefined' && lbconfirmButtonText) ? lbconfirmButtonText : fallback.confirm,
        cancelButtonText: (typeof lbCancelButtonText !== 'undefined' && lbCancelButtonText) ? lbCancelButtonText : fallback.cancel
    });
}
