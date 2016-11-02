using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Neo4jClient;
using Neo4jClient.Cypher;
using PacificHubMarketIntelligenceSystem.Models;

namespace PacificHubMarketIntelligenceSystem.Controllers
{
    [RoutePrefix("api/graph")]
    public class GraphController : ApiController
    {
        //[HttpGet]
        //[Route("queryKeyword")]
        //public async Task<IHttpActionResult> QueryKeyword(string keyword)
        //{
        //    var results = await WebApiConfig.GraphClient.Cypher
        //        .OptionalMatch("(n:NewsFeed)-[r:TAGGED_AS]->(t:Tag)")
        //        .Where((Tag t) => t.Value == keyword)
        //        .Return((n, t, r) => new
        //        {
        //            n = n.CollectAs<Node<NewsFeed>>(),
        //            t = t.As<Node<Tag>>(),
        //            r = r.As<IEnumerable<string>>()
        //        }).ResultsAsync;
        //    return Ok(results);
        //}

        [HttpPost]
        [Route("clearDatabase")]
        public async Task<IHttpActionResult> ClearDatabase()
        {
            await WebApiConfig.GraphClient.Cypher
                .Match("(n)")
                .DetachDelete("n")
                .ExecuteWithoutResultsAsync();
            return Ok();
        }

        //http://geekswithblogs.net/cskardon/archive/2013/07/23/neo4jclient-ndash-getting-path-results.aspx
        [HttpGet]
        [Route("queryKeyword")]
        public async Task<IHttpActionResult> QueryKeyword(string keyword)
        {
            var tag = await WebApiConfig.GraphClient.Cypher
                .Match("(t:Tag)")
                .Where((Tag t) => t.Value == keyword)
                .Return(t => t.As<Node<Tag>>())
                .ResultsAsync;

            NodeReference<Tag> tagReference = tag.FirstOrDefault().Reference;

            ICollection<PathsResult<Tag, TaggedAs>> paths = WebApiConfig.GraphClient.Paths<Tag, TaggedAs>(tagReference);

            return Ok(paths);
        }
    }

    public class PathsResult<TNode, TRelationship> where TRelationship : Relationship, new()
    {
        public IEnumerable<Node<TNode>> Nodes { get; set; }
        public IEnumerable<RelationshipInstance<TRelationship>> Edges { get; set; }
    }

    public static class Neo4JClientExtensions
    {
        public static ICollection<PathsResult<TNode, TRelationship>> Paths<TNode, TRelationship>(this IGraphClient client, NodeReference<TNode> rootNode, int levels = 1)
            where TRelationship : Relationship, new()
        {
            ICypherFluentQuery<PathsResult<TNode, TRelationship>> pathsQuery = client.Cypher
                .Start(new { n = rootNode })
                .Match(string.Format("p = ((n) <-[:{0}*1..{1}]-())", new TRelationship().RelationshipTypeKey, levels))
                .Return(p => new PathsResult<TNode, TRelationship>
                {
                    Nodes = Return.As<IEnumerable<Node<TNode>>>("nodes(p)"),
                    Edges = Return.As<IEnumerable<RelationshipInstance<TRelationship>>>("rels(p)")
                });

            return pathsQuery.Results.ToList();
        }
    }

    public class AlchemyNode
    {
       // public Node<T> Data { get; set; }
        public string Type { get; set; }
        public int Id { get; set; }
    }

    public class AlchemyRelationship
    {
        public int Source { get; set; }
        public int Target { get; set; }
        public string Caption { get; set; }
    }
}
