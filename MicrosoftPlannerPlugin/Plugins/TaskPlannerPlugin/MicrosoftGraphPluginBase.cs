using Microsoft.Graph;
using Azure.Identity;

namespace MicrosoftPlannerPlugin;

public abstract class MicrosoftGraphPluginBase
{
    public GraphServiceClient GraphClient { get; private set; }

    public MicrosoftGraphPluginBase(
        string tenantId, 
        string clientId, 
        string clientSecret)
    {
        GraphClient = CreateGraphClient(tenantId, clientId, clientSecret);
        //GraphClient = CreateGraphClientForUser(tenantId, clientId, clientSecret);
    }

    private GraphServiceClient CreateGraphClient(
        string tenantId, 
        string clientId, 
        string clientSecret)
    {
        var options = new TokenCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        };

        var clientSecretCredential = new ClientSecretCredential(
            tenantId, clientId, clientSecret, options);
        var scopes = new[] { "https://graph.microsoft.com/.default" };

        return new GraphServiceClient(clientSecretCredential, scopes);
    }

    private GraphServiceClient CreateGraphClientForUser(
        string tenantId,
        string clientId,
        string clientSecret)
    {
        var scopes = new[] { "https://graph.microsoft.com/.default" };

        var options = new InteractiveBrowserCredentialOptions
        {
            TenantId = tenantId,
            ClientId = clientId,
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud, 
            // Adjust as necessary for sovereign clouds
            // Optionally specify a redirect URI; default is localhost
            RedirectUri = new Uri("http://localhost")
        };

        var credential = new InteractiveBrowserCredential(options);

        var graphClient = new GraphServiceClient(credential, scopes);

        return graphClient;
    }
}
