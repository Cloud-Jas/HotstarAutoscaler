## Hotstar Autoscaler GPT

We have all seen enough posts about <b> Game Changer 😅</b> plugins built on top of the GPT models. I can assure you this won't be just another one of those.
Join with me in this series to learn about some cool stuffs that you can built using these models in your real-time applications. ( <b> Definitely not a Game Changer! 😉 </b>). 

<img src="https://media2.giphy.com/media/Pja2X9HxQVqVoXx8hc/giphy.gif" height="300" width="300"/>


The objective of this blog is to 

- Stimulate fresh ideas by exploring the logical and reasoning capabilities of GPT models with Azure OpenAI.
- Ensure anyone from a development background should be able to follow along the entire series. (I'm not a Data Scientist myself😉 )

## Have you watched Hotstar scaling architecture video? 🖐️

I assume most of us would have watched this video, where <a href="https://www.linkedin.com/in/gauravkamboj/">Gaurav Kamboj</a>, Cloud Architect at Hotstar,  explains why traditional autoscaling doesn’t work for Hotstar. and shared with us how Hotstar created a global record for live streaming to 25.3 million concurrent viewers.  
Well this is gonna be our problem statement for this entire series. While their is no doubt that hotstar's custom autoscaler is perfectly working fine, our objective here is to expand upon the problem they initially addressed and explore how GPT models could offer innovative solutions for these unique use cases.

## How hotstar approached this problem with custom autoscaler?

- Trafic based scaling for those services that exposes metrics about number of requests being processed
- Ladder based scaling for those services that didn't expose these metrics


Hotstar defined those scaling ladder configurations as below, 
<a class="lightgallery" href="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/vr16ubj86xdqd5gzvoat.png" title="Image description" data-thumbnail="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/vr16ubj86xdqd5gzvoat.png">
 <img class="lazyautosizes lazyloaded" data-src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/vr16ubj86xdqd5gzvoat.png" src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/vr16ubj86xdqd5gzvoat.png" width="1100">
 </a>

For more details refer to this blog : https://blog.hotstar.com/scaling-for-tsunami-traffic-2ec290c37504

## How we are going to approach this problem with Azure OpenAI

### Traffic spikes during IND vs NZ 2019!

<a class="lightgallery" href="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ksrhdceyn7cv9r50o41d.png" title="Image description" data-thumbnail="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ksrhdceyn7cv9r50o41d.png">
 <img class="lazyautosizes lazyloaded" data-src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ksrhdceyn7cv9r50o41d.png" src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ksrhdceyn7cv9r50o41d.png" width="1100">
 </a>

 Some of the key observations in the above chart

 - Unusual spikes happens based on the sentiments of the viewers
 - On day two, when Dhoni came to bat it raisd live users concurrency from 16M to 25M+ users
 - After the fall of his wicket , there is a sudden drop in the traffic ( 25M to 1M users)

 What if these sentiments are tracked by our GPT models and sent a signal to increase or decrease the instance of services.

### Architecture

<a class="lightgallery" href="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/wkc7m2f1b7yzs0yuo88x.jpg" title="Image description" data-thumbnail="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/wkc7m2f1b7yzs0yuo88x.jpg">
 <img class="lazyautosizes lazyloaded" data-src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/wkc7m2f1b7yzs0yuo88x.jpg" src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/wkc7m2f1b7yzs0yuo88x.jpg">
 </a>

 Let's understand different components of this archiecture

 - For simplicity, I have considered 3 services for Hotstar application.
    - <b> Commentary Service </b>: For simulating the commentary in a gap of 1 minute for each ball, I have created a Azure Durable function which picks each commentary in a gap of 1 minute and push those messages to a Azure service bus queue
    - <b> Video service </b>: Video service is for streaming the live game and for that I have created a Virtual machine scale set. Steady and spikes of traffic is common in this service as it is based on the sentiment of the game.
    - <b> Recommendation service </b>: Same as video service , we have virtual machine scale sets for recommendation service, that gets triggered when everyone tries to press back button or come to home screen after a key player wicket has been taken
- Azure Cosmos DB : I have created 2 containers ipl_match to hold the ipl_match and ipl_scaling_ladder to hold the scaling ladder configuration
- Azure Prompt flow: We deploy the prompt flow as a real-time endpoint and consumed in Azure Logic app
- Azure Logic app acts as a Prompt flow invoker and based on the output dynamically changes the instance count of video service and recommendation service


<a class="lightgallery" href="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ahdraxazf5mzltyedkbe.png" title="Image description" data-thumbnail="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ahdraxazf5mzltyedkbe.png">
 <img class="lazyautosizes lazyloaded" data-src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ahdraxazf5mzltyedkbe.png" src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ahdraxazf5mzltyedkbe.png" width="700">
 </a>

<a class="lightgallery" href=" https://dev-to-uploads.s3.amazonaws.com/uploads/articles/nljap9udunw2dbut8p84.png" title="Image description" data-thumbnail=" https://dev-to-uploads.s3.amazonaws.com/uploads/articles/nljap9udunw2dbut8p84.png">
 <img class="lazyautosizes lazyloaded" data-src=" https://dev-to-uploads.s3.amazonaws.com/uploads/articles/nljap9udunw2dbut8p84.png" src=" https://dev-to-uploads.s3.amazonaws.com/uploads/articles/nljap9udunw2dbut8p84.png" width="700">
 </a>


We have loaded the match details for Ind vs NZ in ipl_match container. It contains the Playing XII details along with key players defined for this particular match which is then used in our prompt. This container also contains the number of concurrent users 



### DataFlow

1. First we will simulate the commentary of the match through our Commentary Service (Azure Durable Functions) that pushes the message to Azure service bus queue at a gap of 40 sec interval. 
2. Next we have Azure Logic app , that listens to the Azure service bus Queue at a 1 sec interval. It parses the message content in the queue and gets matchId & commentary.
3. Now the parsed output is passed as an input to our Azure Prompt flow via a HTTP post call.
<a class="lightgallery" href="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/6akk5xbqffp2p2xya57l.png" title="Image description" data-thumbnail="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/6akk5xbqffp2p2xya57l.png">
 <img class="lazyautosizes lazyloaded" data-src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/6akk5xbqffp2p2xya57l.png" src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/6akk5xbqffp2p2xya57l.png">
 </a>
4. MatchId is then passed onto a python code <br>
    4.1. It is used to fetch the match details from Azure CosmosDB
5. Concurrent users from the previous step is then passed onto the next python code <br>
    5.1. It is now used to fetch the scaling ladder configurations from Azure CosmosDB
6. Now we make use of all inputs from previous 3 steps to form the system prompt. 
7. GPT model act upon the prompt that we provided and outputs the JSON in the requested fomrat. It is then received by Azure Logic app <br>
8. & 9. Based on the output, we either increase/decrease the instance count of virtual machine scale sets in Video and Recommendation service.


## Brace for Tsunamis!

- Run the Commentary service and initiate the process of a simulated hotstar environment

<a class="lightgallery" href="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/lu55eff3gfudayd102z4.png" title="Image description" data-thumbnail="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/lu55eff3gfudayd102z4.png">
 <img class="lazyautosizes lazyloaded" data-src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/lu55eff3gfudayd102z4.png" src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/lu55eff3gfudayd102z4.png">
 </a>


<a class="lightgallery" href="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ouiyhydhe071xa9ekmhm.png" title="Image description" data-thumbnail="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ouiyhydhe071xa9ekmhm.png">
 <img class="lazyautosizes lazyloaded" data-src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ouiyhydhe071xa9ekmhm.png" src="https://dev-to-uploads.s3.amazonaws.com/uploads/articles/ouiyhydhe071xa9ekmhm.png">
 </a>


## Special thanks

- <a href="https://www.linkedin.com/in/nikhil-sehgal-32513142/"> Nikhil Sehgal </a> - despite being the CEO and founder of a company, took the time to address the questions and uncertainties I had.

##   References

- <a href="https://learn.microsoft.com/en-us/azure/machine-learning/prompt-flow/overview-what-is-prompt-flow?view=azureml-api-2"> Azure Machine Learning Prompt flow </a>
- <a href="https://learn.microsoft.com/en-us/azure/machine-learning/prompt-flow/concept-connections?view=azureml-api-2"> Connections in Prompt flow </a>
- <a href="https://learn.microsoft.com/en-us/azure/machine-learning/prompt-flow/concept-runtime?view=azureml-api-2"> Runitimes in Prompt flow </a>
