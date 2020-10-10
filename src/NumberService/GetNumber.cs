using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NumberService
{
    public static class GetNumber
    {
        private static readonly string _clientId = Guid.NewGuid().ToString("N");
        private static readonly Container _container =
            new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbConnectionString"))
            .GetContainer(
                Environment.GetEnvironmentVariable("CosmosDbDatabaseId"),
                Environment.GetEnvironmentVariable("CosmosDbContainerId"));

        [FunctionName("GetNumber")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "numbers")] HttpRequest req,
            ILogger log)
        {
            var number = await _container.Scripts.ExecuteStoredProcedureAsync<NumberResult>(
                "incrementNumber",
                new PartitionKey("number3"),
                new dynamic[] { "number3", _clientId });

            log.LogInformation($"Number {number.Resource.Number} issued to clientId {number.Resource.ClientId} with ETag {number.Resource.ETag} from key {number.Resource.Key}");

            return new OkObjectResult(number.Resource);
        }
    }
}
