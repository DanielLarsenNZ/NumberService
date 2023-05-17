using Microsoft.ApplicationInsights;
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
        private readonly TelemetryClient _telemetry;
        private readonly Container _container;

        public GetNumber(TelemetryClient telemetry, CosmosClient cosmos)
        {
            _telemetry = telemetry;
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


            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Response<NumberResult> response = null;

            try
            {
                response = await _container.ReadItemAsync<NumberResult>(
                                key,
                                new PartitionKey(key));
            }
            finally
            {
                timer.Stop();
                _telemetry.TrackCosmosDependency(
                    response,
                    $"$key={key}",
                    startTime,
                    timer.Elapsed);
            }

            var number = response.Resource;

            return new OkObjectResult(number);
        }
    }
}
