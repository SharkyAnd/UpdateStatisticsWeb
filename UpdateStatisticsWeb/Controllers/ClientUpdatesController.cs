using System;
using System.Collections.Generic;
using System.Web;
using System.Linq;
using System.Web.Mvc;
using System.Linq.Dynamic;
using UpdateStatisticsWeb.Models;
using System.Web.Script.Serialization;
using UpdateStatisticsCore.Models;
using System.Timers;
using Microsoft.AspNet.SignalR;

namespace UpdateStatisticsWeb.Controllers
{
    public class ClientUpdatesController : Controller
    {
        private UpdateStatisticsCore.Providers.ClientUpdatesProvider _cuProvider;
        public ClientUpdatesController()
        {
            _cuProvider = new UpdateStatisticsCore.Providers.ClientUpdatesProvider();
        }

        // GET: ClientUpdates
        public ActionResult ClientsView()
        {
            return View();
        }

        private void GetNowUpdatingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<GetNowUpdatingHub>();
            context.Clients.All.update();
        }

        private void GetMainGridTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<UpdateMainGridHub>();
            context.Clients.All.update();
        }

        public JsonResult GetNowUpdating()
        {
            return Json(_cuProvider.GetNowUpdatingMongo(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetClientUpdates(DevExtremeGridOptions options)
        {
            var clientUpd = _cuProvider.GetClientUpdatesMongo().OrderByDescending(cu => cu.StartDate);

            return Json(clientUpd, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTooltipDataJson(int distributiveId)
        {
            var lastMessage = _cuProvider.GetDistributiveUpdateLastMessage(distributiveId);
            return Json(new
            {
                Date = lastMessage.Moment.ToJson(),
                Message = lastMessage.Message
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult UpdateDistributiveComment(int distributiveId, string comment = null)
        {
            return Json(_cuProvider.UpdateDistributiveComment(distributiveId, comment), JsonRequestBehavior.AllowGet);
        }

        public JsonResult UpdateClientsList(string selectedRows, string engineerName)
        {
            var serializer = new JavaScriptSerializer();

            List<ClientInfo> selectedClients = serializer.Deserialize<List<ClientInfo>>(selectedRows);

            _cuProvider.UpdateClientsListByEngineerName(selectedClients, engineerName, Request.UserHostAddress);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSystemCodeDescriptionJson(string systemCode)
        {
            return Json(_cuProvider.GetSystemCodeDescription(systemCode), JsonRequestBehavior.AllowGet);
        }
    }
}