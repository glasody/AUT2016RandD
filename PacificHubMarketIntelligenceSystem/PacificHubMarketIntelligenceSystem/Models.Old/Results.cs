using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PacificHubMarketIntelligenceSystem.Models
{
    public class Results
    {
        public IEnumerable<string> Columns { get; set; }
        public IEnumerable<Data> Data { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}