using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ShopMakersManager;

namespace MakersManager
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(WebHostBuilderContext context, IApplicationBuilder app)
        {
            base.Configure(context, app);
        }

        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            services.AddHttpClient<MVPOS>();
            services.AddHttpClient<Notion>();
            services.AddSecretManagerServiceClient();

            base.ConfigureServices(context, services);
        }
    }
}
