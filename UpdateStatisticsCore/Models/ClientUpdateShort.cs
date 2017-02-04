using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateStatisticsCore.Models
{
    public class ClientUpdateShort
    {
        public ObjectId Id { get; set; }
        public string DistributiveNumber { get; set; }
        public string FileName { get; set; }
        public string ClientName { get; set; }
        public string Message { get; set; }
        public string FormattedDate { get; set; }
        public string Status { get; set; }
    }
}
