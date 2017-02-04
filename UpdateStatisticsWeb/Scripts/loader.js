var Loader = {
    init: function (source) {
        $('#LoadPanel').dxLoadPanel({
            position: { of: source },
            visible: false,
            showIndicator: true,
            showPane: true,
            closeOnOutsideClick: false,
            message:'Загрузка...'
        });
    },
    show: function () {
        $('#LoadPanel').dxLoadPanel("instance").show();
    },
    hide: function () {
        $('#LoadPanel').dxLoadPanel("instance").hide();
    }
}