using Neo4jClient;
using PacificHubMarketIntelligenceSystem.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Neo4jClient.Cypher;

namespace PacificHubMarketIntelligenceSystem.Controllers
{
    [RoutePrefix("api/newsfeed")]
    public class NewsFeedController : ApiController
    {
        [HttpPost]
        [Route("addFeed")]
        public IHttpActionResult AddFeed(NewsFeed feed)
        {
            var url = ConfigurationManager.AppSettings["GraphDBUrl"];
            var user = ConfigurationManager.AppSettings["GraphDBUser"];
            var password = ConfigurationManager.AppSettings["GraphDBPassword"];
            var client = new GraphClient(new Uri(url), user, password);
            client.Connect();

            var addedFeed = client.Cypher
                .Create("(feed:Feed {feed})")
                .WithParam("feed", feed)
                .ExecuteWithoutResultsAsync();

            if (feed.Tags.Count > 0)
            {
                foreach (var tag in feed.Tags)
                {
                    var t = client.Cypher
                        .Merge("(keyword:Keyword {value : {tag}})")
                        .WithParam("tag", tag)
                        .ExecuteWithoutResultsAsync();
                    AddRelationship(client, "TAGGED_AS", feed, tag);
                }
            }

            return Ok(feed);
        }

        public void AddRelationship(GraphClient client, string relationship, NewsFeed feed, string tag)
        {
            var rel = client.Cypher
                .Match("(newFeed:Feed)", "(keyword:Keyword)")
                .Where((NewsFeed newFeed) => newFeed.Author.Equals(feed.Author))
                .AndWhere((Keyword keyword) => keyword.value.Equals(tag))
                .CreateUnique("(newFeed)-[:{relationship}]->(keyword)")
                .WithParam("relationship", relationship)
                .ExecuteWithoutResultsAsync();
        }

        [HttpGet]
        public IHttpActionResult GetAll()
        {
            var url = ConfigurationManager.AppSettings["GraphDBUrl"];
            var user = ConfigurationManager.AppSettings["GraphDBUser"];
            var password = ConfigurationManager.AppSettings["GraphDBPassword"];
            var client = new GraphClient(new Uri(url), user, password);
            client.Connect();

            var query = client.Cypher
                .Start(all = All.Nodes)
                .Return<object>()

            return Ok();
        }
    }

    public class Keyword
    {
        public string value { get; set; }
    }
}
