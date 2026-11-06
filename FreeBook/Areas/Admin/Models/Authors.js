function Rest() {
    $("#authorId").val("");
    $("#authorName").val("");
    $("#authorName").removeClass("is-invalid");
    $("#title").text(lbBtnSaveNewRole);
    $("#btnSave").val(lbBtnSave);
}

function Edit(id, name) {
    Rest();
    $("#authorId").val(id);
    $("#authorName").val(name);
    $("#title").text(lbTitleEdit);
    $("#btnSave").val(lbEdit);
}

function DeleteAuthor(id) {
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
    $("#tableAuthors").DataTable({
        "responsive": true,
        "lengthChange": false,
        "autoWidth": false,
        "buttons": ["copy", "csv", "excel", "pdf", "print"]
    }).buttons().container().appendTo('#tableAuthors_wrapper .col-md-6:eq(0)');
});