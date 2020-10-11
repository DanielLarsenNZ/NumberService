using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberService
{
    public class PutNumber
    {
        private static readonly string _clientId = Guid.NewGuid().ToString("N");
        private static readonly Container _container =
            new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbConnectionString"))
            .GetContainer(
                Environment.GetEnvironmentVariable("CosmosDbDatabaseId"),
                Environment.GetEnvironmentVariable("CosmosDbContainerId"));
        private readonly TelemetryClient _telemetry;


        public PutNumber(TelemetryClient telemetry)
        {
            _telemetry = telemetry;
        }

        [FunctionName("PutNumber")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "numbers/{key:alpha}")] HttpRequest req,
            string key,
            ILogger log)
        {
            var response = (await _container.Scripts.ExecuteStoredProcedureAsync<NumberResult>(
                "incrementNumber",
                new PartitionKey(key),
                new dynamic[] { key, _clientId }));

            var number = response.Resource;

            // As long as sproc is written correctly, this case should never be true.
            if (number.ClientId != _clientId) throw new InvalidOperationException($"Response ClientId \"{number.ClientId}\" does not match ClientId \"{_clientId}\".");

            log.LogInformation($"Number {number.Number} issued to clientId {number.ClientId} with ETag {number.ETag} from key {number.Key}");

            _telemetry.TrackEvent(
                "PutNumber",
                properties: new Dictionary<string, string>
                    {
                        { "Number", number.Number.ToString() },
                        { "ClientId", _clientId },
                        { "Key", number.Key },
                        { "ETag", number.ETag }
                    },
                metrics: new Dictionary<string, double>
                    {
                        { "CosmosRequestCharge", response.RequestCharge }
                    });

            return new OkObjectResult(number);
        }
    }
}
