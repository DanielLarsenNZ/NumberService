using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NumberService
{
    public static class GetNumber
    {
        private static HttpClient _http = new HttpClient();
        private static long _lastNumber = 10000;     //TODO: App Setting
        private static string _storageUrl = "http://localhost:7071/api/numbers";       //TODO: App Setting
        private static string _session = Guid.NewGuid().ToString("N");

        [FunctionName("GetNumber")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            Interlocked.Increment(ref _lastNumber);

            var result = await _http.GetFromJsonAsync<NumberResult>($"{_storageUrl}/{_lastNumber}?session={_session}");

            return new OkObjectResult(result);
        }
    }
}
