using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PacificHubMarketIntelligenceSystem.Models
{
    public class Node
    {
        public long Id { get; set; }
        public IEnumerable<string> Labels { get; set; }
        public IDictionary<string, string> Properties { get; set; }
    }
}