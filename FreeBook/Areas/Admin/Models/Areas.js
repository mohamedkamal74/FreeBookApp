function Rest() {
    $("#areaId").val("");
    $("#areaName").val("");
    $("#governorateId").val("");
    $("#areaName, #governorateId").removeClass("is-invalid");
    $("#title").text(lbBtnSaveNewCategory);
    $("#btnSave").val(lbBtnSave);
}

function Edit(id, name, governorateId) {
    Rest();
    $("#areaId").val(id);
    $("#areaName").val(name);
    $("#governorateId").val(governorateId);
    $("#title").text(lbTitleEdit);
    $("#btnSave").val(lbEdit);
}

function DeleteArea(id) {
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
    $("#tableAreas").DataTable({
        "responsive": true,
        "lengthChange": false,
        "autoWidth": false,
        "buttons": ["copy", "csv", "excel", "pdf", "print"]
    }).buttons().container().appendTo('#tableAreas_wrapper .col-md-6:eq(0)');
});