
$(document).ready(function () {
    $('#tableGovernorates').DataTable({
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

function DeleteGovernorate(id) {
    confirmDelete().then((result) => {
        if (result.isConfirmed) {
            postDelete('/Admin/Governorates/DeleteGovernorate', id);
        }
    })
}

Edit = (id, name) => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('governorateModalTitle').innerHTML = lbTitleEditGovernorate;
    document.getElementById('governorateModalBtnSave').value = lbEdit;
    document.getElementById('governorateId').value = id;
    document.getElementById('governorateName').value = name;
}

Rest = () => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('governorateModalTitle').innerHTML = lbBtnSaveNewGovernorate;
    document.getElementById('governorateModalBtnSave').value = lbSave;
    document.getElementById('governorateId').value = "";
    document.getElementById('governorateName').value = "";
}
