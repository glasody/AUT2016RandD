using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PacificHubMarketIntelligenceSystem.Models
{
    public class Data
    {
        public IDictionary<string, string> Row { get; set; }
        public Meta Meta { get; set; }
        public Graph Graph { get; set; }
    }
}