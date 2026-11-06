function Rest() {
    $("#subscriptionId").val("");
    $("#subscriberId").val("");
    $("#startDate").val("");
    $("#endDate").val("");
    $("#subscriberId, #startDate, #endDate").removeClass("is-invalid");
    $("#title").text(lbBtnSaveNewCategory);
    $("#btnSave").val(lbBtnSave);
}

function Edit(id, subscriberId, startDate, endDate) {
    Rest();
    $("#subscriptionId").val(id);
    $("#subscriberId").val(subscriberId);
    $("#startDate").val(startDate.split('T')[0]);
    $("#endDate").val(endDate.split('T')[0]);
    $("#title").text(lbTitleEdit);
    $("#btnSave").val(lbEdit);
}

function DeleteSubscription(id) {
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
    $("#tableSubscriptions").DataTable({
        "responsive": true,
        "lengthChange": false,
        "autoWidth": false,
        "buttons": ["copy", "csv", "excel", "pdf", "print"]
    }).buttons().container().appendTo('#tableSubscriptions_wrapper .col-md-6:eq(0)');
});