using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using UpdateStatisticsWeb.Models;

namespace UpdateStatisticsWeb.Infrastructure
{
    public class GridOptionsJsonModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = controllerContext.HttpContext.Request.Params["options"]; ;
               
                return serializer.Deserialize<DevExtremeGridOptions>(json);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.AddModelError("", "The item could not be serialized");
                LogManager.GetCurrentClassLogger().Error("Ошибка при попытке десериализовать опции таблицы с клиентами. Текст ошибки: {0}", ex.Message);
                return null;
            }
        }
    }
}