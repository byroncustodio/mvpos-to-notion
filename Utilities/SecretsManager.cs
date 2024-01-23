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

        public string GetSecret(string name, string version)
        {
            try
            {
                var secretVersionName = new SecretVersionName(gcProjectId, name, version);
                var response = _secretManagerServiceClient.AccessSecretVersion(secretVersionName);
                return response.Payload.Data.ToStringUtf8();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
