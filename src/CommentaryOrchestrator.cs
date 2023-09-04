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
            [TimerTrigger("0 59 23 * * *")] TimerInfo timer,
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
                commentary="Ferguson to Dhoni, out Dhoni Run Out!! 1 run completed. Guptill you genius of a fielder! Got to the ball quickly and fired in a terrific direct hit at the keeper's end. There was a slight stutter on the second run and cost Dhoni as he was just short. Ironically, for a man who is so good between the wickets, he had to fall to a run out. Guptill has had a bad tourney with the bat but hasn't he redeemed himself in a big way with this effort? Was a slower ball on a length and it popped up off the glove towards short fine leg as Dhoni got cramped on the pull. They took on Guppy for the second run and it cost MS. Dhoni run out (Guptill) 50(72) [4s-1 6s-1]"
            },
             new MatchCommentary{
                matchId="1",
                commentary="Boult to Jadeja, FOUR, the edge goes in the gap! It's turning folks, this game is certainly turning. When moments like these are going in the batting side's favor, momentum is bound to be with them. Was a shortish length ball that Jadeja wanted to guide with an open face, got it off the edge but he won't complain as it bisects the keeper and short third man perfectly"
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