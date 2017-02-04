var formatter;
var currentCacheId;
if (window.name == "") {
    var date = new Date();
    window.name = date.getYear().toString() + date.getMonth().toString() + date.getDay().toString() +
        date.getHours().toString() + date.getMinutes().toString() + date.getSeconds().toString() + date.getMilliseconds().toString();
}

var currentFilters = [];
var allFilters = ["IsCanceled", "OnlyNewUpdates"];

var load_tooltip_data = function (distributiveId) {
    var retData;
    $.ajax({
        type: 'POST',
        url: $('#clients-grid-container').attr('data-get-tooltip'),
        data: { distributiveId: distributiveId },
        async: false,
        success: function (data) {
            retData = data;
        }
    });
    return retData;
}

function GetSystemCodeDescription(systemCode) {
    var retData;
    $.ajax({
        type: 'GET',
        url: $('#clients-grid-container').attr('data-get-system-code'),
        data: { systemCode: systemCode },
        async: false,
        success: function (data) {
            retData = data;
        }
    });
    return retData;
}

var mainGridOptions = {
    width: '100%',
    dataSource: [{}],
    showColumnLines: true,
    paging: { pageSize: 20 },
    showRowLines: true,
    rowAlternationEnabled: false,
    headerFilter: {
        visible: true
    },
    editing: {
        allowUpdating: true,
        mode: 'cell'
    },
    scrolling: {
        mode: 'virtual'
    },
    filterRow: {
        visible: true,
        applyFilter: "auto"
    },
    grouping: {
        autoExpandAll: false
    },
    groupPanel: {
        visible: true
    },
    allowColumnResizing: true,
    columns: [
        {
            width: 60,
            alignment: 'center',
            cellTemplate: function (container, options) {
                if (options.data.EndDate == null) {
                    $('<div id=popover-' + options.data.DistributiveId + '/>').appendTo(container);
                    var tooltipWithTemplate = $("#popover-" + options.data.DistributiveId).dxTooltip({
                        target: "#popover-target-" + options.data.DistributiveId,
                        position: "right",
                        contentTemplate: function (data) {
                            var templ = load_tooltip_data(options.data.DistributiveId);
                            data.html("<div id='popup-content'/>").append('<div>Последнее сообщение:</div>')
                            .append('<div>Дата: ' + moment(templ.Date).format("DD.MM.YYYY HH:mm:ss") + '</div>')
                            .append('<div>Текст: ' + templ.Message + '</div>');
                        }
                    }).dxTooltip("instance");

                    $('<i class="fa fa-question-circle" aria-hidden="true" id="popover-target-' + options.data.DistributiveId + '"></i>').unbind().
                    hover(function () {
                        tooltipWithTemplate.toggle();
                    }).appendTo(container);

                }
            }, dataType: 'string'
        },
        { dataField: "DistributiveId", visible: false, allowEditing: false, dataType: 'number' },
        {
            dataField: "DistributiveNumber",
            cellTemplate: function (container, options) {
                var engName = "";
                var filter = $('#clients-grid-container').dxDataGrid("instance").getCombinedFilter();
                if (filter) {
                    var column = getColumnFieldName($('#clients-grid-container').dxDataGrid("instance"), filter[0]);
                    if (column == "EngineerName") {
                        engName = filter[2];
                    }
                }

                $('<a/>')
                .text(options.value)
                .attr('href', $('#clients-grid-container').attr('data-view-client-info') +
                    "?distributiveId=" + options.data.DistributiveId + "&&serverName=" + options.data.ServerName + "&&distributiveNumber=" + options.data.DistributiveNumber + "&&engineerName=" + engName)
                .appendTo(container);
            }, allowGrouping: false, width: '20%', allowEditing: false, caption: 'Номер дистрибутива', dataType: 'string'
        },
        {
            dataField: "SystemCode", allowGrouping: true, allowEditing: false, width: '20%', alignment: 'center', caption: 'Код системы', dataType: 'string',
            cellTemplate: function (container, options) {
                $('<div id=popover-system-code-' + options.data.DistributiveId + '></div>').attr('data-code', options.data.SystemCode).appendTo(container);
                var tooltipWithTemplate = $("#popover-system-code-" + options.data.DistributiveId).dxTooltip({
                    target: "#popover-system-code-target-" + options.data.DistributiveId,
                    position: "top",
                    contentTemplate: function (data) {
                        var templ = GetSystemCodeDescription(options.data.SystemCode);
                        data.html("<div id='popup-content'/>").append('<div>' + templ + '</div>')
                    }
                }).dxTooltip("instance");

                $('<div id="popover-system-code-target-' + options.data.DistributiveId + '">' + options.data.SystemCode + '</div>').unbind().
                hover(function () {
                    tooltipWithTemplate.toggle();
                }).appendTo(container);
            }
        },
        {
            dataField: "StartDate",
            dataType: "date",
            format: Globalize.dateFormatter({ raw: 'dd.MM.yyyy HH:mm:ss' }),
            allowGrouping: false,
            width: '20%', alignment: 'center', allowEditing: false, caption: 'Дата подключения'
        },
            {
                dataField: "LastSuccessUpdateDate",
                dataType: "date",
                format: Globalize.dateFormatter({ raw: 'dd.MM.yyyy HH:mm:ss' }),
                allowGrouping: false,
                width: '20%', alignment: 'center', allowEditing: false, caption: 'Дата успешного обновления'
            },
                {
                    dataField: "ClientName", allowGrouping: false, allowEditing: false, dataType: 'string', caption: 'Наименование клиента',
                    showWhenGrouped: true, width: 700
                },
                {
                    dataField: "EngineerName", allowGrouping: true, allowEditing: false, dataType: 'string', width: 150, alignment: 'center', caption: 'Инженер',
                    showWhenGrouped: true
                },
                {
                    dataField: "GroupChiefName", allowGrouping: true, allowEditing: false, dataType: 'string', width: '20%', alignment: 'center', caption: 'Руководитель',
                    showWhenGrouped: true
                },
                {
                    dataField: "EngineerDistributiveComment", allowEditing: true, dataType: 'string', allowGrouping: false, width: '20%', alignment: 'center', caption: 'Комментарий',
                    showWhenGrouped: true
                },
            {
                dataField: "SttReceivedDate",
                dataType: "date",
                format: Globalize.dateFormatter({ raw: 'dd.MM.yyyy HH:mm:ss' }),
                allowGrouping: false,
                filterOperations: ['>=', '<=', 'between'],
                selectedFilterOperation: '<=',
                width: '20%', alignment: 'center', allowEditing: false, caption: 'Дата получение STT',
                showWhenGrouped: true
            },
                { dataField: "ResVersion", allowGrouping: false, allowEditing: false, dataType: 'string', visible: false, caption: 'Версия res' },
                { dataField: "ServerName", allowGrouping: false, allowEditing: false, dataType: 'string', visible: false, caption: 'Имя сервера' },
                {
                    dataField: "Status", allowGrouping: false, allowEditing: false, dataType: 'string', visible: false, caption: 'Статус', cellTemplate: function (container, options) {
                        var statusText;

                        if (options.value == "green")
                            statusText = 'Пополнение не требуется';
                        else if (options.value == "yellow")
                            statusText = 'Неуспешное пополнение';
                        else if (options.value == "red")
                            statusText = 'Давно не пополнялся';
                        else if (options.value == "purple")
                            statusText = 'Идет обновление';
                        else if (options.value == "yellow-red") {
                            statusText = 'Давно не пополнялся/Неуспешное пополнение';
                        }
                        else
                            statusText = 'Ок';

                        $('<div/>').text(statusText).appendTo(container);
                    }
                }
    ],
    columnChooser: {
        enabled: true,
        height: 380,
        width: 400,
        emptyPanelText: 'A place to hide the columns'
    },
    summary: {
        groupItems: [{
            column: "StartDate",
            summaryType: "count",
            displayFormat: "Всего {0} клиентов"
        }
        ]
    },
    onEditorPrepared: function (e) {
        if ((e.dataField == 'StartDate' || e.dataField == 'EndDate' || e.dataField == 'SttReceivedDate') && e.parentType == 'filterRow') {
            e.editorElement.dxDateBox('instance').option('format', 'datetime');
            e.editorElement.dxDateBox('instance').option('onValueChanged', function (options) { e.setValue(options.value); });
        }
    },
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
    onRowUpdated: function (e) {
        $.ajax({
            type: 'GET',
            url: $('#clients-grid-container').attr('data-update-comment'),
            data: { distributiveId: e.key.DistributiveId, comment: e.data.EngineerDistributiveComment },
            success: function () {
                return true;
            }
        })
    },
    allowColumnReordering: true,
    wordWrapEnabled: true,
    "export": {
        enabled: true,
        fileName: "Client Statistics"
    },
    cacheEnabled: false,
    stateStoring: {
        enabled: true,
        type: "custom",
        customLoad: function () {
            var state = localStorage.getItem(window.name);
            if (state) {
                state = JSON.parse(state);
                if (state["yellow-red"]) {
                    setTimeout(function () {
                        $('#yellow-red-checkbox').dxCheckBox("instance").option("value", state["yellow-red"]);
                    }, 2000)
                }
                if (state["only-on-clients"]) {
                    setTimeout(function () {
                        $('#only-on-clients-checkbox').dxCheckBox("instance").option("value", state["only-on-clients"]);
                    }, 2000)
                }
                if (state["only-new-clients"]) {
                    setTimeout(function () {
                        $('#only-new-clients-checkbox').dxCheckBox("instance").option("value", state["only-new-clients"]);
                    }, 2000)
                }
                state.selectedRowKeys = null;
            }
            return state;
        },
        customSave: function (state) {
            state["yellow-red"] = $('#yellow-red-checkbox').dxCheckBox("instance").option("value");
            state["only-on-clients"] = $('#only-on-clients-checkbox').dxCheckBox("instance").option("value");
            state["only-new-clients"] = $('#only-new-clients-checkbox').dxCheckBox("instance").option("value");

            localStorage.setItem(window.name, JSON.stringify(state));
        }
    },
    onContentReady: function (e) {
        var grid = $('#clients-grid-container').dxDataGrid("instance");
        var filter = grid.getCombinedFilter();
        if (filter) {
            if (filter.columnIndex == 7) {
                $.when(grid.selectAll()).then(function () {
                    var selectedRows = grid.getSelectedRowsData();
                    grid.deselectAll();
                    if (selectedRows.length != 0)
                        $.ajax({
                            type: 'POST',
                            url: $('#clients-grid-container').attr('data-update-clients-list'),
                            data: { selectedRows: JSON.stringify(selectedRows), engineerName: filter[2] },
                            success: function (data) {
                                currentCacheId = data;
                            }
                        });
                })
            }
            /*else if (filter.columnIndex == 4 || filter.columnIndex == 5) {
                var columnName = getColumnFieldName(grid, filter[0][0]);
                filter[0][0] = columnName;
                filter[2][0] = columnName;
                var startDate = filter[0][2];
                var endDate = moment(startDate).endOf('month').toDate();
                filter[2][2] = endDate;
                grid.filter([filter[0], filter[2]]);
            }*/
        }
        $('#clientsCount').text('Общее количество клиентов: ' + this.totalCount());
    }
};

