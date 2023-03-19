# Number Service

Highly available sequential number generator backed by Cosmos DB with guaranteed uniqueness<sup>*</sup>.

[Free numbers here!](https://numberservice-aue.azurewebsites.net/api/numbers/free)

    PUT https://numberservice-aue.azurewebsites.net/api/numbers/free
    GET https://numberservice-aue.azurewebsites.net/api/numbers/free

    PUT https://numberservice-ase.azurewebsites.net/api/numbers/free
    GET https://numberservice-ase.azurewebsites.net/api/numbers/free

<sup>*</sup> Within same region. For multiple regions, conflict resolution is required.

## Getting started

Check the constant variables in `deploy/_vars.ps1`. Make changes if required.

    cd deploy
    az login
    deploy-azure.ps1
    deploy-sprocs.ps1
    deploy-function.ps1 -FunctionLocation "Australia East"
    deploy-function.ps1 -FunctionLocation "Australia Southeast"

## Requirements

* Each request for a new number must return a number that is unique and one greater than the last number generated (for all clients).
* Number should be able to be seeded (start at 10,000 for example), but only the first time it is generated
* Number service must be highly available with an uptime of around 99.99%, or less than 5 minutes of total downtime per month.
* RTO = minutes
* RPO = 0

## Phase 1

Number generation requires a strong write. It is not possible to have a strong write and multiple write regions, even with a consensus algorithm (not even Cosmos DB offers strong multi-region writes).

The Google **Chubby lock service** is actually a single write region service. It orchestrates a master<sup>1</sup>. The read replicas (which may be thousands of miles away) are mirrors and are eventually consistent. This is OK because chubby is essentially a consistent cache lock service.

Phase 1 fulfils the sequential guarantee requirement with Azure Cosmos DB and Functions. A Cosmos DB stored procedure is used to read a lock document, increment the number, and replacement (with an etag pre-condition) in one procedure. If the proc is unable to replace the document (due another client updating first) then the proc will throw an exception.

[Stored procedures and triggers are always executed on the primary replica of an Azure Cosmos container](https://docs.microsoft.com/en-us/azure/cosmos-db/stored-procedures-triggers-udfs#data-consistency#:~:text=Stored%20procedures%20and%20triggers%20are%20always%20executed%20on%20the%20primary%20replica%20of%20an%20Azure%20Cosmos%20container). This guarantees strong consistency within the proc (regardless of container consistency level) and is perfect for this use case. It is also interesting to note that Cosmos will ensure a [local majority quorum write before acknowledging back to the client](https://docs.microsoft.com/en-us/azure/cosmos-db/consistency-levels-tradeoffs#consistency-levels-and-throughput) (regardless of container consistency level). Strong consistency and multi-region reads ensures global majority quorum writes. 

 The phase one cost is ~NZ$75 per month for 28 numbers per second.

### Analysis

* 1 Cosmos DB Region. Zone Redundant.
* 1 Azure Functions (consumption) region
* 99.94% uptime. No region failover
* RPO and RTO for Zone failure = 0
* RPO for Region failure could be hours and RTO for Region failure could be days, assuming the Region never recovers. However an RPO of 0 and an RTO of hours is more likely IMO.
* ~14 RU/s per number
* Single partition (per key)
* Max RU/s per partition = 10,000, so max throughput is 625 per second
* At 400 RU/s provision, max throughput is 28 per second.
* Highest number currently supported (I assume) is Javascript `Number.MAX_SAFE_INTEGER`, which is 9,007,199,254,740,991.
* Stored proc write consistency is strong. If proc can't increment number in atomic operation it will fail with an exception that is thrown to client.
* Read consistency is the default (session). While out of sproc/session reads may not get the latest number, ordering will be consistent. Strong consistency of reads is not a requirement for NumberService.

### Costs

> 🧮 [Azure Pricing Calculator estimate](https://azure.com/e/cfb40099955e4f83bdfe059840ece9dd)

* Cosmos DB, single region (Australia East), ZR, 400 RU/s, 1GB data = NZ$51.21 /month
* Azure Functions, Consumption, Australia East, @28RPS = NZ$23.90 /month

## Phase 2

In this phase we are experimenting with multi-region writes and conflict resolution in Cosmos DB.

GET https://numberservice-aue.azurewebsites.net/api/conflicts/free
GET https://numberservice-ase.azurewebsites.net/api/conflicts/free

Run the test script `test/test-multiregion.ps1` to (eventually) observe a conflict due to multi-region writes. This can take some time; synchronization between regional replicas is super fast!

### Analysis

* 2 Cosmos DB Regions, multi-region writes enabled
* 2 Azure Functions (consumption), one in each region.
* ~24 RU/s per number
* Single partition (per key)
* As long as clients only PUT to one region, consistency is strong. If multi-region writes, conflict resolution is required.

## References and links

<sup>1</sup> [The Chubby lock service for loosely-coupled distributed systems](https://research.google.com/archive/chubby-osdi06.pdf), section 2.12.

Read from conflict feed: <https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-manage-conflicts?tabs=dotnetv3%2Capi-async%2Casync#read-from-conflict-feed>

Use the `ApplicationPreferredRegions` property to set the preferred region: <https://docs.microsoft.com/en-us/azure/cosmos-db/tutorial-global-distribution-sql-api?tabs=dotnetv3%2Capi-async#net-sdk>
