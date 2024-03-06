using Google.Cloud.SecretManager.V1;
using System;

namespace MakersManager.Utilities
{
    public class SecretsManager
    {
        private readonly SecretManagerServiceClient _secretManagerServiceClient;
        private const string GcProjectId = "makers-411506";

        public SecretsManager(SecretManagerServiceClient secretManagerServiceClient) 
        {
            _secretManagerServiceClient = secretManagerServiceClient;
        }

        public string GetSecret(string name)
        {
            try
            {
                var secretVersionName = new SecretVersionName(GcProjectId, name, GetLatestSecretVersion(name));
                var response = _secretManagerServiceClient.AccessSecretVersion(secretVersionName);
                return response.Payload.Data.ToStringUtf8();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private string GetLatestSecretVersion(string name)
        {
            return name switch
            {
                "mvpos-base-url" => "1",
                "mvpos-password" => "1",
                "mvpos-sku-code" => "1",
                "mvpos-user" => "1",
                "notion-base-url" => "1",
                "notion-locations-id" => "1",
                "notion-products-id" => "1",
                "notion-sales-id" => "1",
                "notion-summary-id" => "1",
                "notion-token" => "1",
                _ => null
            };
            ;
        }
    }
}
