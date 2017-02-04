var scriptsPath = $('#helpers').attr('data-localization-path') + "scripts/";
Loader.init('#clients-grid-container');
Loader.show();
$.when(
$.get(scriptsPath+"cldr/supplemental/likelySubtags.json"),
    $.get(scriptsPath + "ru/ca-gregorian.json"),
    $.get(scriptsPath + "ru/dateFields.json"),
    $.get(scriptsPath + "ru/ca-generic.json"),
    $.get(scriptsPath + "ru/timeZoneNames.json"),
    $.get(scriptsPath + "ru/currencies.json"),
    $.get(scriptsPath + "ru/numbers.json"),
    $.get(scriptsPath + "cldr/supplemental/timeData.json"),
    $.get(scriptsPath + "cldr/supplemental/weekData.json"),
    $.get(scriptsPath + "cldr/supplemental/currencyData.json"),
    $.get(scriptsPath + "cldr/supplemental/numberingSystems.json")
).then(function () {
    //The following code converts the got results into an array
    return [].slice.apply(arguments, [0]).map(function (result) {
        return result[0];
    });
}).then(
    Globalize.load //loads data held in each array item to Globalize
).then(function () {
    Globalize.locale("ru");
    formatter = Globalize.dateFormatter({ raw: 'dd.MM.yyyy HH:mm:ss' });
    InitializeApp();
})