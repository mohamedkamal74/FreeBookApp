$(document).ready(function () {
    $('#tableRentals').DataTable({
        "autoWidth": false,
        "responsive": true,
        // The controller already returns newest first (StartDate descending);
        // without an explicit order DataTables would fall back to sorting the
        // subscriber column, so keep the newest-first view the page had.
        "order": [[1, 'desc']],
        // The action column holds POST forms (return, extend, pay penalty).
        // Sorting it is meaningless and Responsive must not collapse it into
        // the child row, where the form controls would be duplicated.
        "columnDefs": [
            { "targets": 5, "orderable": false, "responsivePriority": 1 },
            { "targets": [0, 4], "responsivePriority": 2 },
            { "targets": [1, 2, 3], "responsivePriority": 3 }
        ]
    });
});
