
$(document).ready(function () {
    $('#tableRoles').DataTable();
});

function DeleteRoles(id) {
    confirmDelete().then((result) => {
        if (result.isConfirmed) {
            const form = document.getElementById('deleteForm');
            form.action = '/Admin/Accounts/DeleteRole';
            document.getElementById('deleteFormId').value = id;
            form.submit();
        }
    })
}

Edit = (id, name) => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('roleModalTitle').innerHTML = lbTitleEdit;
    document.getElementById('roleModalBtnSave').value = lbEdit;
    document.getElementById('roleId').value = id;
    document.getElementById('roleName').value = name;
}

Rest = () => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('roleModalTitle').innerHTML = lbEditNewRole;
    document.getElementById('roleModalBtnSave').value = lbSave;
    document.getElementById('roleId').value = "";
    document.getElementById('roleName').value = "";
}

