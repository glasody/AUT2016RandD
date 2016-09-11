using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PacificHubMarketIntelligenceSystem.Models
{
    public class Relationship
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public int StartNode { get; set; }
        public int EndNode { get; set; }
        public IDictionary<string, string> Properties { get; set; }
    }
}