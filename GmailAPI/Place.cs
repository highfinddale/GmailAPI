using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualBasic.FileIO;
using Google.Apis.Customsearch.v1.Data;

namespace GmailAPI
{
    public static class Place
    {
        public static string PLACE_BASE = "https://maps.googleapis.com/maps/api/place/";
        public static string SEARCH_BASE = "https://www.googleapis.com/customsearch/v1?"; 
        public static string RADAR_URL = PLACE_BASE + "radarsearch/json?location={},{}&radius={}&types={}&key={}";
        public static string NEARBY_URL = PLACE_BASE + "nearbysearch/json?location={},{}&radius={}&types={}&key={}";
        public static string DETAIL_URL = PLACE_BASE + "details/json?placeid={0}&key={1}&fields=name,formatted_address,geometry,place_id";
        public static string PLACE_AUTOCOMPLETE = PLACE_BASE + "autocomplete/json?radius=100&strictbound=true&input={0}&key={1}";
        public static string PLACE_KEY="Place:PlaceAPIKey";
        public static string SEARCH_ID = "SearchEngineId";
        public static string SEARCH_URL = SEARCH_BASE + "key={0}&cx={1}&q={2}&prettyPrint=true&tbm=map"; 
        public static readonly HttpClient client = new HttpClient();
        public static readonly IConfigurationRoot config = new ConfigurationBuilder()
              .SetBasePath(Environment.CurrentDirectory).AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .AddUserSecrets(Assembly.Load(new AssemblyName("GmailAPI")))
              .Build();

        public static readonly MemoryCache memcache = new MemoryCache(new MemoryCacheOptions());
       // [FunctionName("Place")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
           log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // get the place ID from search  
            // get the radar map to get nearby places 
            // for each nearby place get the poputartime 
            string cx = config[SEARCH_ID]; 
            var result =await memcache.GetOrCreateAsync<string>("Ratnadeep SuperMarket", entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(30); 
                return getIdSearch("Ratnadeep SuperMarket", config, log);
            }); 
            var loc = await PopularTimeSearch(result, config,log); 
            
        }

        public static async Task<List<int>> PopularTimeSearch(string q, IConfigurationRoot config, ILogger log)
        {
            var detailsUrl = string.Format(SEARCH_URL, config[PLACE_KEY], config[SEARCH_ID], q);
            string match = "";
            try
            {
                var result = await client.GetAsync(detailsUrl);
                result.EnsureSuccessStatusCode();
                var res = await result.Content.ReadAsAsync<Search>();
            }
            catch (HttpRequestException ex)
            {
                log.LogInformation($"Http call failed with exception {ex.Message}");

            }
            return new List<int>();

        }

        public static async Task<string> getIdSearch(string input, IConfigurationRoot config,  ILogger log)
        {
            var placeSearchUrl = string.Format(PLACE_AUTOCOMPLETE, input, config[PLACE_KEY]);
            string match = ""; 
            try
            {
                var result = await client.GetAsync(placeSearchUrl);
                result.EnsureSuccessStatusCode();
                var res = await result.Content.ReadAsAsync<AutoCompleteResponse>();
                return res.Predictions.FirstOrDefault().PlaceId; 
                
            }
            catch (HttpRequestException ex)
            {
                log.LogInformation($"Http call failed with exception {ex.Message}");

            }
            return match; 
        }
   
        public static async Task<List<int>> GetLocationDetails(string location_id, IConfigurationRoot config, ILogger log)
        {
            var detailsUrl = string.Format(DETAIL_URL, location_id, config[PLACE_KEY]);
            string match = "";
            try
            {
                var result = await client.GetAsync(detailsUrl);
                result.EnsureSuccessStatusCode();
                var res = await result.Content.ReadAsAsync<LocationDetailsResponse>();
            }
            catch (HttpRequestException ex)
            {
                log.LogInformation($"Http call failed with exception {ex.Message}");

            }
            return new List<int>();
        }
    }
}
