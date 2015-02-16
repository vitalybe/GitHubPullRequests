using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using EasyHttp.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHubPullRequests
{
    static class GitHub
    {

        private static readonly string ApiRoot = AppSettings.Default.ApiRoot + AppSettings.Default.GitHubRepo;
        private static readonly string AuthorizationValue = "token " + AppSettings.Default.Token;

        
        private static dynamic HttpGetQueryGitHub(string requestUrl)
        {
            var client = PrepareForGitHubCall(null);
            var result = client.DownloadString(requestUrl);
            return JsonConvert.DeserializeObject(result);
        }

        private static HttpResponse HttpPostQueryGitHub(string requestUrl, dynamic body)
        {
            var http = new HttpClient();
            http.Request.AddExtraHeader("Authorization", AuthorizationValue);
            http.Request.Accept = "application/json";

            return http.Post(requestUrl, body, HttpContentTypes.ApplicationJson);
        }


        private static WebClient PrepareForGitHubCall(Dictionary<string, string> additionalHeaders)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            WebClient client = new WebClient();
            // TODO: Configurable
            client.Headers.Add("Authorization", AuthorizationValue);
            client.Headers.Add("Accept", "application/json");
            if (additionalHeaders != null)
            {
                foreach (var additionalHeader in additionalHeaders)
                {
                    client.Headers.Add(additionalHeader.Key, additionalHeader.Value);
                }
            }
            return client;
        }

        public static dynamic GetPullRequestData(int pullRequestId)
        {
            return GetPullRequestData(string.Format("{0}/pulls/{1}", ApiRoot, pullRequestId));
        }


        public static dynamic GetPullRequestData(string pullRequestUrl)
        {
            var prData = HttpGetQueryGitHub(pullRequestUrl);

            return prData;
        }

        public static IEnumerable<dynamic> OpenPullRequests()
        {
            var prData = HttpGetQueryGitHub(ApiRoot + "/pulls");

            return prData;
        }

        public static IEnumerable<dynamic> GetActivity(string etag, int lastId)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("If-None-Match", "\""+etag+"\"");

            var client = PrepareForGitHubCall(headers);
            string responseData = null;
            try
            {
                responseData = client.DownloadString(ApiRoot+"/events");
            }
            catch (WebException e)
            {
                var response = (HttpWebResponse)(e.Response);
                if (response.StatusCode != HttpStatusCode.NotModified)
                {
                    throw;
                }
            }

            if (responseData != null)
            {
                var eventsData = (JArray)JsonConvert.DeserializeObject(responseData);
                // We want to go from oldest to newset
                var eventsDataEnumerable = eventsData.Reverse();
                foreach (dynamic eventData in eventsDataEnumerable)
                {
                    if (lastId < int.Parse(eventData.id.Value))
                    {
                        dynamic result = new ExpandoObject();
                        result.data = eventData;
                        result.etag = client.ResponseHeaders["ETag"];

                        yield return result;
                    }
                }
            }
        }

        public static string GetFileCode(string file, string commitId)
        {
            var getContentQuery = ApiRoot + string.Format("/contents/{0}?ref={1}", file, commitId);

            dynamic contentsQueryResult = HttpGetQueryGitHub(getContentQuery);
            byte[] data = Convert.FromBase64String(contentsQueryResult.content.Value    );
            string fileContents = Encoding.UTF8.GetString(data);

            return fileContents;
        }

        public static string FindPullRequestByCommit(string commitHash)
        {
            Log.Write("Searching for commit: " + commitHash);
            dynamic openPullRequests = HttpGetQueryGitHub(ApiRoot + "/pulls");
            foreach (var openPullRequest in openPullRequests)
            {
                Log.Write("\tSearching in PR: " + openPullRequest.id);
                var prCommitsData = HttpGetQueryGitHub(openPullRequest.commits_url.Value);
                foreach (var prCommit in prCommitsData)
                {
                    if (prCommit.sha == commitHash)
                    {
                        return openPullRequest.url;
                    }
                }
            }

            return null;
        }

        public static HttpResponse CreatePullRequest(string title, string headBranch, string baseBranch, string body)
        {
            dynamic customer = new ExpandoObject(); // Or any dynamic type
            customer.title = title;
            customer.head = headBranch;
            ((IDictionary<string, object>)customer).Add("base", baseBranch); 
            customer.body = body;

            return HttpPostQueryGitHub(ApiRoot + "/pulls", customer);
        }
    }
}