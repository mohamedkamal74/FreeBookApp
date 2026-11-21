$(document).ready(function () {
    $('#tableBooks').DataTable({
        "autoWidth": false,
        "responsive": true
    });

    $('#categoryId').on('change', filterSubCategories);
    $('#coverImage').on('change', previewCoverImage);
    filterSubCategories();
});

function filterSubCategories() {
    const categoryId = document.getElementById('categoryId').value;
    $('#subCategoryId option').each(function () {
        const optionCategory = this.dataset.category || '';
        const show = !categoryId || !optionCategory || optionCategory === categoryId;
        $(this).toggle(show);
        if (!show && this.selected) {
            this.selected = false;
        }
    });
}

function showCoverPreview(url) {
    const preview = document.getElementById('coverImagePreview');
    if (!preview) {
        return;
    }

    if (!url) {
        preview.src = '';
        preview.classList.add('d-none');
        return;
    }

    preview.src = url;
    preview.classList.remove('d-none');
}

function previewCoverImage() {
    const input = document.getElementById('coverImage');
    const file = input.files && input.files[0];
    if (!file) {
        showCoverPreview('');
        return;
    }

    const reader = new FileReader();
    reader.onload = function (event) {
        showCoverPreview(event.target.result);
    };
    reader.readAsDataURL(file);
}

function postDelete(url, id) {
    const form = document.getElementById('deleteForm');
    form.action = url;
    document.getElementById('deleteFormId').value = id;
    form.submit();
}

function DeleteBook(id) {
    confirmDelete().then((result) => {
        if (result.isConfirmed) {
            postDelete('/Admin/Books/DeleteBook', id);
        }
    });
}

function Edit(button) {
    clearValidation(document.getElementById('exampleModal'));

    const data = button.dataset;
    document.getElementById('bookModalTitle').innerHTML = lbTitleEditBook;
    document.getElementById('bookModalBtnSave').value = window.bookify.ui.edit;
    document.getElementById('bookId').value = data.id || '';
    document.getElementById('bookName').value = data.name || '';
    document.getElementById('authorId').value = data.author || '';
    document.getElementById('publisher').value = data.publisher || '';
    document.getElementById('publishingDate').value = data.publishingDate || '';
    document.getElementById('categoryId').value = data.category || '';
    filterSubCategories();
    document.getElementById('subCategoryId').value = data.subcategory || '';
    document.getElementById('hall').value = data.hall || '';
    document.getElementById('isAvailableForRental').checked = data.available === 'true' || data.available === 'True';
    document.getElementById('coverImage').value = '';
    showCoverPreview(data.thumbnailUrl || data.imageUrl || '');
    document.getElementById('imageUrl').value = data.imageUrl || '';
    document.getElementById('imageThumbnailUrl').value = data.thumbnailUrl || '';
    document.getElementById('imagePublicId').value = data.publicId || '';
    document.getElementById('description').value = data.description || '';
}

function Rest() {
    clearValidation(document.getElementById('exampleModal'));

    document.getElementById('bookModalTitle').innerHTML = lbBtnSaveNewBook;
    document.getElementById('bookModalBtnSave').value = window.bookify.ui.save;
    document.getElementById('bookId').value = '';
    document.getElementById('bookName').value = '';
    document.getElementById('authorId').value = '';
    document.getElementById('publisher').value = '';
    document.getElementById('publishingDate').value = '';
    document.getElementById('categoryId').value = '';
    filterSubCategories();
    document.getElementById('subCategoryId').value = '';
    document.getElementById('hall').value = '';
    document.getElementById('isAvailableForRental').checked = true;
    document.getElementById('coverImage').value = '';
    showCoverPreview('');
    document.getElementById('imageUrl').value = '';
    document.getElementById('imageThumbnailUrl').value = '';
    document.getElementById('imagePublicId').value = '';
    document.getElementById('description').value = '';
}
