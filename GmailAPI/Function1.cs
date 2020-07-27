using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Google.Apis.Gmail.v1;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Google.Apis.Services;
using System.Threading;
using Google.Apis.Util.Store;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.WebUtilities;
using MimeKit;
using HtmlAgilityPack;

namespace GmailAPI
{
    public static class Function1
    {
        public static string CLIENT_ID = "GSuiteClientId";
        public static string CLIENT_GRANTTYPE = "GSuiteClientGrantType";
        public static string CLIENT_KEY = "GSuite_Key";
        public static string OAUTH_ENDPOINT = "https://accounts.google.com/o/oauth2/v2/auth";
        public static string OAUTH_TOEKN_ENDPOINT = "https://oauth2.googleapis.com/token";
        public static string CLIENT_REDIRECT_URL = "GSuiteRedirectUrl";
        public static IConfiguration config = new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets(Assembly.Load(new AssemblyName("GmailAPI")))
                .Build();

        public static string WEEKLY_TITLE_LEETCODE = "Weekly Digest";
        public static string WEEKLY_HACKRANK = "";
        public static string LEET_CODE_Q = "from:no-reply@leetcode.com";
        public static string HACK_Q = "from:no-reply@hackerrankmail.com"; 
        public static readonly HttpClient client = new HttpClient();
        public static readonly GmailService gmailClient = GetGmailService(GetCredenetial("Resources/client_secret_installed.json"));

        [FunctionName("GMailReader")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // for each mail if title is "weekly digest" get the title link and body 
            ConcurrentDictionary<String, String> weekly = new ConcurrentDictionary<string, string>();
            Dictionary<String, List<String>> dict = await GetMails(new List<string>() { LEET_CODE_Q, HACK_Q }); 
            dict.Values.ToList().ForEach(e =>
                  e.GetWeeklyQuestions().ToList().ForEach(a => weekly.Append(a))); 
        }


        public  static async Task<Dictionary<string,List<string>>> GetMails(List<String> mailsQuery)
        {
            var mailRequest = gmailClient.Users.Messages.List("me");
            ConcurrentDictionary<string, ConcurrentBag<string>> allMails = new ConcurrentDictionary<string, ConcurrentBag<string>>(); 
            mailRequest.PrettyPrint = true;
            await Task.WhenAll(mailsQuery.Select(async q => {
                mailRequest.Q = q;
                var res = await mailRequest.ExecuteAsync();
                ConcurrentBag<string> mails = new ConcurrentBag<string>();
                await Task.WhenAll(res.Messages.Select(async e =>
                {
                    var getRequest = gmailClient.Users.Messages.Get("me", e.Id);
                    getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                    var message = (await getRequest.ExecuteAsync());
                    mails.Add((await MimeMessage.LoadAsync(new MemoryStream(Base64UrlTextEncoder.Decode(message.Raw)))).HtmlBody.Trim());
                }));
                allMails.GetOrAdd(q, mails); 
            }));

            return allMails.ToDictionary(key => key.Key, value => value.Value.ToList()); 
        }

        public static Dictionary<string,string> GetWeeklyQuestions(this List<string> mails)
        {
            Dictionary<string, string> messageLinks = new Dictionary<string, string>();
            mails.ToList().ForEach(mail =>
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(mail);
                string title = htmlDocument.DocumentNode.SelectSingleNode("//title")?.InnerText;
                if (string.Equals(title, WEEKLY_TITLE_LEETCODE, StringComparison.CurrentCultureIgnoreCase))
                {
                    htmlDocument.DocumentNode.SelectNodes("//a").ToList().ForEach(a =>
                    {
                        // extract top pick title 
                        string title = a.SelectSingleNode("//div[contains(@class,'top-pick-title')]").InnerText.Trim();
                        string message = a.SelectSingleNode("//div[contains(@class,'top-pick-content')]").InnerText.Trim();
                        string link = a.Attributes.Where(e => e.Name == "href").FirstOrDefault().Value.Trim();
                        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(link) && !messageLinks.ContainsKey(link) && !messageLinks.ContainsValue($"{ title}:{message}"))
                            messageLinks.Add(link, $"{ title}:{message}");
                    });
                };

            });
            return messageLinks;

        }

        private static async Task<string> GenerateGSuiteToken(IConfigurationRoot allvariable, ILogger log, string GsuiteSecret)
        {
            // with current cred do a auth 
            try
            {
                GsuiteSecret ??= (string) allvariable["Gmail.ClientSecret"];
                var code_url = $"{OAUTH_ENDPOINT}?" +
                    $"scope={allvariable[CLIENT_GRANTTYPE]}" +
                    $"&access_type=offline" +
                    $"&response_type=code" +
                    $"&client_id={allvariable[CLIENT_ID]}" +
                    $"&redirect_uri={allvariable[CLIENT_REDIRECT_URL]}";
                var response = await client.GetAsync(code_url);
                
                response.EnsureSuccessStatusCode();
                string authcode = HttpUtility.ParseQueryString(await response.Content.ReadAsStringAsync()).Get("code");
                var token_url = $"{OAUTH_TOEKN_ENDPOINT}?" +
                    $"code={authcode}" +
                    $"&grant_type=authorization_code" +
                    $"&client_id={allvariable[CLIENT_ID]}" +
                    $"&client_secret={GsuiteSecret}";
                client.DefaultRequestHeaders.Add("content-type", "application/x-www-form-urlencoded");
                response = (await client.PostAsync(token_url, null));
                response.EnsureSuccessStatusCode();
                return (await response.Content.ReadAsAsync<TokenResponse>()).Token; 
            }
            catch(HttpRequestException ex)
            {
                log.LogInformation($"Http call failed with exception {ex.Message}");
            }

            return null; 
        }



        public static GoogleCredential GetServiceCredenetial(string serviceAccountCredentialJsonFilePath)
        {
            GoogleCredential credential;

            using (var stream = new FileStream(serviceAccountCredentialJsonFilePath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(new[] { GmailService.Scope.GmailReadonly })
                    .CreateWithUser("highfinddale@gmail.com");
            }
            return credential;
        }

        public static UserCredential GetCredenetial(string clientSecretCredPath)
        {
            string credPath = "token.json";
            UserCredential credential; 
            using (var stream =
                new FileStream(clientSecretCredPath, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                 credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { GmailService.Scope.GmailReadonly },
                    "highfinddale@gmail.com",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            return credential; 
        }

        public static GmailService GetGmailService(UserCredential credential)
        {
            return new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Automation App",
            });
        }


    }
}
