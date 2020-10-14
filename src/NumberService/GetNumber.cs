using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Threading.Tasks;

namespace NumberService
{
    public class GetNumber
    {
        private readonly Container _container;

        public GetNumber(CosmosClient cosmos)
        {
            _container = cosmos
                .GetContainer(
                 Environment.GetEnvironmentVariable("CosmosDbDatabaseId"),
                 Environment.GetEnvironmentVariable("CosmosDbContainerId"));
        }

        [FunctionName("GetNumber")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "numbers/{key:alpha}")] HttpRequest req,
            string key)
        {
            var response = await _container.ReadItemAsync<NumberResult>(
                key,
                new PartitionKey(key));

            var number = response.Resource;

            return new OkObjectResult(number);
        }
    }
}
