using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateStatisticsCore.Models
{
    public class ClientInfoViewModel
    {
        public IPagedList<Update> ClientUpdates { get; set; }
        public List<UsrSystems> UsrFileDecryption { get; set; }
        public ClientInfo ClientInfo { get; set; }
    }

    public class ClientInfo
    {
        public int DistributiveId { get; set; }
        public string DistributiveNumber { get; set; }
        public string ClientName { get; set; }
        public string ClientComment { get; set; }
        public string ServerName { get; set; }
        public bool Current { get; set; }
        public string EngineerName { get; set; }
    }

    public class Update
    {
        public long? FileId { get; set; }
        public bool IsUsrExists { get; set; }
        public int? ClientCode { get; set; }
        public int? ServerCode { get; set; }
        public string StatusColor { get; set; }
        public string DownloadTime { get; set; }
        public string DownloadSpeed { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string UpdateTime { get; set; }
        public string UpdateSize { get; set; }
        public string ServerName { get; set; }
    }

    public class UsrFile
    {
        public DateTime? UsrFileDate { get; set; }
        public DateTime? InUsrFileDate { get; set; }
        public List<UsrSystems> UsrSystems { get; set; }
    }

    public class UsrSystems
    {
        public string DistrStatus { get; set; }
        public string UpdateStatus { get; set; }
        public string UpdateKind { get; set; }
        public string SystemName { get; set; }
        public string SystemDirectory { get; set; }
        public string DistributiveNumber { get; set; }
        public string Computer { get; set; }
        public string UpdateDateWithDocs1 { get; set; }
        public string UpdateDateWithDocs2 { get; set; }
        public string UpdateDateWithDocs3 { get; set; }
        public string UpdateDateWithDocs4 { get; set; }
    }

    public class DBDistributive
    {
        public int Id { get; set; }
        public string SoprType { get; set; }
        public string DistrType { get; set; }
    }

    public class UpdateLog
    {
        public string MessageList { get; set; }
        public List<QST> QstList { get; set; }
    }

    public class QST
    {
        public string QstFileName { get; set; }
        public string QstStatusDescription { get; set; }
        public string QstStatusColor { get; set; }
    }
}
