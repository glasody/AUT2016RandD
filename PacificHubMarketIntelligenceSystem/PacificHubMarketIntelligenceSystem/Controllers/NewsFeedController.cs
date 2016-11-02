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

            var feedResult = await WebApiConfig.GraphClient.Cypher
                .Merge("(newsfeed:NewsFeed {Url: {u}})")
                .OnCreate()
                .Set("newsfeed = {t}")
                .WithParams(new
                {
                    u = tempFeed.Url,
                    t = tempFeed
                })
                .Return<Node<NewsFeed>>("newsfeed")
                .ResultsAsync;

            NodeReference<NewsFeed> feedReference = feedResult.FirstOrDefault().Reference;

            if (feed.Tags != null)
            {
                foreach (var tag in feed.Tags)
                {
                    var tagUpper = tag.ToUpper();

                    var tagResult = await WebApiConfig.GraphClient.Cypher
                        .Merge("(t:Tag {Value : {v}})")
                        .OnCreate()
                        .Set("t = {newTag}")
                        .WithParams(new
                        {
                            v = tagUpper,
                            newTag = new Tag { Value = tagUpper }
                        })
                        .Return<Node<Tag>>("t")
                        .ResultsAsync;

                    NodeReference<Tag> tagReference = tagResult.FirstOrDefault().Reference;

                    WebApiConfig.GraphClient.CreateRelationship(feedReference, new TaggedAs(tagReference));

                    //await WebApiConfig.GraphClient.Cypher
                    //    .Match("(n:NewsFeed)", "(t:Tag)")
                    //    .Where((NewsFeed n) => n.Url == feed.Url)
                    //    .AndWhere((Tag t) => t.Value == tagUpper)
                    //    .CreateUnique("(n)-[:TAGGED_AS]->(t)")
                    //    .ExecuteWithoutResultsAsync();
                }
            }

            //
            //http://geekswithblogs.net/cskardon/archive/2013/07/23/neo4jclient-ndash-getting-path-results.aspx
            //NodeReference<NewsFeed> feedReference = WebApiConfig.GraphClient.Create(tempFeed);

            //if (feed.Tags != null)
            //{
            //    foreach (var tag in feed.Tags)
            //    {
            //        var tagUpper = tag.ToUpper();
            //        NodeReference<Tag> tagReference = WebApiConfig.GraphClient.Create(new Tag() { Value = tagUpper });
            //        WebApiConfig.GraphClient.CreateRelationship(feedReference, new TaggedAs(tagReference));
            //    }
            //}

            return Ok();
        }

        [HttpGet]
        [Route("keywords")]
        public async Task<IHttpActionResult> GetKeyWords()
        {
            var query = await WebApiConfig.GraphClient.Cypher
                .Match("(t:Tag)")
                .Return(t => t.As<Tag>())
                .ResultsAsync;
            return Ok(query.OrderBy(t => t.Value));
        }

        //http://geekswithblogs.net/cskardon/archive/2013/07/23/neo4jclient-ndash-getting-path-results.aspx
        //[HttpGet]
        //[Route("queryKeyword")]
        //public async Task<IHttpActionResult> QueryKeyword(string keyword)
        //{
        //    var tag = new Tag() {Value = keyword};
        //    NodeReference<Tag> tagReference = WebApiConfig.GraphClient.Create(tag);

        //    ICollection<PathsResult<Tag, TaggedAs>> paths = WebApiConfig.GraphClient.Paths<Tag, TaggedAs>(tagReference);

        //    return Ok(paths);
        //}
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

    //http://geekswithblogs.net/cskardon/archive/2013/07/23/neo4jclient-ndash-getting-path-results.aspx
    public class TaggedAs : Relationship,
        IRelationshipAllowingSourceNode<NewsFeed>,
        IRelationshipAllowingTargetNode<Tag>
    {
        public TaggedAs() : base(-1)
        {
        }

        public TaggedAs(NodeReference targetNode) : base(targetNode)
        {
        }

        public TaggedAs(NodeReference targetNode, object data) : base(targetNode, data)
        {
        }

        public const string TypeKey = "TAGGED_AS";

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }

    //public class PathsResult<TNode, TRelationship> where TRelationship : Relationship, new()
    //{
    //    public IEnumerable<Node<TNode>> Nodes { get; set; }
    //    public IEnumerable<RelationshipInstance<TRelationship>> Relationships { get; set; }
    //}

    //public static class Neo4JClientExtensions
    //{
    //    public static ICollection<PathsResult<TNode, TRelationship>> Paths<TNode, TRelationship>(this IGraphClient client, NodeReference<TNode> rootNode, int levels = 1)
    //        where TRelationship : Relationship, new()
    //    {
    //        ICypherFluentQuery<PathsResult<TNode, TRelationship>> pathsQuery = client.Cypher
    //            .Start(new { n = rootNode })
    //            .Match(string.Format("p=n-[:{0}*1..{1}]->()", new TRelationship().RelationshipTypeKey, levels))
    //            .Return(p => new PathsResult<TNode, TRelationship>
    //            {
    //                Nodes = Return.As<IEnumerable<Node<TNode>>>("nodes(p)"),
    //                Relationships = Return.As<IEnumerable<RelationshipInstance<TRelationship>>>("rels(p)")
    //            });

    //        return pathsQuery.Results.ToList();
    //    }
    //}
}
