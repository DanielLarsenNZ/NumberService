using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Microsoft.ApplicationInsights
{
    internal static class TelemetryClientCosmosExtensions
    {
        /// <summary>
        /// Track a Cosmos dependency call in Azure Monitor Application Insights
        /// </summary>
        /// <typeparam name="T">The type parameter of the Cosmos Response<typeparamref name="T"/></typeparam>
        /// <param name="telemetry">An initialised TelemetryClient</param>
        /// <param name="response">The Cosmos Response returned</param>
        /// <param name="command">The command text to display in the dependency trace. NOTE: Do not include PII or Secrets</param>
        /// <param name="timestamp">The timestamp for this call. Usually the DateTimeOffset.UtcNow immediately before the dependency call was made.</param>
        /// <param name="duration">The measured duration of the dependency call as TimeSpan</param>
        public static void TrackCosmosDependency<T>(
            this TelemetryClient telemetry,
            Response<T> response,
            string command,
            DateTimeOffset timestamp,
            TimeSpan duration)
        {
            try
            {
                var diagnostics = Diagnostics(response);

                if (diagnostics is null || diagnostics.Context is null)
                {
                    telemetry.TrackTrace("Cosmos Dependency Diagnostics deserialization error. var diagnostics is null", SeverityLevel.Warning);
                    return;
                }

                var statistics = diagnostics.Context.FirstOrDefault(c => c.Id == "StoreResponseStatistics");

                var dependency = new DependencyTelemetry
                {
                    Data = command,
                    Duration = duration,
                    Name = Name(statistics),
                    ResultCode = StatusCode(response),
                    Timestamp = timestamp,
                    Success = StatusCodeSuccess(response),
                    Target = statistics?.LocationEndpoint?.Host,
                    Type = "Azure DocumentDB"   // This type name will display the Cosmos icon in the UI
                };

                telemetry.TrackDependency(dependency);
            }
            catch (Exception ex)
            {
                // log and continue
                telemetry.TrackException(ex);
            }
        }

        private static string Name(NumberService.Context statistics)
        {
            if (statistics is null) return null;
            return $"{statistics.ResourceType} {statistics.OperationType}";
        }

        private static bool StatusCodeSuccess<T>(Response<T> response)
        {
            if (response is null) return false;
            if ((int)response.StatusCode >= 400) return false;
            return true;
        }

        private static string StatusCode<T>(Response<T> response)
        {
            if (response is null) return null;
            return ((int)response.StatusCode).ToString();
        }

        //private static string FirstDotPartOfHostname(string host)
        //{
        //    if (string.IsNullOrEmpty(host)) return null;
        //    var parts = host.Split(".");
        //    if (parts.Any()) return parts[0];
        //    return host;
        //}

        private static NumberService.CosmosDiagnostics Diagnostics<T>(Response<T> response)
        {
            if (response is null || response.Diagnostics is null) return null;
            return JsonConvert.DeserializeObject<NumberService.CosmosDiagnostics>(response.Diagnostics.ToString());
        }
    }
}
