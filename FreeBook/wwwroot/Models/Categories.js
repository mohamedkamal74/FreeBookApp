
$(document).ready(function () {
    $('#tableCategories').DataTable({
        "autoWidth": false,
       " responsive": true
    });
    $('#tableLogCategories').DataTable({
        "autoWidth": false,
        " responsive": true
    });
});


function Delete(id) {
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
            window.location.href = `/Admin/Categories/Delete?Id=${id}`;
            Swal.fire(
                'تم المسح',
                'تم مسح مجموعة المستخدم بنجاح ',
                'success'
            )
        }
    })
}

function DeleteLog(id) {
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
            window.location.href = `/Admin/Categories/DeleteLog?Id=${id}`;
            Swal.fire(
                'تم المسح',
                'تم مسح مجموعة المستخدم بنجاح ',
                'success'
            )
        }
    })
}

Edit = (id, name, description) => {
    document.getElementById('title').innerHTML = lbTitleEditCategory;
    document.getElementById('btnSave').value = lbEdit;
    document.getElementById('categoryId').value = id;
    document.getElementById('categoryName').value = name;
    document.getElementById('description').value = description;   
}

Rest = () => {
    document.getElementById('title').innerHTML = lbBtnSaveNewCategory;
    document.getElementById('categoryId').value = "";
    document.getElementById('categoryName').value ="" ;
    document.getElementById('description').value = "";
}



document.getElementById("defaultOpen").click();

function openCity(evt, cityName) {
    // Declare all variables
    var i, tabcontent, tablinks;

    // Get all elements with class="tabcontent" and hide them
    tabcontent = document.getElementsByClassName("tabcontent");
    for (i = 0; i < tabcontent.length; i++) {
        tabcontent[i].style.display = "none";
    }

    // Get all elements with class="tablinks" and remove the class "active"
    tablinks = document.getElementsByClassName("tablinks");
    for (i = 0; i < tablinks.length; i++) {
        tablinks[i].className = tablinks[i].className.replace(" active", "");
    }

    // Show the current tab, and add an "active" class to the button that opened the tab
    document.getElementById(cityName).style.display = "block";
    evt.currentTarget.className += " active";
}

