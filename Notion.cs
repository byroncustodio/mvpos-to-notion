using Google.Cloud.SecretManager.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MakersManager.Models.Notion.Block;
using MakersManager.Models.Notion.Database;
using MakersManager.Utilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Type;

namespace MakersManager
{
    public class Notion
    {
        private readonly SecretsManager _secretsManager;
        private readonly HttpClient _httpClient;

        public Notion(HttpClient httpClient, SecretManagerServiceClient secretManagerServiceClient)
        {
            _secretsManager = new SecretsManager(secretManagerServiceClient);

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_secretsManager.GetSecret("notion-base-url", "1"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _secretsManager.GetSecret("notion-token", "1"));
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
        }

        public async Task<JArray> QueryDatabase(string databaseId, object filter = null)
        {
            var data = string.Empty;

            if (filter != null)
            {
                data = JsonConvert.SerializeObject(new
                {
                    filter
                });
            }

            HttpRequestMessage httpRequest = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(string.Format("v1/databases/{0}/query", databaseId), UriKind.Relative),
                Content = new StringContent(data, System.Text.Encoding.UTF8, "application/json")
            };

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var message = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception(message);
            }

            string content = await httpResponse.Content.ReadAsStringAsync();
            dynamic deserializedContent = JsonConvert.DeserializeObject(content) ?? throw new Exception("Deserialized JSON resulted in null value.");
            return deserializedContent["results"];
        }

        public async Task<Database> CreateDatabase(string parentId, string dbTitle, object properties)
        {
            var data = new
            {
                parent = new Models.Notion.Parent.Page()
                {
                    Type = "page_id",
                    PageId = parentId
                },
                title = new List<RichText>
                {
                    new() 
                    { 
                        Type = "text",
                        Text = new Models.Notion.Block.Text()
                        {
                            Content = dbTitle
                        }
                    }
                },
                properties
            };

            HttpRequestMessage httpRequest = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("v1/databases", UriKind.Relative),
                Content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json")
            };

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var message = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception(message); 
            }

            string content = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Database>(content) ?? throw new Exception("Deserialized JSON resulted in null value.");
        }

        public async Task<Database> GetDatabase(string databaseId)
        {
            HttpRequestMessage httpRequest = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(string.Format("v1/databases/{0}", databaseId), UriKind.Relative)
            };

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var message = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception(message);
            }

            string content = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Database>(content) ?? throw new Exception("Deserialized JSON resulted in null value.");
        }

        public async Task AddDatabaseRow(string databaseId, object properties)
        {
            var data = new
            {
                parent = new Models.Notion.Parent.Database()
                {
                    Type = "database_id",
                    DatabaseId = databaseId
                },
                properties
            };

            HttpRequestMessage httpRequest = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("v1/pages", UriKind.Relative),
                Content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json")
            };

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var message = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception(message); 
            }
        }
    }
}
