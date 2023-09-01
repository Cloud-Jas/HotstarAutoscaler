using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace Hotstar.Autoscaler
{
public static class CommentaryOrchestration
{
    [FunctionName("ProcessCommentaryOrchestration")]
    public static async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        var timer = context.CreateTimer(new DateTime(2023, 8, 29, 16, 0, 0), CancellationToken.None);
        var commentaryList = GetCommentaryList();

        while (!timer.IsCompleted)
        {
            foreach (string commentary in commentaryList)
            {
                await context.CallActivityAsync("PublishToServiceBus", commentary);
            }
            
            if (!context.IsReplaying)
            {
                await timer;
            }
        }
    }

    [FunctionName("PublishToServiceBus")]
    public static async Task PublishToServiceBus(
        [ActivityTrigger] string commentary,
        [ServiceBus("%ServiceBusTopic%", Connection = "ServiceBusConnection")] IAsyncCollector<string> messageQueue,
        ILogger log)
    {        
        await messageQueue.AddAsync(commentary);
        log.LogInformation($"Published: {commentary}");
    }

    private static List<string> GetCommentaryList()
    {
        return new List<string>
        {
            "Commentary 1",
            "Commentary 2", 
        };
    }
}

}