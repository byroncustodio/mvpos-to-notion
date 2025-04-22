using Google.Cloud.SecretManager.V1;

namespace MakersManager.Utilities;

public class SecretsManager(SecretManagerServiceClient secretManagerServiceClient)
{
    private const string GcProjectId = "makers-411506";

    private AccessSecretVersionResponse GetSecretVersionResponse(string name, string version = "latest")
    {
        SecretVersionName secretVersionName = new(GcProjectId, name, version);
        return secretManagerServiceClient.AccessSecretVersion(secretVersionName);
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