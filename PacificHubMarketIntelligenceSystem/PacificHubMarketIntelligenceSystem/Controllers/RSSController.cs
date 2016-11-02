using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Neo4jClient;

namespace PacificHubMarketIntelligenceSystem.Controllers
{
    [RoutePrefix("api/rss")]
    public class RSSController : ApiController
    {
        [HttpPost]
        [Route("subscribe")]
        public async Task<IHttpActionResult> Subscribe(NewSubscription sub)
        {
            Subscription newSub = new Subscription {Title = sub.Title, Uri = sub.Uri, LastFetched = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero) };
            User u = new User {Name = sub.User};

            //create User
            await WebApiConfig.GraphClient.Cypher
                    .Merge("(u:User { Name: {n} })")
                    .OnCreate()
                    .Set("u = {newUser}")
                    .WithParams(new
                    {
                        n = u.Name,
                        newUser = u
                    })
                    .ExecuteWithoutResultsAsync();

            //create subscription
            await WebApiConfig.GraphClient.Cypher
                    .Merge("(s:Subscription { Uri: {u} })")
                    .OnCreate()
                    .Set("s = {subs}")
                    .WithParams(new
                    {
                        u = newSub.Uri,
                        subs = newSub
                    })
                    .ExecuteWithoutResultsAsync();

            //create relationship
            //TODO move lastFetched variable to relationship so that new news sources can benefit 
            await WebApiConfig.GraphClient.Cypher
                .Match("(user:User)", "(subscription:Subscription)")
                .Where((User user) => user.Name == u.Name)
                .AndWhere((Subscription subscription) => subscription.Uri == newSub.Uri)
                .CreateUnique("(user)-[:SUBSCRIBED_TO]->(subscription)")
                .ExecuteWithoutResultsAsync();

            return Ok();
        }

        [HttpGet]
        [Route("fetch")]
        public async Task<IHttpActionResult> Fetch(string user)
        {
            var query = await WebApiConfig.GraphClient.Cypher
                .Match("(u:User)", "(s:Subscription)")
                .Where((User u) => u.Name == user)
                .AndWhere("(u)-[:SUBSCRIBED_TO]->(s)")
                .Return(s => s.As<Subscription>())
                .ResultsAsync;
                //.Return((u, s) => new
                //{
                //    User = u.As<User>(),
                //    Subscriptions = s.As<Subscription>()
                //}).ResultsAsync;

            var subs = query;

            //http://www.wadewegner.com/2011/11/aggregating-rss-feeds-in-c-and-asp-net-mvc-3/
            //https://blogs.msdn.microsoft.com/steveres/2008/01/20/using-syndicationfeed-to-display-photos-from-spaces-live-com/
            
            SyndicationFeed mainFeed = new SyndicationFeed();
            List<Feed> feeds = new List<Feed>();

            foreach (var sub in subs)
            {
                SyndicationFeed feed;
                using (XmlReader r = XmlReader.Create(sub.Uri))
                {
                    feed = SyndicationFeed.Load(r);
                }
                if (feed != null)
                {
                    foreach (var f in feed.Items)
                    {
                        if (f.PublishDate > sub.LastFetched)
                        {
                            var tempFeed = new Feed
                            {
                                Author = f.Authors.FirstOrDefault()?.Email ?? f.Authors.FirstOrDefault()?.Name,
                                Description = f.Summary.Text,
                                PubDate = f.PublishDate,
                                Title = f.Title.Text,
                                Url = f.Links.FirstOrDefault()?.Uri.AbsoluteUri,
                                Categories = f.Categories.Select(category =>category.Name).ToList()
                            };

                            if (f.Categories.Select(category => category.Name).ToList().Count < 1)
                            {
                                tempFeed.Categories = await GetKeywords(f.Links.FirstOrDefault()?.Uri.AbsoluteUri);
                            }

                            feeds.Add(tempFeed);

                            //TODO create a categoriser

                            await new NewsFeedController().AddFeed(new InputNewsFeed
                            {
                                Author = tempFeed.Author,
                                Description = tempFeed.Description,
                                PubDate = tempFeed.PubDate,
                                Title = tempFeed.Title,
                                Url = tempFeed.Url,
                                Tags = tempFeed.Categories
                            });
                        }
                    }
                }

                var tempResult = await WebApiConfig.GraphClient.Cypher
                    .Match("(s:Subscription)")
                    .Where((Subscription s) => s.Uri == sub.Uri)
                    .Set("s.LastFetched = {currentTime}")
                    .WithParam("currentTime", DateTime.Now)
                    .Return(s => s.As<Subscription>())
                    .ResultsAsync;
            }

            //update last fetched
            //var tempUser = await WebApiConfig.GraphClient.Cypher
            //    .Match("(u:User)")
            //    .Where((User u) => u.Name == user)
            //    .Set("u.LastFetched = {currentTime}")
            //    .WithParam("currentTime", DateTime.Now)
            //    .Return(u => u.As<User>())
            //    .ResultsAsync;

            //http://stackoverflow.com/questions/10042849/how-to-sort-feed-by-category-if-im-using-syndication-in-windows-phone-7
            //var orderedFeeds = feeds.OrderByDescending(x => x.PublishDate);
            return Ok(feeds.OrderByDescending(i => i.PubDate));
        }

        [HttpGet]
        [Route("subscriptions")]
        public async Task<IHttpActionResult> GetSubscriptions(string user)
        {
            var query = await WebApiConfig.GraphClient.Cypher
                .Match("(u:User)", "(s:Subscription)")
                .Where((User u) => u.Name == user)
                .AndWhere("(u)-[:SUBSCRIBED_TO]->(s)")
                .Return(s => s.As<Subscription>())
                .ResultsAsync;
            return Ok(query.ToList());
        }

        [HttpPost]
        [Route("deleteSubscription")]
        public async Task<IHttpActionResult> DeleteSubscription(string uri)
        {
            await WebApiConfig.GraphClient.Cypher
                .OptionalMatch("(s:Subscription)<-[r]-()")
                .Where((Subscription s) => s.Uri == uri)
                .Delete("s,r")
                .ExecuteWithoutResultsAsync();

            return Ok();
        }

        //http://stackoverflow.com/questions/16738421/parse-html-meta-keywords-using-regex
        private async Task<List<string>> GetKeywords(string url)
        {
            List<string> keywords = null;
            using (WebClient client = new WebClient())
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(await client.DownloadStringTaskAsync(new Uri(url)));
                HtmlNode node = doc.DocumentNode.SelectSingleNode("//meta[@name='keywords']");
                if (node != null)
                {
                    keywords = doc.DocumentNode
                        .SelectSingleNode("//meta[@name='keywords']")
                        .Attributes["content"].Value
                        .Split(',').ToList();
                }
            }
            return keywords;
        }
    }

    public class Subscription
    {
        public string Title { get; set; }
        public string Uri { get; set; }
        public DateTimeOffset LastFetched { get; set; }
    }

    public class Subscriptions
    {
        public IEnumerable<Subscription> SubscriptionsList { get; set; }
    }

    public class User
    {
        public string Name { get; set; }
    }

    public class NewSubscription
    {
        public string User { get; set; }
        public string Title { get; set; }
        public string Uri { get; set; }
    }

    public class Feed
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public DateTimeOffset PubDate { get; set; }
        public IEnumerable<string> Categories { get; set; }
    }
}
