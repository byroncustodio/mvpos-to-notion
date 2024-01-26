using Google.Cloud.SecretManager.V1;
using Newtonsoft.Json;
using MakersManager.Models.MVPOS;
using MakersManager.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MakersManager
{
    public class MVPOS
    {
        private readonly SecretsManager _secretsManager;
        private readonly HttpClient _httpClient;
        private string _sessionCookie = string.Empty;

        public MVPOS(HttpClient httpClient, SecretManagerServiceClient secretManagerServiceClient)
        {
            _secretsManager = new SecretsManager(secretManagerServiceClient);

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_secretsManager.GetSecret("mvpos-base-url", "1"));
        }

        public async Task Login()
        {
            // Gets and stores session cookie
            await CreateSession();

            string endpoint = "api/v1/users/login";
            string queryParams = string.Format("email_address={0}&password={1}&password_reset_token=", _secretsManager.GetSecret("mvpos-user", "1"), _secretsManager.GetSecret("mvpos-password", "1"));

            HttpRequestMessage httpRequest = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(string.Join("?", endpoint, queryParams), UriKind.Relative),
                Headers =
                {
                    { HttpRequestHeader.Cookie.ToString(), _sessionCookie }
                }
            };

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode) 
            {
                var message = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception(message);
            }
        }

        private async Task CreateSession()
        {
            using HttpResponseMessage httpResponse = await _httpClient.GetAsync("/");
            if (!httpResponse.IsSuccessStatusCode) 
            {
                var message = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception(message); 
            }

            _sessionCookie = httpResponse.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value.First().Replace(" ", "").Split(";").Where(x => x.StartsWith("PHPSESSID")).First();
        }

        public async Task SetStoreLocation(StoreLocation location)
        {
            string endpoint = "api/v1/users/changeactiveclientlocation";

            List<KeyValuePair<string, string>> content = new() { new KeyValuePair<string, string>("client_location_id", ((int)location).ToString()) };

            HttpRequestMessage httpRequest = new()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri(endpoint, UriKind.Relative),
                Headers =
                {
                    { HttpRequestHeader.Cookie.ToString(), _sessionCookie }
                },
                Content = new FormUrlEncodedContent(content)
            };

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode) 
            {
                var message = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception(message);
            }
        }

        public async Task<SaleItems> GetSaleItemsByDateRange(DateTime from, DateTime to)
        {
            string endpoint = "api/v1/vendors/0/saleitems/date";
            string queryParams = "start_date=" + from.ToString("MM/dd/yyyy") + "&end_date=" + to.ToString("MM/dd/yyyy");

            HttpRequestMessage httpRequest = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(string.Join("?", endpoint, queryParams), UriKind.Relative),
                Headers =
                    {
                        { HttpRequestHeader.Cookie.ToString(), _sessionCookie }
                    }
            };

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var message = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception(message);
            }

            string content = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SaleItems>(content) ?? throw new Exception("Deserialized JSON resulted in null value.");
        }

        public enum Vendor
        {
            LittleSaika = 1,
            SukitaStudio
        }

        public enum StoreLocation
        {
            Gastown = 212,
            Kitsilano = 213,
            NorthVancouver = 214,
            Victoria = 215,
            Metrotown = 216,
            Guildford = 217,
            Tsawwassen = 252,
            Richmond = 253,
            ParkRoyal = 261,
            Southgate = 262
        }
    }
}