function getColumnFieldName(dataGridInstance, getter) {
    var column,
        i;

    if ($.isFunction(getter)) {
        for (i = 0; i < dataGridInstance.columnCount() ; i++) {
            column = dataGridInstance.columnOption(i);
            if (column.selector === getter) {
                return column.dataField;
            }
        }
    }
    else {
        return getter;
    }
}

var nowUpdatingGridOptions = {
    dataSource: [{}],
    showColumnLines: true,
    paging: { pageSize: 5 },
    showRowLines: true,
    rowAlternationEnabled: false,
    allowColumnResizing: true,
    columns: [
                {
                    dataField: "DistributiveNumber", width: 80, allowGrouping: false, caption: 'Номер дистрибутива',
                    showWhenGrouped: true, dataType: 'string'
                },
                {
                    dataField: "ClientName", allowGrouping: true, alignment: 'center', caption: 'Наименование клиента',
                    showWhenGrouped: true, dataType: 'string'
                },
                {
                    dataField: "Message", allowGrouping: false, alignment: 'center', caption: 'Последнее сообщение',
                    showWhenGrouped: true, dataType: 'string'
                },
                {
                    dataField: "FormattedDate", caption: 'Время последнего сообщения',
                    width: 300,
                    alignment: 'center',
                    cellTemplate: function (container, options) {
                        $('<div/>').addClass("last-message-date").attr("time", options.value).
                            appendTo(container);
                    }, dataType: 'string'
                },
    ],
    wordWrapEnabled: true,
    onRowPrepared: function (rowInfo) {
        if (rowInfo.data) {
            if (rowInfo.data.Status == "yellow")
                rowInfo.rowElement.css('background', '#FFE066');
            else if (rowInfo.data.Status == "red")
                rowInfo.rowElement.css('background', '#F25F5C');
        }
    },
    cacheEnabled: false
};


