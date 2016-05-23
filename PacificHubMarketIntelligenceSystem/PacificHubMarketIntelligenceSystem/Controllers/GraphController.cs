using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PacificHubMarketIntelligenceSystem.Controllers
{
    public class GraphController : ApiController
    {
        [HttpGet]
        [Route("{limit:int?}", Name = "getgraph")]
        public IHttpActionResult Index(int limit = 100)
        {
            var query = WebApiConfig.GraphClient.Cypher
                .Match("(f:Feed)")
                .Return((f) => new
                {
                    feed = f.As<Feed>().title
                })
                .Limit(limit);

            var data = query.Results.ToList();

            var nodes = new List<NodeResult>();
            var rels = new List<object>();
            //int i = 0, target;
            foreach (var item in data)
            {
                nodes.Add(new NodeResult {title = item.feed, label = "feed"});
                //add relations like so
                //target = i;
                //i++;
                //if (!string.IsNullOrEmpty(item.cast))
                //{
                //    var casts = JsonConvert.DeserializeObject<JArray>(item.cast);
                //    foreach (var cast in casts)
                //    {
                //        var source = nodes.FindIndex(c => c.title == cast.Value<string>());
                //        if (source == -1)
                //        {
                //            nodes.Add(new NodeResult { title = cast.Value<string>(), label = "actor" });
                //            source = i;
                //            i += 1;
                //        }
                //        rels.Add(new { source = source, target = target });
                //    }
                //}
            }

            return Ok(new { nodes = nodes, links = rels});
        }
    }

    public class NodeResult
    {
        public string title { get; set; }
        public string label { set; get; }
    }

    public class Feed
    {
        public string title { get; set; }
    }
}
