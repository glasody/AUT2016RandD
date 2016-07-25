﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PacificHubMarketIntelligenceSystem.Models
{
    public class NewsFeed
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public DateTime PubDate { get; set; }
        public List<string> Tags { get; set; }
    }
}