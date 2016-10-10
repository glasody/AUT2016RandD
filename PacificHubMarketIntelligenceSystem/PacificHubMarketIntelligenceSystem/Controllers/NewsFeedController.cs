using Neo4jClient;
using PacificHubMarketIntelligenceSystem.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web.Http;
using Neo4jClient.Cypher;
using Newtonsoft.Json;

namespace PacificHubMarketIntelligenceSystem.Controllers
{
    [RoutePrefix("api/newsfeed")]
    public class NewsFeedController : ApiController
    {
        [HttpPost]
        [Route("addFeed")]
        public async Task<IHttpActionResult> AddFeed(InputNewsFeed feed)
        {
            //http://stackoverflow.com/questions/19534511/how-to-create-a-node-with-neo4jclient-in-neo4j-v2
            //create newsfeed node
            var tempFeed = new NewsFeed()
            {
                Title = feed.Title,
                Url = feed.Url,
                Description = feed.Description,
                Author = feed.Author,
                PubDate = feed.PubDate
            };
            
            await WebApiConfig.GraphClient.Cypher
                .Merge("(newsfeed:NewsFeed {Url: {u}})")
                .OnCreate()
                .Set("newsfeed = {t}")
                .WithParams(new
                {
                    u = tempFeed.Url,
                    t = tempFeed
                })
                .ExecuteWithoutResultsAsync();

            foreach (var tag in feed.Tags)
            {
                await WebApiConfig.GraphClient.Cypher
                    .Merge("(t:Tag {Value : {v}})")
                    .OnCreate()
                    .Set("t = {newTag}")
                    .WithParams(new
                    {
                        v = tag,
                        newTag = new Tag { Value = tag}
                    })
                    .ExecuteWithoutResultsAsync();

                await WebApiConfig.GraphClient.Cypher
                    .Match("(n:NewsFeed)", "(t:Tag)")
                    .Where((NewsFeed n) => n.Url == feed.Url)
                    .AndWhere((Tag t) => t.Value == tag)
                    .CreateUnique("(n)-[:TAGGED_AS]->(t)")
                    .ExecuteWithoutResultsAsync();
            }

            ////http://stackoverflow.com/questions/34675334/is-there-a-way-to-add-multiple-nodes-with-the-net-neo4j-client
            ////create tag nodes
            //if (feed.Tags.Any())
            //{
            //    try
            //    {
            //        WebApiConfig.GraphClient.Cypher
            //            .Unwind(feed.Tags, "tag")
            //            .Create("(t:Tag)")
            //            .Set("t.Value = tag")
            //            .ExecuteWithoutResults();
            //    }
            //    catch(NeoException e) { }
            //    //create relationships
            //}

            return Ok();
        }

        //https://dzone.com/articles/neo4j-30-with-a-net-driver-neo4jclient
        //private void CreateUniqueRelationship(int person1Id, int person2Id, string relType, bool twoWay, GraphClient client)
        //{
        //    var query = client.Cypher
        //        .Match("(p1:Person)", "(p2:Person)")
        //        .Where((Person p1) => p1.Id == person1Id)
        //        .AndWhere((Person p2) => p2.Id == person2Id)
        //        .CreateUnique("(p1)-[:" + relType + "]->(p2)");

        //    if (twoWay)
        //        query = query.CreateUnique("(p1)<-[:" + relType + "]-(p2)");
        //
        //    query.ExecuteWithoutResults();
        //}

        [HttpGet]
        public async Task<IHttpActionResult> GetAll()
        {
            var query = await WebApiConfig.GraphClient.Cypher
                .Start(new {all = All.Nodes})
                .Return<object>("all")
                .ResultsAsync;
                //.Return((news, keyword) => new
                //{
                //    NewsFeed = news.As<NewsFeed>(),
                //    Keyword = keyword.As<Keyword>()
                //});

            return Ok(query);
        }
    }

    public class InputNewsFeed
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public DateTimeOffset PubDate { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }
}
