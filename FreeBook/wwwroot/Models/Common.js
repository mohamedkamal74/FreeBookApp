// Shared helpers for the admin views. Loaded from _LayoutAdmin after the
// DataTables plugins and the one-time localized string bundle, so every page
// gets the same defaults without redeclaring anything.
(function () {
    'use strict';

    function strings() {
        return window.bookify || {};
    }

    /**
     * Apply localized DataTables strings to the global defaults. Falls back to
     * English if a key is missing. Called once when this file loads.
     */
    function initDataTablesLanguage() {
        if (!window.jQuery || !jQuery.fn.dataTable) { return; }

        const src = strings().dt || {};
        const paginate = src.paginate || {};
        const aria = src.aria || {};
        const dt = {
            search:       src.search       || 'Search:',
            lengthMenu:   src.lengthMenu   || 'Show _MENU_ entries',
            info:         src.info         || 'Showing _START_ to _END_ of _TOTAL_ entries',
            infoEmpty:    src.infoEmpty    || 'Showing 0 to 0 of 0 entries',
            infoFiltered: src.infoFiltered || '(filtered from _MAX_ total entries)',
            zeroRecords:  src.zeroRecords  || 'No matching records found',
            emptyTable:   src.emptyTable   || 'No data available in table',
            loadingRecords: src.loadingRecords || 'Loading...',
            processing:   src.processing   || 'Processing...',
            paginate: {
                first:    paginate.first    || 'First',
                previous: paginate.previous || 'Previous',
                next:     paginate.next     || 'Next',
                last:     paginate.last     || 'Last'
            },
            aria: {
                sortAscending:  aria.sortAscending  || ': activate to sort column ascending',
                sortDescending: aria.sortDescending || ': activate to sort column descending'
            }
        };

        jQuery.extend(true, jQuery.fn.dataTable.defaults, { language: dt });
    }

    function clearValidation(modal) {
        if (!modal) {
            return;
        }
        modal.querySelectorAll('.field-validation-error').forEach(el => el.textContent = '');
        modal.querySelectorAll('.field-validation-valid').forEach(el => el.textContent = '');
        modal.querySelectorAll('.input-validation-error').forEach(el => el.classList.remove('input-validation-error'));
        modal.querySelectorAll('.validation-summary-errors').forEach(el => el.classList.add('d-none'));
    }

    function confirmDelete() {
        const ui = strings().ui || {};
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
            title: ui.deleteTitle || fallback.title,
            text: ui.deleteText || fallback.text,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: ui.deleteConfirm || fallback.confirm,
            cancelButtonText: ui.deleteCancel || fallback.cancel
        });
    }

    window.initDataTablesLanguage = initDataTablesLanguage;
    window.clearValidation = clearValidation;
    window.confirmDelete = confirmDelete;

    initDataTablesLanguage();
})();