function UpdateFilters() {
    var grid = $('#clients-grid-container').dxDataGrid("instance");
    var gridFilters = [];
    var filtersCount = 0;
    for (var i = 0; i < currentFilters.length; i++) {
        var currentFilter = currentFilters[i];
        switch (currentFilter) {
            case "IsCanceled":
                if (filtersCount > 1)
                    gridFilters.push('and');
                gridFilters.push(['IsCanceled', '=', false])
                filtersCount++;
                break;
            case "OnlyNewUpdates":
                if (filtersCount > 1)
                    gridFilters.push('and');
                var newClientsStartDate = moment().add(-30, 'days').toDate();
                gridFilters.push(['StartDate', '>=', newClientsStartDate]);
                filtersCount++;
                break;
            default:
                break;
        }
    }
    for (var i = 0; i < allFilters.length; i++) {
        var currentFilter = allFilters[i];
        if (currentFilters.indexOf(currentFilter) == -1)
            switch (currentFilter) {
                case "IsCanceled":
                    if (filtersCount > 1)
                        gridFilters.push('and');
                    gridFilters.push([['IsCanceled', '=', false], 'or', ['IsCanceled', '=', true]])
                    filtersCount++;
                    break;
                case "OnlyNewUpdates":
                    if (filtersCount > 1)
                        gridFilters.push('and');
                    var newClientsStartDate = moment().add(-30, 'days').toDate();
                    gridFilters.push([['StartDate', '>=', newClientsStartDate], 'or', ['StartDate', '<=', newClientsStartDate]]);
                    filtersCount++;
                    break;
                default:
                    break;
            }
    }
    grid.filter(gridFilters);
}

