# Understanding Azure Cosmos DB Conflict Feed

> 👷🏻‍♂️ Work in Progress 👷🏻‍♀️

Azure Cosmos DB is an amazing _globally_ replicated Database with multi-region write capability, i.e. you can write to any region 🤯. With global replication comes _eventual consistency_. And with multi-region writes comes the requirement to manage Conflicts when the same document is created, updated or deleted at the same time in two different regions. Cosmos DB provides an important tool for developers to manage Conflicts called the _Conflicts Feed_.

This article explains with demonstrations how Conflicts are handled by Cosmos DB and how the Conflict feed works.

## Conflicts are hard

The first challenge when trying to understand how conflicts are handled by Cosmos is to actually create a conflict. Cosmos is enabled by one of the largest and fastest private networks on the planet. Replication packets flow between regional replicas on dark Microsoft fibre and replication between Australia East and Australia Southeast (in my example) is incredibly quick. 

My parallelised test from Auckland can take more than 100 attempts before I get a collision. Just goes to show how awesome the multi-region sync actually is.
 
You can run this yourself on your local or in cloud shell using this script: NumberService/test-multiregion.ps1 at main · DanielLarsenNZ/NumberService (github.com)
 
It hits two Functions in parallel, deployed in Australia East and Australia Southeast respectively. They have been configured to only write to their local node. Here’s example output:
 
 
 
I have not configured a Conflict resolution policy yet. The conflicts in Data Explorer is empty:
 
 
 
 
When I go to set a custom conflict policy it is not supported:
 
 
I have to recreate the collection as per https://stackoverflow.com/a/61198147/610731 🤔
 
So I did that. Now I have custom conflict resolution, no merge sproc.
 
 
 
After 244 tests I get a conflict on number 488. I only have one current item.
 
 
 
If I check the Conflicts I get:
 
 
 
Very nice! Now I can inform my client “a55a8…” that their number is no longer valid.
 
 
 
 
⭐ My unanswered question is: Is Cosmos in a consistent state; i.e. would it be possible to failover in this state? I have multi-region writes enabled so this is not possible to test?
 
Next from a client perspective I would (optionally) Container.ReplaceItemAsync (manual merge) and then Conflicts.DeleteItemAsync.
 
For the numbers scenario, I would only ever Delete the conflict, once my client has been notified.                                 


## Notes

When you turn multi-region writes again, your clients will remain "sticky" to the primary write region at the time. I assume they reconnect after a period of time, or next time the app is restarted.