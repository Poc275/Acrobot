using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Microsoft.ProjectOxford.Text.Sentiment;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Acrobot.Dialogs
{
    [Serializable]
    [LuisModel("c9f531aa-69d6-4090-8049-7463f0182f34", "027b3f51a6cd47fab4301e9d5066e96d")]
    public class RootDialog : LuisDialog<object>
    {
        string cognitiveServicesKey = "beb52f8948964337abf9fbb920fd7773";
        // must be a better way of doing this than a global
        string partialDefinition;

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            // user is sending something that isn't an acronym
            // analyse the sentiment to construct a response
            float sentimentScore = await GetSentiment(result.Query);

            if (sentimentScore < 0.4)
            {
                await context.PostAsync("😟 I'm sorry to hear that. Would you mind leaving me a short message telling me how I can improve?");
            }
            else if (sentimentScore < 0.6)
            {
                await context.PostAsync("😐 It seems I wasn't of much use to you. Would you mind telling me why?");
            }
            else
            {
                await context.PostAsync("😎 I'm happy you think so! Would you mind leaving me a short message telling me what you enjoyed about my service?");
            }

            context.Wait(GetFeedback);
        }


        // function that gets called with any user feedback
        public async Task GetFeedback(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            TelemetryClient telemetry = new TelemetryClient();
            var message = await result;

            // add feedback to telemetry for collection/analysis later
            var props = new Dictionary<string, string> { { "feedback", message.Text } };
            telemetry.TrackEvent("Feedback", properties: props);

            await context.PostAsync("Thanks for the feedback!");
            context.Done("");
        }


        [LuisIntent("welcome")]
        public async Task Welcome(IDialogContext context, LuisResult result)
        {
            context.Call(new GreetingDialog(), DialogResumeAfter);
        }


        [LuisIntent("find")]
        public async Task Find(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
        {
            // add acronym to telemetry
            TelemetryClient telemetry = new TelemetryClient();

            // get the acronym entity
            if(result.TryFindEntity("acronym", out EntityRecommendation acronymEntityRecommendation))
            {
                // add acronym to telemetry so we can track it
                // Set up some properties and metrics:
                // Properties - String values that you can use to filter 
                // your telemetry in the usage reports.
                // Metrics - Numeric values that can be presented graphically
                var props = new Dictionary<string, string> { { "acronym", acronymEntityRecommendation.Entity } };
                telemetry.TrackEvent("FindAcronym", properties: props);

                var acronym = await message;
                acronym.Text = acronymEntityRecommendation.Entity.ToUpper();
                await context.Forward(new AcronymDialog(), DialogResumeAfter, acronym, CancellationToken.None);
            }
            else
            {
                // LUIS couldn't extract an acronym
                await context.PostAsync("I'm sorry, I don't know which acronym you are looking for. " +
                    "Try something like 'What is a TLA?'");
            }
        }


        [LuisIntent("create")]
        public async Task Create(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
        {
            // retrieve acronym & definition entities
            result.TryFindEntity("acronym", out EntityRecommendation acronymEntity);
            result.TryFindEntity("definition", out EntityRecommendation definitionEntity);

            if (acronymEntity != null && definitionEntity != null)
            {
                var definition = await message;
                definition.Text = acronymEntity.Entity.ToUpper() + "=" + definitionEntity.Entity;
                await context.Forward(new CreateDialog(), DialogResumeAfter, definition, CancellationToken.None);
            }
            else if (acronymEntity != null && definitionEntity == null)
            {
                // we have an acronym but not a definition, so someone has typed 
                // something like "Define TLA" expecting to be prompted for a definition
                partialDefinition = acronymEntity.Entity.ToUpper();

                var definitionDialog = new PromptDialog.PromptString($"What is the definition of { acronymEntity.Entity.ToUpper() }?", 
                                                                    "What was that sorry?", 
                                                                    3);

                context.Call(definitionDialog, DefinitionPromptResumeAfter);
            }
            else
            {
                await context.PostAsync("I couldn't find the acronym you wanted to create. " +
                    "Try something like 'TLA stands for Three Letter Acronym'");
            }
        }

        private async Task DefinitionPromptResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            var definition = await result;
            IActivity activity = new Activity
            (
                text: partialDefinition + "=" + definition
            );

            await context.Forward(new CreateDialog(), DialogResumeAfter, activity, CancellationToken.None);
        }


        // Tag an acronym as duplicate
        [LuisIntent("duplicate")]
        public async Task Duplicate(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
        {
            if(result.TryFindEntity("id", out EntityRecommendation idEntityRecommendation))
            {
                var duplicateMessage = await message;
                duplicateMessage.Text = idEntityRecommendation.Entity;
                await context.Forward(new DuplicateDialog(), DialogResumeAfter, duplicateMessage, CancellationToken.None);
            }
            else
            {
                // should really handle not finding a valid id but the LUIS recogniser shouldn't fail on this
                await context.PostAsync("Thankyou. This will help me provide better results");
            }
        }


        [LuisIntent("book")]
        public async Task Book(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I can book conference rooms. This will be coming in my next release!");
        }


        [LuisIntent("who")]
        public async Task Who(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I'm the Acrobot!");
        }


        [LuisIntent("feeling")]
        public async Task Feeling(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I'm great, thanks for asking!");
        }


        private async Task DialogResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            await context.PostAsync("Let me know if you need anything else");
        }


        // method that calls the sentiment analysis API
        // and returns a score between 0 (negative) and 1 (positive)
        private async Task<float> GetSentiment(string sentence)
        {
            TelemetryClient telemetryClient = new TelemetryClient();
            float sentiment = 0;

            try
            {
                var document = new SentimentDocument()
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = sentence,
                    Language = "en"
                };

                var request = new SentimentRequest();
                request.Documents.Add(document);

                var client = new SentimentClient(cognitiveServicesKey);
                var response = await client.GetSentimentAsync(request);

                sentiment = response.Documents[0].Score;
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
            }

            return sentiment;
        }
    }
}