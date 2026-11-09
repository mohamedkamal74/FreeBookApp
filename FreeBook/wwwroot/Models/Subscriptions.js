
$(document).ready(function () {
    $('#tableSubscriptions').DataTable({
        "autoWidth": false,
        "responsive": true
    });
});

function postDelete(url, id) {
    const form = document.getElementById('deleteForm');
    form.action = url;
    document.getElementById('deleteFormId').value = id;
    form.submit();
}

function DeleteSubscription(id) {
    confirmDelete().then((result) => {
        if (result.isConfirmed) {
            postDelete('/Admin/Subscriptions/DeleteSubscription', id);
        }
    })
}

Rest = () => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('subscriptionModalTitle').innerHTML = lbBtnSaveNewSubscription;
    document.getElementById('subscriptionModalBtnSave').value = lbSave;
    document.getElementById('subscriptionId').value = "";
    document.getElementById('subscriberId').value = "";
    document.getElementById('startDate').value = "";
    document.getElementById('endDate').value = "";
}
