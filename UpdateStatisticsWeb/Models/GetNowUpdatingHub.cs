﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace UpdateStatisticsWeb.Models
{
    public class GetNowUpdatingHub : Hub
    {
        public void Update()
        {
            Clients.All.update();
        }
    }
}