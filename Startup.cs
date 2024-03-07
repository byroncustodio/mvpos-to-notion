using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MvposSDK;
using NotionSDK;

namespace MakersManager
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            services.AddHttpClient<Mvpos>();
            services.AddHttpClient<Notion>();
            services.AddSecretManagerServiceClient();

            base.ConfigureServices(context, services);
        }
    }
}
