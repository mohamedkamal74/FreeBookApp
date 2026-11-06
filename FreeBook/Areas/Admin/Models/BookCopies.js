function Rest() {
    $("#bookCopyId").val("");
    $("#bookId").val("");
    $("#editionNumber").val("");
    $("#serialNumber").val("");
    $("#isAvailable").prop("checked", true);
    $("#bookId, #editionNumber, #serialNumber").removeClass("is-invalid");
    $("#title").text(lbBtnSaveNewCategory);
    $("#btnSave").val(lbBtnSave);
}

function Edit(id, bookId, editionNumber, serialNumber, isAvailable) {
    Rest();
    $("#bookCopyId").val(id);
    $("#bookId").val(bookId);
    $("#editionNumber").val(editionNumber);
    $("#serialNumber").val(serialNumber);
    $("#isAvailable").prop("checked", isAvailable === "True" || isAvailable === "true");
    $("#title").text(lbTitleEdit);
    $("#btnSave").val(lbEdit);
}

function DeleteBookCopy(id) {
    Swal.fire({
        title: lbTitleDeleteMsg,
        text: lmTextMsgDelete,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: lbconfirmButtonText,
        cancelButtonText: lbCancelButtonText
    }).then((result) => {
        if (result.isConfirmed) {
            $("#deleteFormId").val(id);
            $("#deleteForm").submit();
        }
    });
}

$(function () {
    $("#tableBookCopies").DataTable({
        "responsive": true,
        "lengthChange": false,
        "autoWidth": false,
        "buttons": ["copy", "csv", "excel", "pdf", "print"]
    }).buttons().container().appendTo('#tableBookCopies_wrapper .col-md-6:eq(0)');
});