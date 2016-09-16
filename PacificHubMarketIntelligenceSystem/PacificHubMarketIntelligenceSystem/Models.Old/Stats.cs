using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PacificHubMarketIntelligenceSystem.Models
{
    public class Stats
    {
        public bool Contains_updates { get; set; }
        public int Nodes_created { get; set; }
        public int Nodes_deleted { get; set; }
        public int Properties_set { get; set; }
        public int Relationships_created { get; set; }
        public int Relationship_deleted { get; set; }
        public int Labels_added { get; set; }
        public int Labels_removed { get; set; }
        public int Indexes_added { get; set; }
        public int Indexes_removed { get; set; }
        public int Contraints_added { get; set; }
        public int Contraints_removed { get; set; }
    }
}