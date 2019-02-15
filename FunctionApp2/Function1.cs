using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FunctionApp2
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task Run([TimerTrigger("0 15 19 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var q = await DualStrikeQuotes.DualStrike.GetAllSentencesAsync();
            int c = q.Count();
            string s = q.Skip(new Random().Next(0, c)).First();
            log.LogInformation(s);

            try
            {
                await PostToMastodon(s);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Could not post");
            }
        }

        private static async Task PostToMastodon(string s)
        {
            await Mastodon.Api.Statuses.Posting(
                Environment.GetEnvironmentVariable("MastodonDomain"),
                Environment.GetEnvironmentVariable("MastodonAccessToken"),
                s);
        }

        //private static async Task PostToTwitter(string s)
        //{
        //    Tweetinvi.Auth.SetUserCredentials(
        //        Environment.GetEnvironmentVariable("TwitterConsumerKey"),
        //        Environment.GetEnvironmentVariable("TwitterConsumerSecret"),
        //        Environment.GetEnvironmentVariable("TwitterTokenKey"),
        //        Environment.GetEnvironmentVariable("TwitterTokenSecret"));

        //    await Tweetinvi.TweetAsync.PublishTweet(s);
        //}
    }
}
