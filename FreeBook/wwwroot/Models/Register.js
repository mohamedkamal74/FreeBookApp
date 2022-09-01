
$(document).ready(function () {
    $('#tableRoles').DataTable({
        "autoWidth": false,
        "responsive": true
    });
});

function DeleteUser(id) {
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
            window.location.href = `/Admin/Accounts/DeleteUser?userId=${id}`;
            Swal.fire(
                'تم المسح',
                'تم مسح مجموعة المستخدم بنجاح ',
                'success'
            )
        }
    })
}

Edit = (id, name, email, image, role, active) => {
    document.getElementById('title').innerHTML = "تعديل مجموعة المستخدم";
    document.getElementById('btnSave').value = "تعديل";
    document.getElementById('userId').value = id;
    document.getElementById('userName').value = name;
    document.getElementById('userEmail').value = email;
    document.getElementById('ddluserRole').value = role;
    var Active = document.getElementById('userActive');
    if (active == "True")
        Active.checked = true;
    else
        Active.checked = false;
    $('#grPassword').hide();
    $('#confPassword').hide();
    document.getElementById('userPassword').value = "$$$$$$";
    document.getElementById('userConfirm').value = "$$$$$$";
    document.getElementById('image').hidden = false;
    document.getElementById('image').src = "/Images/Users/" + image;
    document.getElementById('imagehidden').value = image;

    
}

Rest = () => {
    document.getElementById('title').innerHTML = "اضف مجموعة مستخدم جديد";
    document.getElementById('btnSave').value = "حفظ";
    document.getElementById('userId').value = "";
    document.getElementById('userName').value ="" ;
    document.getElementById('userEmail').value = "";
    //document.getElementById('userImage').value ="" ;
    document.getElementById('ddluserRole').value = "";
    document.getElementById('userActive').checked = false;
    $('#grPassword').show();
    $('#confPassword').show();
    document.getElementById('userPassword').value = "";
    document.getElementById('userConfirm').value = "";
    document.getElementById('image').hidden = true;


}

function ChangePassword(id) {
    document.getElementById('userPassId').value = id;
}

