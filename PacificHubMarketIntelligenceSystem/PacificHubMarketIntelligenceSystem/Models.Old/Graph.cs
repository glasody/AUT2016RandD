using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PacificHubMarketIntelligenceSystem.Models
{
    public class Graph
    {
        public IEnumerable<Node> Nodes { get; set; }
        public IEnumerable<Relationship> Relationships { get; set; }
    }
}