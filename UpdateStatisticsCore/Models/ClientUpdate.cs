using MongoDB.Bson;
using System;

namespace UpdateStatisticsCore.Models
{
    public class ClientUpdate
    {
        public ObjectId Id { get; set; }
        public int? DistributiveId { get; set; }
        public string DistributiveNumber { get; set; }
        public int SystemCode { get; set; }
        public double? StartDate { get; set; }
        public double? LastSuccessUpdateDate { get; set; }
        public double? EndDate { get; set; }
        public double? SttReceivedDate { get; set; }
        public string ClientName { get; set; }
        public bool IsCanceled { get; set; }
        public string DistributiveComment { get; set; }
        public string EngineerDistributiveComment { get; set; }
        public string EngineerName { get; set; }
        public string GroupChiefName { get; set; }
        public string ResVersion { get; set; }
        public int SendStt { get; set; }
        public int ClientReturnedCode { get; set; }
        public int LastSuccessUpdateClientReturnedCode { get; set; }
        public int ServerReturnedCode { get; set; }
        public int LastSuccessUpdateServerReturnedCode { get; set; }
        public string UsrRarFileName { get; set; }
        public string LastSuccessUpdateUsrRarFileName { get; set; }
        public string ServerName { get; set; }
        public string Status { get; set; }
    }

    public class LastMessage
    {
        public DateTime? Moment { get; set; }
        public string Message { get; set; }
    }
}
