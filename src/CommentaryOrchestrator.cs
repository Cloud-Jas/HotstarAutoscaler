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
        [FunctionName("Commentary_SchedulerJob")]
        public static async Task CommentarySchedulerJob(
            [TimerTrigger("0 57 01 * * *")] TimerInfo timer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation("CommentaryScheduler Cron job fired!");

            string instanceId = await starter.StartNewAsync(nameof(ProcessCommentaryOrchestration));
            log.LogInformation($"Started new instance with ID = {instanceId}.");

            DurableOrchestrationStatus status;
            while (true)
            {
                status = await starter.GetStatusAsync(instanceId);
                log.LogInformation($"Status: {status.RuntimeStatus}, Last update: {status.LastUpdatedTime}.");

                if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed ||
                    status.RuntimeStatus == OrchestrationRuntimeStatus.Failed ||
                    status.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
                {
                    break;
                }
            }

            log.LogInformation($"Output: {status.Output}");
        }
        [FunctionName("ProcessCommentaryOrchestration")]
        public static async Task ProcessCommentaryOrchestration(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
        {
            List<MatchCommentary> commentaryList = GetCommentaryList();
            
            foreach(var commentary in commentaryList) 
            {                
                await context.CallActivityAsync("PublishToServiceBusQueue", commentary);                
                await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(20), CancellationToken.None);               
            }
        }

        [FunctionName("PublishToServiceBusQueue")]
        public static async Task PublishToServiceBusQueue(
           [ActivityTrigger] MatchCommentary commentary,
           [ServiceBus("%ServiceBusQueue%", Connection = "ServiceBusConnection")] IAsyncCollector<MatchCommentary> messageQueue,
           ILogger log)
        {
            await messageQueue.AddAsync(commentary);
            log.LogInformation($"Published to Queue: {commentary}");
        }

        private static List<MatchCommentary> GetCommentaryList()
        {
            return new List<MatchCommentary>
        {
            new MatchCommentary{
            matchId ="1",
            commentary= "Boult to Jadeja, out Caught by Williamson!! Massive wicket! NZ needed a wicket and his main strike bowler has done precisely that. Slower ball and for once, Jadeja didn't quite pick it as he went across the line. A bit too early and skewed it straight up in the air. Never easy, these pressure catches but it's the ice-cool KW at mid-off who settles under it calmly. Exceptional knock from Jadeja but he has to go. NZ in the driver's seat. Jadeja c Williamson b Boult 77(59) [4s-4 6s-4]"
            },
            new MatchCommentary{
                matchId ="1",
                commentary="Ferguson to Chahal, no run, very full and outside off, squeezed out towards mid-off"
            },
            new MatchCommentary{
                matchId="1",
                commentary="Ferguson to Chahal, 1 run, full and angling into Chahal who flicks it away towards deep square leg"    
            }
        };
        }

        public class MatchCommentary
        {
            public string commentary { get; set; }
            public string matchId { get; set; }
        }
    }

}