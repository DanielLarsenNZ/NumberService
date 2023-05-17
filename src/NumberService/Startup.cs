using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

[assembly: FunctionsStartup(typeof(NumberService.Startup))]

namespace NumberService
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton((c) =>
            {
                var options = new CosmosClientOptions();
                string preferredRegions = Environment.GetEnvironmentVariable("CosmosApplicationPreferredRegions");

                if (!string.IsNullOrEmpty(preferredRegions))
                {
                    var regions = preferredRegions.Split(';').ToList();
                    options.ApplicationPreferredRegions = regions;
                }

                return new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbConnectionString"), options);
            });
        }
    }
}