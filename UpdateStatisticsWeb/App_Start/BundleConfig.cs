using System.Web;
using System.Web.Optimization;

namespace UpdateStatisticsWeb
{
    public class BundleConfig
    {
        //Дополнительные сведения об объединении см. по адресу: http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Используйте версию Modernizr для разработчиков, чтобы учиться работать. Когда вы будете готовы перейти к работе,
            // используйте средство сборки на сайте http://modernizr.com, чтобы выбрать только нужные тесты.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new ScriptBundle("~/bundles/devextreme-main").Include(
                "~/Scripts/cldr.js",
                "~/Scripts/cldr/event.js",
                "~/Scripts/cldr/supplemental.js",
                "~/Scripts/cldr/unresolved.js",
                "~/Scripts/globalize.js",
                "~/Scripts/globalize/number.js",
                "~/Scripts/globalize/date.js",
                "~/Scripts/globalize/message.js",
                "~/Scripts/dx.viz-web.js",                                               
                "~/Scripts/devextreme-localization/dx.web.ru.js",
                "~/Scripts/jszip.min.js",
                "~/Scripts/moment.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/devextreme-charts").Include(
                      "~/Scripts/dx.chartjs.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                                      "~/Content/bootstrap.min.css",
                      "~/Content/dx.common.css",
                      "~/Content/dx.light.css"));
        }
    }
}
