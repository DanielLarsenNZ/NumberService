# Number Service

Highly available sequential number generator backed by Cosmos DB with guaranteed uniqueness.

Free numbers here!

    PUT https://numberservice-aue.azurewebsites.net/api/numbers/free

## Getting started

Check the constant variables in `deploy-azure.ps1` and `deploy-sprocs.ps1` and make changes if required.

1. `az login`
1. Run `deploy/deploy-azure.ps1`
1. Run `deploy/deploy-sprocs.ps1`

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

## References and links

<sup>1</sup> [The Chubby lock service for loosely-coupled distributed systems](https://research.google.com/archive/chubby-osdi06.pdf), section 2.12.
