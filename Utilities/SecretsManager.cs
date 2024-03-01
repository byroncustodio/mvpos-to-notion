using Google.Cloud.SecretManager.V1;
using System;

namespace MakersManager.Utilities
{
    public class SecretsManager
    {
        private readonly SecretManagerServiceClient _secretManagerServiceClient;
        private const string gcProjectId = "makers-411506";

        public SecretsManager(SecretManagerServiceClient secretManagerServiceClient) 
        {
            _secretManagerServiceClient = secretManagerServiceClient;
        }

        public string GetSecret(string name)
        {
            try
            {
                var secretVersionName = new SecretVersionName(gcProjectId, name, GetLatestSecretVersion(name));
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
            switch (name)
            {
                case "mvpos-base-url":
                    return "1";
                case "mvpos-password":
                    return "1";
                case "mvpos-sku-code":
                    return "1";
                case "mvpos-user":
                    return "1";
                case "notion-summary-id":
                    return "1";
                case "notion-base-url":
                    return "1";
                case "notion-inventory-id":
                    return "1";
                case "notion-locations-id":
                    return "1";
                case "notion-products-id":
                    return "1";
                case "notion-sales-id":
                    return "1";
                case "notion-token":
                    return "1";
                default:
                    return null;
            };
        }
    }
}
