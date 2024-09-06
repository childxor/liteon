$(document).on('click', '.tree label', function (e) {
    $(this).next('ul').fadeToggle();
    e.stopPropagation();
});

$(document).on('change', '.tree input[type=checkbox]', function (e) {
    $(this).siblings('ul').find("input[type='checkbox']").prop('checked', this.checked);
    $(this).parentsUntil('.tree').children("input[type='checkbox']").prop('checked', this.checked);
    e.stopPropagation();
});

$(document).on('click', '.button_tree', function (e) {
    switch ($(this).data('id')) {
        case 'Collepsed':
            $('.tree ul').fadeOut();
            break;
        case 'Expanded':
            $('.tree ul').fadeIn();
            break;
        case 'Checked All':
            $(".tree input[type='checkbox']").prop('checked', true);
            create_icon.val(null);
            create_icon.select2('destroy');
            create_icon.select2({
                escapeMarkup: function (m) {
                    return m;
                },
                placeholder: "",
                allowClear: true
            });
            $('.tree input[type=checkbox]:checked').each(function () {
                $('option').prop('disabled', false);
            });
            break;
        case 'Unchecked All':
            $(".tree input[type='checkbox']").prop('checked', false);
            create_icon.val(null);
            create_icon.select2('destroy');
            create_icon.select2({
                escapeMarkup: function (m) {
                    return m;
                },
                placeholder: "",
                allowClear: true
            });
            $('.tree input[type=checkbox]:not(:checked)').each(function () {
                $('option').prop('disabled', true);
            });
            break;
        default:
    }
});