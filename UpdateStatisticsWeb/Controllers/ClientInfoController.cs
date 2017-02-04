using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using UpdateStatisticsCore.Models;
using UpdateStatisticsCore.Providers;

namespace UpdateStatisticsWeb.Controllers
{
    public class ClientInfoController : Controller
    {
        private ClientInfoProvider _CIProvider;
        public ClientInfoController()
        {
            _CIProvider = new ClientInfoProvider();
        }

        [HttpGet]
        public ActionResult ClientInfo(int distributiveId, string serverName, string distributiveNumber, string engineerName, int pageNumber = 1)
        {
            ClientInfoViewModel viewModel = new ClientInfoViewModel();
            viewModel.ClientInfo = _CIProvider.GetClientInfo(distributiveId, serverName, distributiveNumber);

            viewModel.ClientInfo.EngineerName = engineerName;

            _CIProvider.SetCurrentClient(engineerName, Request.UserHostAddress, viewModel.ClientInfo.DistributiveId, viewModel.ClientInfo.ServerName);

            viewModel.ClientUpdates = _CIProvider.GetClientUpdates(distributiveId).OrderByDescending(clu => clu.StartDate).ToPagedList(pageNumber, 5);
            
            return View(viewModel);
        }

        public JsonResult GetUsrFileJson(string serverName, string distributiveNumber, int distributiveId)
        {
            return Json(_CIProvider.GetUsrFileDecryption(distributiveId, distributiveNumber, serverName), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetLogJson(string startDate, string serverName, int distributiveId)
        {
            DateTime? StartDate = Convert.ToDateTime(startDate);

            return Json(_CIProvider.GetUpdateLog(distributiveId, serverName, StartDate), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCurrentClientsList(string engName)
        {            
            return Json(_CIProvider.GetClientsListFromCache(engName, Request.UserHostAddress), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetClientCodeDescription(string clientCode)
        {
            return Json(_CIProvider.GetClientCodeDescription(clientCode), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Открыть usr файл в браузере
        /// </summary>
        /// <returns>Usr файл</returns>
        public ActionResult OpenUsr(string serverName, string distributiveNumber, int distributiveId)
        {
            FileInfo xmlFile = _CIProvider.GetUsrFile(distributiveId, distributiveNumber, ClientInfoProvider.UsrFileGetMode.Open, serverName);
            if (xmlFile != null)
            {
                Response.AppendHeader("Content-Disposition", "inline; filename=foo.pdf");
                return File(xmlFile.FullName, "application/xml");
            }
            return null;
        }
        /// <summary>
        /// Скачать usr файл
        /// </summary>
        /// <returns>Диалог для скачивания</returns>
        public ActionResult DownloadUsr(string serverName, string distributiveNumber, int distributiveId)
        {
            FileInfo usrFile = _CIProvider.GetUsrFile(distributiveId, distributiveNumber, ClientInfoProvider.UsrFileGetMode.Download, serverName);
            if (usrFile != null)
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(usrFile.FullName);
                return File(fileBytes, MediaTypeNames.Application.Octet, usrFile.Name);
            }
            return null;
        }
        /// <summary>
        /// Скачать архив с отчетом о пополнении
        /// </summary>
        /// <returns>Диалог для скачивания</returns>
        public ActionResult DownloadArchive(string serverName, int distributiveId)
        {
            FileInfo rarFile = _CIProvider.GetRarFile(distributiveId, serverName);
            if (rarFile != null)
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(rarFile.FullName);
                return File(fileBytes, MediaTypeNames.Application.Octet, rarFile.Name);
            }
            return null;
        }
    }
}