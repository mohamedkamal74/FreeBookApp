function Rest() {
    $("#subscriberId").val("");
    $("#firstName").val("");
    $("#lastName").val("");
    $("#dateOfBirth").val("");
    $("#nationalId").val("");
    $("#mobileNumber").val("");
    $("#hasWhatsApp").prop("checked", false);
    $("#email").val("");
    $("#areaId").val("");
    $("#governorateId").val("");
    $("#address").val("");
    $("#isBlackListed").prop("checked", false);
    $("#firstName, #lastName, #nationalId, #mobileNumber, #email, #areaId, #governorateId").removeClass("is-invalid");
    $("#title").text(lbBtnSaveNewUser);
    $("#btnSave").val(lbBtnSave);
}

function Edit(id, firstName, lastName, nationalId, mobileNumber, hasWhatsApp, email, areaId, governorateId, address, isBlackListed, dateOfBirth) {
    Rest();
    $("#subscriberId").val(id);
    $("#firstName").val(firstName);
    $("#lastName").val(lastName);
    $("#nationalId").val(nationalId);
    $("#mobileNumber").val(mobileNumber);
    $("#hasWhatsApp").prop("checked", hasWhatsApp === "True" || hasWhatsApp === "true");
    $("#email").val(email);
    $("#areaId").val(areaId);
    $("#governorateId").val(governorateId);
    $("#address").val(address);
    $("#isBlackListed").prop("checked", isBlackListed === "True" || isBlackListed === "true");
    if (dateOfBirth) {
        var date = new Date(dateOfBirth);
        var formattedDate = date.toISOString().split('T')[0];
        $("#dateOfBirth").val(formattedDate);
    }
    $("#title").text(lbTitleEdit);
    $("#btnSave").val(lbUpdate);
}

function DeleteSubscriber(id) {
    Swal.fire({
        title: lbTitleDeleteMsg,
        text: lmTextMsgDelete,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: lbconfirmButtonText,
        cancelButtonText: lbCancelButtonText
    }).then((result) => {
        if (result.isConfirmed) {
            $("#deleteFormSubscriberId").val(id);
            $("#deleteForm").submit();
        }
    });
}

function ToggleBlacklist(id) {
    $("#blacklistFormId").val(id);
    $("#blacklistForm").submit();
}

$(function () {
    $("#tableSubscribers").DataTable({
        "responsive": true,
        "lengthChange": false,
        "autoWidth": false,
        "buttons": ["copy", "csv", "excel", "pdf", "print"]
    }).buttons().container().appendTo('#tableSubscribers_wrapper .col-md-6:eq(0)');
});