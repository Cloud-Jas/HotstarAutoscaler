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
        List<string> commentaryList = GetCommentaryList();

        foreach (var commentary in commentaryList)
        {
            await context.CallActivityAsync("PublishToServiceBusQueue", commentary);            
            await context.CreateTimer(context.CurrentUtcDateTime.AddMinutes(1), CancellationToken.None);
        }
    }

     [FunctionName("PublishToServiceBusQueue")]
    public static async Task PublishToServiceBusQueue(
        [ActivityTrigger] string commentary,
        [ServiceBus("%ServiceBusQueue%", Connection = "ServiceBusConnection")] IAsyncCollector<string> messageQueue,
        ILogger log)
    {
        await messageQueue.AddAsync(commentary);
        log.LogInformation($"Published to Queue: {commentary}");
    }

    private static List<string> GetCommentaryList()
    {
        return new List<string>
        {
            "Boult to Jadeja, out Caught by Williamson!! Massive wicket! NZ needed a wicket and his main strike bowler has done precisely that. Slower ball and for once, Jadeja didn't quite pick it as he went across the line. A bit too early and skewed it straight up in the air. Never easy, these pressure catches but it's the ice-cool KW at mid-off who settles under it calmly. Exceptional knock from Jadeja but he has to go. NZ in the driver's seat. Jadeja c Williamson b Boult 77(59) [4s-4 6s-4]",
            "Ferguson to Chahal, no run, very full and outside off, squeezed out towards mid-off", 
            "Ferguson to Chahal, 1 run, full and angling into Chahal who flicks it away towards deep square leg"
        };
    }
}

}