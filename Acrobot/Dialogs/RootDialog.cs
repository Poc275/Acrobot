using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.ProjectOxford.Text.Sentiment;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Acrobot.Dialogs
{
    [Serializable]
    [LuisModel("c9f531aa-69d6-4090-8049-7463f0182f34", "027b3f51a6cd47fab4301e9d5066e96d")]
    public class RootDialog : LuisDialog<object>
    {
        string cognitiveServicesKey = "beb52f8948964337abf9fbb920fd7773";

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            // user is sending something that isn't an acronym
            // analyse the sentiment to construct a response
            float sentimentScore = await GetSentiment(result.Query);

            if (sentimentScore < 0.4)
            {
                await context.PostAsync($"Negative response. (Sentiment: { sentimentScore.ToString() })");
            }
            else if (sentimentScore < 0.6)
            {
                await context.PostAsync($"Neutral response. (Sentiment: { sentimentScore.ToString() })");
            }
            else
            {
                await context.PostAsync($"Positive response. (Sentiment: { sentimentScore.ToString() })");
            }
        }


        [LuisIntent("welcome")]
        public async Task Welcome(IDialogContext context, LuisResult result)
        {
            context.Call(new GreetingDialog(), DialogResumeAfter);
        }


        [LuisIntent("find")]
        public async Task Find(IDialogContext context, LuisResult result)
        {
            // add acronym to telemetry
            // TelemetryClient is not Serializable but is in a class
            // marked [Serializable] so it must be marked as [NonSerialized]
            //[NonSerialized()]
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

                // assign acronym to user data so the AcronymDialog can access it
                context.UserData.SetValue<string>("Acronym", acronymEntityRecommendation.Entity.ToUpper());
                context.Call(new AcronymDialog(), DialogResumeAfter);
            }
            else
            {
                // LUIS couldn't extract an acronym
                await context.PostAsync("I'm sorry, I don't know which acronym you are looking for. " +
                    "Try something like 'What is a TLA?'");
            }
        }


        [LuisIntent("create")]
        public async Task Create(IDialogContext context, LuisResult result)
        {
            // get entities
            if (result.TryFindEntity("acronym", out EntityRecommendation acronymEntityRecommendation) && 
                result.TryFindEntity("definition", out EntityRecommendation definitionEntityRecommendation))
            {
                // assign entities to user data so the CreateDialog can access them
                context.UserData.SetValue<string>("Acronym", acronymEntityRecommendation.Entity.ToUpper());
                context.UserData.SetValue<string>("Definition", definitionEntityRecommendation.Entity);
                context.Call(new CreateDialog(), DialogResumeAfter);
            }
            else
            {
                await context.PostAsync("I couldn't find the acronym or definition to create. " +
                    "Try something like 'TLA stands for Three Letter Acronym'");
            }
        }


        // Tag an acronym as duplicate
        [LuisIntent("duplicate")]
        public async Task Duplicate(IDialogContext context, LuisResult result)
        {
            if(result.TryFindEntity("id", out EntityRecommendation idEntityRecommnedation))
            {
                int.TryParse(idEntityRecommnedation.Entity, out int id);
                context.UserData.SetValue<int>("Id", id);
                context.Call(new DuplicateDialog(), DialogResumeAfter);
            }
            else
            {
                // should really handle this better but the LUIS recogniser shouldn't fail on this
                await context.PostAsync("Thankyou. This will help me provide better results");
            }
        }


        [LuisIntent("book")]
        public async Task Book(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I can book conference rooms. This will be coming in my next release.");
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
                Console.WriteLine(ex);
            }

            return sentiment;
        }
    }
}