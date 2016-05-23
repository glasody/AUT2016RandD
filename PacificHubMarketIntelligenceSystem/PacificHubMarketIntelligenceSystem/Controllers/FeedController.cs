using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PacificHubMarketIntelligenceSystem.Controllers
{
    public class FeedController : ApiController
    {
        public IHttpActionResult GetFeedByTitle(string title)
        {
            var data = WebApiConfig.GraphClient.Cypher
                .Match("(feed:Feed {title:{title})")
                //.OptionalMatch("(movie)<-[r]-(person:Person)")
                .WithParam("title", title)
                .Return((feed) => new
                {
                    feed = feed.As<Feed>().title
                })
                .Limit(1)
                .Results.FirstOrDefault();

            var result = new FeedResult();
            result.title = data.feed;

            //example for calling related data onto the feed result
            //var castresults = new List<CastResult>();
            //foreach (var item in data.cast)
            //{
            //    var tempData = JsonConvert.DeserializeObject<dynamic>(item);
            //    var roles = tempData[2] as JArray;
            //    var castResult = new CastResult
            //    {
            //        name = tempData[0],
            //        job = tempData[1],
            //    };
            //    if (roles != null)
            //    {
            //        castResult.role = roles.Select(c => c.Value<string>());
            //    }
            //    castresults.Add(castResult);
            //}
            //result.cast = castresults;

            return Ok(result);
        }
    }

    public class FeedResult
    {
        public string title { get; set; }
        public string author { get; set; }
        public IEnumerable<string> summary { get; set; }
        public IEnumerable<string> keywords { get; set; }
        public DateTime published { get; set; }
        public string originId { get; set; }
    }
}
