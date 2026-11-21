// Shared DataTables setup for the admin report pages.
//
// Arabic used to come out wrong in two of the export buttons:
//   CSV - the file is UTF-8 but the CSV button does not prepend a byte order
//         mark, so Excel guesses a legacy ANSI code page and turns every
//         Arabic word into mojibake. `bom: true` adds EF BB BF.
//   PDF - the bundled pdfmake only ships Roboto, which has no Arabic glyphs,
//         and pdfmake does no bidi or letter shaping, so Arabic can never
//         render correctly there. The PDF button reuses the print view
//         instead: the browser print dialog uses system fonts (correct
//         shaping and order) and offers "Save as PDF".
//
// Excel (.xlsx) writes UTF-8 XML and is already safe.
//
// Copy is not offered: it exports the same data as CSV but to the clipboard
// only, which made it redundant next to four real file exports.

function reportPrintStyle(dir) {
    return [
        '@page { margin: 12mm; }',
        'body.dt-print-view { font-family: Cairo, "Segoe UI", Tahoma, sans-serif; direction: ' + dir + '; }',
        'body.dt-print-view h1 { font-size: 18px; margin: 0 0 12px; }',
        'body.dt-print-view table { border-collapse: collapse; width: 100%; font-size: 12px; }',
        'body.dt-print-view th, body.dt-print-view td { border: 1px solid #999; padding: 4px 6px; text-align: ' + (dir === 'rtl' ? 'right' : 'left') + '; }',
        'body.dt-print-view thead th { background: #f2f2f2; }'
    ].join('\n');
}

function customizeReportPrint(win) {
    var dir = document.documentElement.getAttribute('dir') || 'ltr';
    var lang = document.documentElement.getAttribute('lang');
    if (lang) {
        win.document.documentElement.setAttribute('lang', lang);
    }
    win.document.documentElement.setAttribute('dir', dir);
    var style = win.document.createElement('style');
    style.appendChild(win.document.createTextNode(reportPrintStyle(dir)));
    win.document.head.appendChild(style);
}

// selector must be an id selector, e.g. '#tableRentalHistory'.
function initReportTable(selector, options) {
    // Reports can opt columns out of exports (for example, thumbnail cells).
    var exportOptions = options && options.exportOptions;
    var settings = $.extend(true, {}, options || {});
    delete settings.exportOptions;

    function withExport(button) {
        if (exportOptions) {
            button.exportOptions = $.extend(true, {}, exportOptions);
        }
        return button;
    }

    var table = $(selector).DataTable($.extend(true, {
        responsive: true,
        lengthChange: false,
        autoWidth: false,
        buttons: [
            withExport({ extend: 'csv', bom: true }),
            withExport({ extend: 'excel' }),
            withExport({
                // Same print view as the print button, labelled the way users
                // look for it: their browser saves the dialog as a PDF.
                extend: 'print',
                text: '<i class="fas fa-file-pdf"></i> PDF',
                className: 'buttons-print buttons-pdf',
                customize: customizeReportPrint
            }),
            withExport({ extend: 'print', customize: customizeReportPrint })
        ]
    }, settings));

    // btn-group glues the buttons together and strips their corner radius.
    // Dropping it lets theme.css space them evenly in both directions.
    table.buttons().container()
        .removeClass('btn-group')
        .addClass('dt-buttons-spaced')
        .appendTo(selector + '_wrapper .col-md-6:eq(0)');

    return table;
}