var getGridData = function () {
    Loader.show();
    var returndata;
    $.when($.ajax({
        type: "GET",
        url: $('#clients-grid-container').attr('data-get-source'),
        async: false,
        success: function (data) {
            $.map(data, function (item, index) {
                item["StartDate"] = item["StartDate"] == null ? null : moment(item["StartDate"]);
                item["EndDate"] = item["EndDate"] == null ? null : moment(item["EndDate"]);
                item["SttReceivedDate"] = item["SttReceivedDate"] == null ? null : moment(item["SttReceivedDate"]);
            })
            returndata = data;
        }
    })).then(function (data, textStatus, jqXHR) {
        var grid = $('#clients-grid-container').dxDataGrid(mainGridOptions).dxDataGrid("instance");
        grid.option("dataSource", returndata);
        $('#clientsCount').text('Общее количество клиентов: ' + returndata.length);
        Loader.hide();
    });
}

var getUpdatingNow = function () {
    var returndata;
    $.when($.ajax({
        type: "GET",
        url: $('#updating-now').attr('data-get-source'),
        success: function (data) {
            returndata = data;
        }
    })).then(function (data, textStatus, jqXHR) {
        var grid = $('#updating-now').dxDataGrid(nowUpdatingGridOptions).dxDataGrid("instance");
        grid.option("dataSource", returndata);
        $('#countdown').timeUpdate('.last-message-date', 'time', 1000);
    })
}

function InitializeApp() {
   
    $(document).ready(function () {

        $("#yellow-red-checkbox").dxCheckBox({
            text: "Желтеть при неудачных попытках",
            onValueChanged: function (e) {
                if (e.value)
                    $('.yellow-red-row').css('background', '#FFE066');
                else
                    $('.yellow-red-row').css('background', '#F25F5C');
            }
        });

        $("#only-on-clients-checkbox").dxCheckBox({
            text: "Только подключенные клиенты",
            onValueChanged: function (e) {
                if (e.value)
                    currentFilters.push('IsCanceled');
                else {
                    var index = currentFilters.indexOf('IsCanceled');
                    if (index > -1)
                        currentFilters.splice(index, 1);
                }
                UpdateFilters();
            }
        });

        $("#only-new-clients-checkbox").dxCheckBox({
            text: "Только попытки за последние 30 дней",
            onValueChanged: function (e) {
                if (e.value)
                    currentFilters.push('OnlyNewUpdates');
                else {
                    var index = currentFilters.indexOf('OnlyNewUpdates');
                    if (index > -1)
                        currentFilters.splice(index, 1);
                }
                UpdateFilters();
            }
        });

        $("#clear-sort-button").dxButton({
            text: 'Сбросить все фильтры',
            onClick: function () {
                $('#clients-grid-container').dxDataGrid("instance").clearFilter();
            }
        });

        getGridData();
        Loader.hide();
        getUpdatingNow();

        /*setInterval(function () {
            getGridData();
        }, 500000)

        setInterval(function () {
            getUpdatingNow();
        }, 100000)*/       
    });
}