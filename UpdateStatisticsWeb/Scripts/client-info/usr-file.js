var usrGridOptions = {
    dataSource: [{}],
    showColumnLines: true,
    showRowLines: true,
    columnAutoWidth: true,
    allowColumnResizing: true,
    scrolling: {
        mode: 'virtual'
    },
    columns: [
        {
            dataField: "SystemName", caption: "Имя системы", cellTemplate: function (container, options) {
                $('<div/>')
                .text(options.value).appendTo(container);
                $(container).css('background', options.data.DistrStatus)
            }, width: '20%'
        },
        {
            dataField: "DistributiveNumber", caption: 'Номер дистрибутива', width: '20%', cellTemplate: function (container, options) {
                $('<div/>')
                .text(options.value).appendTo(container);
                $(container).css('background', options.data.DistrStatus)
            }
        },
        {
            dataField: "UpdateDateWithDocs1", caption: 'Дата обновления/Кол-во документов', cellTemplate: function (container, options) {
                $('<div/>')
                .text(options.value).appendTo(container);

                $(container).css('background', options.data.UpdateStatus)
            }
        },
        { dataField: "UpdateDateWithDocs2", caption: 'Дата обновления/Кол-во документов' },
        { dataField: "UpdateDateWithDocs3", caption: 'Дата обновления/Кол-во документов' },
        { dataField: "UpdateDateWithDocs4", caption: 'Дата обновления/Кол-во документов' }
    ],
    onRowPrepared: function (rowInfo) {
        if (rowInfo.data) {
            if (rowInfo.data.Status == "green")
                rowInfo.rowElement.css('background', '#86CD82');
            else if (rowInfo.data.Status == "yellow")
                rowInfo.rowElement.css('background', '#FFE066');
            else if (rowInfo.data.Status == "red")
                rowInfo.rowElement.css('background', '#F25F5C');
            else if (rowInfo.data.Status == "purple")
                rowInfo.rowElement.css('background', '#BADEFC');
            else if (rowInfo.data.Status == "yellow-red") {
                rowInfo.rowElement.addClass('yellow-red-row');
                rowInfo.rowElement.css('background', '#F25F5C');
            }
        }
    },
    allowColumnReordering: true,
    wordWrapEnabled: true,
    "export": {
        enabled: true,
        fileName: "UsrFile"
    },
    cacheEnabled: false
};

$(document).ready(function () {
    var grid = $('#usrSystems').dxDataGrid(usrGridOptions).dxDataGrid("instance");
    var returndata;
    $.ajax({
        type: "GET",
        url: $('#usrSystems').attr('data-get-source'),
        data: { serverName: $('#helpers').attr('data-server-name'), distributiveNumber: $('#helpers').attr('data-distributive-number'), distributiveId: $('#helpers').attr('data-distributive-id') },
        async: false,
        success: function (data) {
            returndata = data;
        }
    });

    var status = "#a1f57e";
    if (returndata) {
        if (moment(returndata.UsrFileDate).isBefore(moment().subtract(7, 'days')))
            status = "#f38888";

        $('#usrHead').css('background', status).text('Дата последнего USR файла: ' + moment(returndata.UsrFileDate).format("DD.MM.YYYY HH:mm:ss") + ' (дата в файле ' + moment(returndata.InUsrFileDate).format("DD.MM.YYYY HH:mm:ss") + " )");
    }
    grid.option("dataSource", returndata.UsrSystems);
});