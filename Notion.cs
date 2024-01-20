using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ShopMakersManager.Models.MVPOS;
using ShopMakersManager.Models.Notion.Block;
using ShopMakersManager.Models.Notion.Database;
using ShopMakersManager.Utilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShopMakersManager
{
    public class Notion
    {
        private readonly SecretsManager _secretsManager;
        private readonly HttpClient _httpClient;

        public Notion(HttpClient httpClient, SecretManagerServiceClient secretManagerServiceClient)
        {
            _secretsManager = new SecretsManager(secretManagerServiceClient);
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.notion.com");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _secretsManager.GetSecret("notion-token", "1"));
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
        }

        public async Task<string> ImportSales(List<SaleItem> saleItems, string dbTitle)
        {
            var parentId = "f0c4045aa7484e1baddf79936c167ca1";

            var databaseProperties = new Dictionary<string, object>()
            {
                { "Sale Id", new { title = new object() } },
                { "Sale Date", new { date = new object() } }
            };

            var database = await CreateDatabase(parentId, dbTitle, databaseProperties);

            foreach (var item in saleItems)
            {
                var rowProperties = new Dictionary<string, object>()
                {
                    {
                        "Sale Id", new
                        {
                            title = new List<RichText>()
                            {
                                new() { Type = "text", Text = new Text() { Content = item.SaleId.ToString() } }
                            }
                        }
                    },
                    {
                        "Sale Date", new
                        {
                            date = new { start = item.SaleDate.ToString("s"), time_zone = "America/Vancouver" }
                        }
                    }
                };

                await AddDatabaseRow(database.Id, rowProperties);
            }

            return database.Url;
        }

        private async Task<Database> CreateDatabase(string parentId, string dbTitle, object properties)
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

        private async Task AddDatabaseRow(string databaseId, object properties)
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
