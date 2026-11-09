
$(document).ready(function () {
    $('#tableBookCopies').DataTable({
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

function DeleteBookCopy(id) {
    confirmDelete().then((result) => {
        if (result.isConfirmed) {
            postDelete('/Admin/BookCopies/DeleteBookCopy', id);
        }
    })
}

Edit = (id, bookId, editionNumber, serialNumber, isAvailable) => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('bookCopyModalTitle').innerHTML = lbTitleEditBookCopy;
    document.getElementById('bookCopyModalBtnSave').value = lbEdit;
    document.getElementById('bookCopyId').value = id;
    document.getElementById('bookId').value = bookId;
    document.getElementById('editionNumber').value = editionNumber;
    document.getElementById('serialNumber').value = serialNumber;
    document.getElementById('isAvailable').checked = (isAvailable === 'true' || isAvailable === 'True');
}

Rest = () => {
    clearValidation(document.getElementById('exampleModal'));
    document.getElementById('bookCopyModalTitle').innerHTML = lbBtnSaveNewBookCopy;
    document.getElementById('bookCopyModalBtnSave').value = lbSave;
    document.getElementById('bookCopyId').value = "";
    document.getElementById('bookId').value = "";
    document.getElementById('editionNumber').value = "";
    document.getElementById('serialNumber').value = "";
    document.getElementById('isAvailable').checked = true;
}
