using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UpdateStatisticsWeb.Models
{
    public static partial class DateTimeExtensions
    {
        public static double ToJson(this DateTime? dt)
        {
            if (dt.HasValue)
                return dt.Value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            else
                return 0;
        }
    }
}