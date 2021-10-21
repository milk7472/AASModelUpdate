using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace AASModelUpdate
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var task = await CallRefreshAsync();
            Console.WriteLine(task);
            Console.ReadLine();

            return;
        }

        private static async Task<string> CallRefreshAsync()
        {
            HttpClient client = new HttpClient();
            // AAS is located in southeast asia
            //client.BaseAddress = new Uri("https://<rollout>.asazure.windows.net/servers/<serverName>/models/<resource>/");
            client.BaseAddress = new Uri("https://southeastasia.asazure.windows.net/servers/ServerName/models/AAS_Model_Name/");

            // Send refresh request
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await UpdateToken());

            RefreshRequest refreshRequest = new RefreshRequest()
            {
                type = "full",
                maxParallelism = 10
            };

            HttpResponseMessage response = await client.PostAsJsonAsync("refreshes", refreshRequest);
            response.EnsureSuccessStatusCode();
            Uri location = response.Headers.Location;
            Console.WriteLine(response.Headers.Location);

            int updateTime = 1000 * 60 * 1;
            // Check the response
            while (true) // Will exit while loop when exit Main() method (it's running asynchronously)
            {
                string output = "";
                string status = "";

                // Refresh token if required
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await UpdateToken());

                response = await client.GetAsync(location);
                if (response.IsSuccessStatusCode)
                {
                    output = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(output);
                    status = json.GetValue("status").ToString();
                    if ("succeeded" == status)
                    {
                        Console.WriteLine("Model update succeeded!");
                        break;
                    }
                }

                Console.Clear();
                Console.WriteLine(output);

                Thread.Sleep(updateTime);
            }

            return "Done";
        }

        private static async Task<string> UpdateToken()
        {
            string tenantId = "tenant id";
            string appId = "app id that has authority for AAS";
            string appSecret = "app key";
            string authorityUrl = "https://login.microsoftonline.com/";
            string aasUrl = "https://southeastasia.asazure.windows.net";

            authorityUrl = String.Format("{0}{1}", authorityUrl, tenantId);

            AuthenticationContext authContext = new AuthenticationContext(authorityUrl);
            AuthenticationResult authenticationResult = null;

            try
            {
                // Config for OAuth client credentials 
                authContext = new AuthenticationContext(authorityUrl);

                var clientCred = new ClientCredential(appId, appSecret);
                authenticationResult = await authContext.AcquireTokenAsync(aasUrl, clientCred);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return authenticationResult.AccessToken;
        }

        class RefreshRequest
        {
            public string type { get; set; }
            public int maxParallelism { get; set; }
        }
    }
}
