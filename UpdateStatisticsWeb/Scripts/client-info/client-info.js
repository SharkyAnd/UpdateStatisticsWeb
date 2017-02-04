var clientId = 0;
var header = null;
var counter = 0;
var clients = null;
var currentIndex = null;
var openLog = function (caller) {
    $('.log-section').fadeOut(400)
    if (header === $(caller).attr('id') && counter > 0) {
        $(".log-section").slideUp('fast')
        counter = 0
    }
    else {
        $.ajax({
            type: 'GET',
            url: $('#updatesList').attr('data-get-log'),
            data: { distributiveId: $('#helpers').attr('data-distributive-id'), startDate: caller.attr('data-start-date'), serverName: caller.attr('data-server-name') },
            success: function (log) {
                if (log != " ") {
                    $('.qst').html('<p style="text-align:center; margin-bottom:20px;">Обработаны следующие файлы:</p>')
                    $('.log').html('<p style="text-align:center"><i>Лог подключения:</i></p><br/>' + log.MessageList)
                    for (var i = 0; i < log.QstList.length; i++) {
                        var qst = log.QstList[i]
                        var qstDiv = $('<span class="qstRecord"></span>')
                        qstDiv.append(qst.QstFileName + '</br>').append("<em style=color:" + qst.QstStatusColor + ">" + qst.QstStatusDescription + "</em")
                        $('.qst').append(qstDiv)
                    }
                    $('.log-section').fadeIn(400).slideDown('slow').css('display', 'table')
                }
            }
        })
        header = $(caller).attr('id')
        counter++;
    }
}

function arrayObjectIndexOf(myArray, searchTerm, property) {
    for (var i = 0, len = myArray.length; i < len; i++) {
        if (myArray[i][property] === searchTerm) return i;
    }
    return -1;
}

var getAnotherClient = function (kind) {
    switch (kind) {
        case "next":
            currentIndex++
            break;
        case "prev":
            currentIndex--
            break;
        default:
            break;
    }
    var client = clients[currentIndex];

    $.form($('#helpers').attr('data-initialize-client'),
        {
            distributiveId: client.DistributiveId, serverName: client.ServerName, distributiveNumber: client.DistributiveNumber, engineerName: $('#helpers').attr('data-eng-name')
        },'GET').submit()
}

function GetClientCodeDescription(clientCode) {
    var retData;
    $.ajax({
        type: 'GET',
        url: $('#helpers').attr('data-get-client-code-description'),
        data: { clientCode: clientCode },
        async: false,
        success: function (data) {
            retData = data;
        }
    });
    return retData;
}
var tooltipWithTemplate;
$(document).ready(function () {
    $('.body-content').on('click', ".try", function () {
        openLog($(this))
    });

    $('.sipheader').hover(function () {
        if (clients != null && clients.length != 0) {
            if (currentIndex == 0) {
                $('.right').fadeIn()
            }
            else if (currentIndex == clients.length - 1) {
                $('.left').fadeIn()
            }
            else {
                $('.arrow').fadeIn()
            }
        }
    }, function () {
        $('.arrow').fadeOut()
    })
    $('.sipheader').on('click', ".left", function () {
        getAnotherClient("prev")
    });
    $('.sipheader').on('click', ".right", function () {
        getAnotherClient("next")
    });
    $('.code[data-type="client"]').hover(function () {
        var codeDiv = $(this);
        codeDiv.children().each(function (index) { $(this).remove() });
        $('<div id="tooltip"></div>').appendTo(codeDiv);
        tooltipWithTemplate = $('#tooltip').dxTooltip({
            target: codeDiv,
            position: "top",
            contentTemplate: function (data) {
                var templ = GetClientCodeDescription(codeDiv.text());
                data.html("<div id='popup-content'/>").append('<div>' + templ + '</div>')
            }
        }).dxTooltip("instance");

        tooltipWithTemplate.show();
    }, function () {
        tooltipWithTemplate.hide();
    })
    $.ajax({
        type: 'GET',
        url: $('#helpers').attr('data-get-clients'),
        data: {engName: $('#helpers').attr('data-eng-name')},
        success: function (data) {
            clients = data;
            currentIndex = arrayObjectIndexOf(clients, true, "Current")
        }
    })
});