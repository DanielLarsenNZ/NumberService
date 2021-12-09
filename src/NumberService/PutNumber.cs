using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberService
{
    public class PutNumber
    {
        private static readonly string _clientId = Guid.NewGuid().ToString("N");
        private readonly TelemetryClient _telemetry;
        private readonly Container _container;

        public PutNumber(TelemetryClient telemetry, CosmosClient cosmos)
        {
            _telemetry = telemetry;
            _container = cosmos
            .GetContainer(
                Environment.GetEnvironmentVariable("CosmosDbDatabaseId"),
                Environment.GetEnvironmentVariable("CosmosDbContainerId"));
        }

        [FunctionName("PutNumber")]
        public async Task<IActionResult> Run(
            ILogger log,
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "numbers/{key:alpha}")] HttpRequest req,
            string key)
        {
            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Response<NumberResult> response = null;

            try
            {
                response = (await _container.Scripts.ExecuteStoredProcedureAsync<NumberResult>(
                    "incrementNumber",
                    new PartitionKey(key),
                    new dynamic[] { key, _clientId }));
            }
            finally
            {
                timer.Stop();
                _telemetry.TrackCosmosDependency(
                    response,
                    $"incrementNumber $key={key}, $_clientId={_clientId}",
                    startTime,
                    timer.Elapsed);
            }

            var number = response.Resource;
            number.RequestCharge = response.RequestCharge;

            // if query string contains ?diagnostics, return CosmosDiagnostics
            if (req.Query.ContainsKey("diagnostics"))
            {
                try
                {
                    number.CosmosDiagnostics = JsonConvert.DeserializeObject<CosmosDiagnostics>(response.Diagnostics.ToString());
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Could not deserialize Diagnostics");
                }
            }

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
