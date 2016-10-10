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
            Subscription newSub = new Subscription {Title = sub.Title, Uri = sub.Uri};
            User u = new User {Name = sub.User, LastFetched = new DateTimeOffset(1970,1,1,0,0,0,TimeSpan.Zero)};

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
                .Return((u, s) => new
                {
                    User = u.As<User>(),
                    Subscriptions = s.As<Subscription>()
                }).ResultsAsync;

            var subs = query;
            //IList<SyndicationItem> feeds = new List<SyndicationItem>();
            //http://www.wadewegner.com/2011/11/aggregating-rss-feeds-in-c-and-asp-net-mvc-3/
            //https://blogs.msdn.microsoft.com/steveres/2008/01/20/using-syndicationfeed-to-display-photos-from-spaces-live-com/
            
            SyndicationFeed mainFeed = new SyndicationFeed();
            List<Feed> Feeds = new List<Feed>();

            foreach (var sub in subs)
            {
                SyndicationFeed feed;
                using (XmlReader r = XmlReader.Create(sub.Subscriptions.Uri))
                {
                    feed = SyndicationFeed.Load(r);
                }
                if (feed != null)
                {
                    foreach (var f in feed.Items)
                    {
                        if (f.PublishDate > sub.User.LastFetched)
                        {
                            var tempFeed = new Feed
                            {
                                Author = f.Authors.FirstOrDefault().Email,
                                Description = f.Summary.Text,
                                PubDate = f.PublishDate,
                                Title = f.Title.Text,
                                Url = f.Links.FirstOrDefault().Uri.AbsoluteUri,
                                Categories = f.Categories.Select(category => category.Name).ToList()
                            };
                            Feeds.Add(tempFeed);

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
                    //SyndicationFeed tempFeed = new SyndicationFeed(
                    //    mainFeed.Items.Union(feed.Items).OrderByDescending(i => i.PublishDate));
                    //mainFeed = tempFeed;
                }
            }

            //update last fetched
            var tempUser = await WebApiConfig.GraphClient.Cypher
                .Match("(u:User)")
                .Where((User u) => u.Name == user)
                .Set("u.LastFetched = {currentTime}")
                .WithParam("currentTime", DateTime.Now)
                .Return(u => u.As<User>())
                .ResultsAsync;
            //http://stackoverflow.com/questions/10042849/how-to-sort-feed-by-category-if-im-using-syndication-in-windows-phone-7
            //var orderedFeeds = feeds.OrderByDescending(x => x.PublishDate);
            return Ok(Feeds.OrderByDescending(i => i.PubDate));
        }
    }

    public class Subscription
    {
        public string Title { get; set; }
        public string Uri { get; set; }
    }

    public class Subscriptions
    {
        public IEnumerable<Subscription> SubscriptionsList { get; set; }
    }

    public class User
    {
        public string Name { get; set; }
        public DateTimeOffset LastFetched { get; set; }
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
