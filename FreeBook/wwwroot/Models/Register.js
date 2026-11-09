$(document).ready(function () {
    $('#tableRoles').DataTable({
        "autoWidth": false,
        responsive: true
    });
});

//function DeleteUser(id) {
//    Swal.fire({
//        title: lbTitleDeleteMsg,
//        text: lmTextMsgDelete,
//        icon: 'warning',
//        showCancelButton: true,
//        confirmButtonColor: '#3085d6',
//        cancelButtonColor: '#d33',
//        confirmButtonText: lbconfirmButtonText,
//        cancelButtonText: lbCancelButtonText
//    }).then((result) => {
//        if (result.isConfirmed) {
//            window.location.href = `/Admin/Accounts/DeleteUser?userId=${id}`;
//            Swal.fire(
//                lbTitleDeletedOk,
//                lbTitleDeleteMsg,
//                lbSuccess
//            )
//        }
//    })
//}

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
            const form = document.getElementById('deleteForm');
            form.action = '/Admin/Accounts/DeleteUser';
            document.getElementById('deleteFormUserId').value = id;
            form.submit();
        }
    })
}

Edit = (id, name, email, image, role, active) => {
    // Clear validation state before populating edit data
    const modal = document.getElementById('exampleModal');
    clearValidation(modal);

    document.getElementById('userModalTitle').innerHTML = lbTitleEdit;
    document.getElementById('userModalBtnSave').value = lbUpdate;
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
    // Placeholder only: the update path never reads the password, but the field
    // still has to satisfy [Required] + [MinLength(8)] + [Compare].
    document.getElementById('userPassword').value = "$$$$$$$$";
    document.getElementById('userConfirm').value = "$$$$$$$$";
    var imageElement = document.getElementById('image');
    var imageName = image || '';
    if (imageName) {
        imageElement.hidden = false;
        imageElement.src = lbPathImageUser + '/' + imageName;
    } else {
        imageElement.hidden = true;
        imageElement.removeAttribute('src');
    }
    document.getElementById('imagehidden').value = imageName;
    
}

Rest = () => {
    // Clear validation state before resetting form
    const modal = document.getElementById('exampleModal');
    clearValidation(modal);

    document.getElementById('userModalTitle').innerHTML = lbBtnSaveNewUser
    document.getElementById('userModalBtnSave').value = lbBtnSave;
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
