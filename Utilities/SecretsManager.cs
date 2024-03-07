using Google.Cloud.SecretManager.V1;

namespace MakersManager.Utilities;

public class SecretsManager
{
    private readonly SecretManagerServiceClient _secretManagerServiceClient;
    private const string GcProjectId = "makers-411506";

    public SecretsManager(SecretManagerServiceClient secretManagerServiceClient) 
    {
        _secretManagerServiceClient = secretManagerServiceClient;
    }

    private AccessSecretVersionResponse GetSecretVersionResponse(string name, string version = "latest")
    {
        SecretVersionName secretVersionName = new(GcProjectId, name, version);
        return _secretManagerServiceClient.AccessSecretVersion(secretVersionName);
    }

    public string GetSecretFromString(string name)
    {
        return GetSecretVersionResponse(name).Payload.Data.ToStringUtf8();
    }

    public byte[] GetSecretFromByteArray(string name)
    {
        return GetSecretVersionResponse(name).Payload.Data.ToByteArray();
    }
}