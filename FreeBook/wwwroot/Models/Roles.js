
$(document).ready(function () {
    $('#tableRoles').DataTable();
});

function DeleteRoles(id) {
    Swal.fire({
        title: 'هل انت متاكد ؟',
        text: "لن تتمكن من التراجع !",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = `/Admin/Accounts/DeleteRole?Id=${id}`;
            Swal.fire(
                'تم المسح',
                'تم مسح مجموعة المستخدم بنجاح ',
                'success'
            )
        }
    })
}

Edit = (id, name) => {
    document.getElementById('title').innerHTML = lbTitleEdit;
    document.getElementById('btnSave').value = lbEdit;
    document.getElementById('roleId').value = id;
    document.getElementById('roleName').value = name;
}

Rest = () => {
    document.getElementById('title').innerHTML = lbEditNewRole;
    document.getElementById('btnSave').value = lbSave;
    document.getElementById('roleId').value = "";
    document.getElementById('roleName').value = "";
}

