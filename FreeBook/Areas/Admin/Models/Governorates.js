function Rest() {
    $("#governorateId").val("");
    $("#governorateName").val("");
    $("#governorateName").removeClass("is-invalid");
    $("#title").text(lbBtnSaveNewCategory);
    $("#btnSave").val(lbBtnSave);
}

function Edit(id, name) {
    Rest();
    $("#governorateId").val(id);
    $("#governorateName").val(name);
    $("#title").text(lbTitleEdit);
    $("#btnSave").val(lbEdit);
}

function DeleteGovernorate(id) {
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
    $("#tableGovernorates").DataTable({
        "responsive": true,
        "lengthChange": false,
        "autoWidth": false,
        "buttons": ["copy", "csv", "excel", "pdf", "print"]
    }).buttons().container().appendTo('#tableGovernorates_wrapper .col-md-6:eq(0)');
});