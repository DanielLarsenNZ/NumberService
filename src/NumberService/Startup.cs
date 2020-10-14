using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(NumberService.Startup))]

namespace NumberService
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton((c) => {
                return new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbConnectionString"));
            });
        }
    }
}