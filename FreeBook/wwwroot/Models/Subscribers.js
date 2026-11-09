
$(document).ready(function () {
    $('#tableSubscribers').DataTable({
        "autoWidth": false,
        "responsive": true
    });
});

function postDelete(url, id) {
    const form = document.getElementById('deleteForm');
    form.action = url;
    document.getElementById('deleteFormSubscriberId').value = id;
    form.submit();
}

function postBlacklist(id) {
    const form = document.getElementById('blacklistForm');
    form.action = '/Admin/Subscribers/ToggleBlacklist';
    document.getElementById('blacklistFormId').value = id;
    form.submit();
}

function DeleteSubscriber(id) {
    confirmDelete().then((result) => {
        if (result.isConfirmed) {
            postDelete('/Admin/Subscribers/DeleteSubscriber', id);
        }
    })
}

function ToggleBlacklist(id) {
    confirmDelete().then((result) => {
        if (result.isConfirmed) {
            postBlacklist(id);
        }
    })
}

Edit = (id, firstName, lastName, nationalId, mobileNumber, hasWhatsApp, email, areaId, governorateId, address, isBlackListed, dateOfBirth) => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('subscriberModalTitle').innerHTML = lbTitleEditSubscriber;
    document.getElementById('subscriberModalBtnSave').value = lbEdit;
    document.getElementById('subscriberId').value = id;
    document.getElementById('firstName').value = firstName;
    document.getElementById('lastName').value = lastName;
    document.getElementById('nationalId').value = nationalId;
    document.getElementById('mobileNumber').value = mobileNumber;
    document.getElementById('email').value = email;
    document.getElementById('areaId').value = areaId;
    document.getElementById('governorateId').value = governorateId;
    document.getElementById('address').value = address;
    document.getElementById('hasWhatsApp').checked = (hasWhatsApp === 'true' || hasWhatsApp === 'True');
    document.getElementById('isBlackListed').checked = (isBlackListed === 'true' || isBlackListed === 'True');
    document.getElementById('dateOfBirth').value = dateOfBirth;
}

Rest = () => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('subscriberModalTitle').innerHTML = lbBtnSaveNewSubscriber;
    document.getElementById('subscriberModalBtnSave').value = lbBtnSave;
    document.getElementById('subscriberId').value = "";
    document.getElementById('firstName').value = "";
    document.getElementById('lastName').value = "";
    document.getElementById('nationalId').value = "";
    document.getElementById('mobileNumber').value = "";
    document.getElementById('email').value = "";
    document.getElementById('areaId').value = "";
    document.getElementById('governorateId').value = "";
    document.getElementById('address').value = "";
    document.getElementById('hasWhatsApp').checked = false;
    document.getElementById('isBlackListed').checked = false;
    document.getElementById('dateOfBirth').value = "";
}
