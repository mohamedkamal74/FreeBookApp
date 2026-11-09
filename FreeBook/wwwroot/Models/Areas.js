
$(document).ready(function () {
    $('#tableAreas').DataTable({
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

function DeleteArea(id) {
    confirmDelete().then((result) => {
        if (result.isConfirmed) {
            postDelete('/Admin/Areas/DeleteArea', id);
        }
    })
}

Edit = (id, name, governorateId) => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('areaModalTitle').innerHTML = lbTitleEditArea;
    document.getElementById('areaModalBtnSave').value = lbEdit;
    document.getElementById('areaId').value = id;
    document.getElementById('areaName').value = name;
    document.getElementById('governorateId').value = governorateId;
}

Rest = () => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('areaModalTitle').innerHTML = lbBtnSaveNewArea;
    document.getElementById('areaModalBtnSave').value = lbSave;
    document.getElementById('areaId').value = "";
    document.getElementById('areaName').value = "";
    document.getElementById('governorateId').value = "";
}
