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
    public class GetConflicts
    {
        private readonly TelemetryClient _telemetry;
        private readonly Container _container;

        public GetConflicts(TelemetryClient telemetry, CosmosClient cosmos)
        {
            _telemetry = telemetry;
            _container = cosmos
            .GetContainer(
                Environment.GetEnvironmentVariable("CosmosDbDatabaseId"),
                Environment.GetEnvironmentVariable("CosmosDbContainerId"));
        }

        [FunctionName("GetConflicts")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "conflicts/{key:alpha}")] HttpRequest req,
            ILogger log,
            string key)
        {
            // https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-manage-conflicts?tabs=dotnetv3%2Capi-async%2Casync#read-from-conflict-feed

            var sql = new QueryDefinition("Select * from c where id = @id");
            sql.WithParameter("id", key);

            FeedIterator<ConflictProperties> conflictFeed = _container.Conflicts.GetConflictQueryIterator<ConflictProperties>(sql);

            var conflictResults = new List<ConflictResult>();

            while (conflictFeed.HasMoreResults)
            {
                FeedResponse<ConflictProperties> conflicts = await conflictFeed.ReadNextAsync();
                foreach (ConflictProperties conflict in conflicts)
                {
                    var conflictResult = new ConflictResult();

                    // Read the conflicted content
                    conflictResult.Conflict = _container.Conflicts.ReadConflictContent<NumberResult>(conflict);
                    conflictResult.Current = await _container.Conflicts.ReadCurrentAsync<NumberResult>(conflict, new PartitionKey(conflictResult.Conflict.Key));

                    conflictResults.Add(conflictResult);




                    // Do manual merge among documents
                    //await container.ReplaceItemAsync<NumberResult>(intendedChanges, intendedChanges.Key, new PartitionKey(intendedChanges.Key));

                    // Delete the conflict
                    //await container.Conflicts.DeleteAsync(conflict, new PartitionKey(intendedChanges.MyPartitionKey));
                }
            }

            return new OkObjectResult(conflictResults);
        }
    }
}

