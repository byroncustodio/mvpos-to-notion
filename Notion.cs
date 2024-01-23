using Google.Cloud.SecretManager.V1;
using MakersManager.Models.Notion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MakersManager.Models.MVPOS;
using MakersManager.Models.Notion.Block;
using MakersManager.Models.Notion.Database;
using MakersManager.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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

        public async Task<List<SaleItem>> SetSaleItemRelations(List<SaleItem> saleItems)
        {
            var products = await GetDatabaseRows(_secretsManager.GetSecret("notion-products-id", "1"));

            foreach (var item in saleItems)
            {
                foreach (JObject productObj in products.Cast<JObject>())
                {
                    Product product = productObj.ToObject<Product>();

                    if (product.Properties.SKU.RichText[0].PlainText == item.Sku)
                    {
                        item.Product = product;
                        break;
                    }
                }
            }

            return saleItems;
        }

        public async Task<string> ImportSales(List<SaleItem> saleItems, string dbTitle)
        {
            var databaseProperties = new Dictionary<string, object>()
            {
                { "Sale Id", new { title = new object() } },
                { "Sale Date", new { date = new object() } },
                { "Location", 
                    new 
                    { 
                        select =  new 
                        { 
                            options = new List<object> 
                            { 
                                new { name = "Park Royal", color = "green" },
                                new { name = "Guildford", color = "blue" },
                                new { name = "Victoria", color = "yellow" }
                            }
                        }
                    }
                },
                //{ "SKU", new { rich_text = new object() } },
                { "Products", 
                    new 
                    { 
                        relation = new 
                        { 
                            database_id = _secretsManager.GetSecret("notion-products-id", "1"),
                            single_property = new { }
                        } 
                    } 
                },
                { "Payment",
                    new
                    {
                        select = new
                        {
                            options = new List<object>
                            {
                                new { name = "Credit", color = "blue" }
                            }
                        }
                    }
                },
                { "Quantity",
                    new
                    {
                        number = new
                        {
                            format = "number"
                        }
                    }
                },
                { "Subtotal",
                    new
                    {
                        number = new
                        {
                            format = "canadian_dollar"
                        }
                    }
                },
                { "Discount",
                    new
                    {
                        number = new
                        {
                            format = "percent"
                        }
                    }
                },
                { "Total",
                    new
                    {
                        number = new
                        {
                            format = "canadian_dollar"
                        }
                    }
                },
                { "Profit",
                    new
                    {
                        number = new
                        {
                            format = "canadian_dollar"
                        }
                    }
                }
            };

            var database = await CreateDatabase(_secretsManager.GetSecret("notion-reports-id", "2"), dbTitle, databaseProperties);

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
                    },
                    {
                        "Location", new
                        {
                            select = new { name = item.LocationName }
                        }
                    },
                    //{
                    //    "SKU", new
                    //    {
                    //        rich_text = new List<RichText>()
                    //        {
                    //            new() { Type = "text", Text = new Text() { Content = item.Sku ?? string.Empty } }
                    //        }
                    //    }
                    //},
                    {
                        "Products", new
                        {
                            relation = item.ProductRelation
                        }
                    },
                    {
                        "Payment", new
                        {
                            select = new { name = item.PaymentName }
                        }
                    },
                    {
                        "Quantity", new
                        {
                            number = item.Quantity
                        }
                    },
                    {
                        "Subtotal", new
                        {
                            number = item.SubTotal
                        }
                    },
                    {
                        "Discount", new
                        {
                            number = item.Discount / 100
                        }
                    },
                    {
                        "Total", new
                        {
                            number = item.Total
                        }
                    },
                    {
                        "Profit", new
                        {
                            number = item.Profit
                        }
                    }
                };

                await AddDatabaseRow(database.Id, rowProperties);
            }

            return database.Url;
        }

        private async Task<JArray> GetDatabaseRows(string databaseId)
        {
            HttpRequestMessage httpRequest = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(string.Format("v1/databases/{0}/query", databaseId), UriKind.Relative)
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
