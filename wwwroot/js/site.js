// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$('.custom-file-input').on('change', function filePickerChangeEventHandler() {
    let fileName = $(this).val();
    let slashA = fileName.lastIndexOf('/');
    let slashB = fileName.lastIndexOf('\\');
    let slashIndex = slashA > -1 ? slashA : slashB;

    if (slashIndex > 0) {
        fileName = fileName.slice(slashIndex + 1);
    } else {
        fileName = 'Choose file';
    }

    $(this).next('.custom-file-label').html(fileName);
});