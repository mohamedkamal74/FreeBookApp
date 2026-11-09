
$(document).ready(function () {
    $('#tableAuthors').DataTable({
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

function DeleteAuthor(id) {
    confirmDelete().then((result) => {
        if (result.isConfirmed) {
            postDelete('/Admin/Authors/DeleteAuthor', id);
        }
    })
}

Edit = (id, name) => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('authorModalTitle').innerHTML = lbTitleEditAuthor;
    document.getElementById('authorModalBtnSave').value = lbEdit;
    document.getElementById('authorId').value = id;
    document.getElementById('authorName').value = name;
}

Rest = () => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('authorModalTitle').innerHTML = lbBtnSaveNewAuthor;
    document.getElementById('authorModalBtnSave').value = lbSave;
    document.getElementById('authorId').value = "";
    document.getElementById('authorName').value = "";
}
