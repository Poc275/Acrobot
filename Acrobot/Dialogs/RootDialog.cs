using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.ProjectOxford.Text.Sentiment;
using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using System.Collections.Generic;
using Microsoft.Bot.Builder.FormFlow;
using Acrobot.Models;
using Microsoft.Bot.Connector;

namespace Acrobot.Dialogs
{
    [Serializable]
    [LuisModel("c9f531aa-69d6-4090-8049-7463f0182f34", "027b3f51a6cd47fab4301e9d5066e96d")]
    public class RootDialog : LuisDialog<object>
    {
        string cognitiveServicesKey = "beb52f8948964337abf9fbb920fd7773";

        // add acronym to telemetry
        // TelemetryClient is not Serializable but is in a class
        // marked [Serializable] so it must be marked as [NonSerialized]
        //[NonSerialized()]
        //private TelemetryClient telemetry = new TelemetryClient();

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
            // get the acronym entity
            if(result.TryFindEntity("acronym", out EntityRecommendation acronymEntityRecommendation))
            {
                // add acronym to telemetry so we can track it
                // Set up some properties and metrics:
                // Properties - String values that you can use to filter 
                // your telemetry in the usage reports.
                // Metrics - Numeric values that can be presented graphically
                var props = new Dictionary<string, string> { { "acronym", acronymEntityRecommendation.Entity } };
                TelemetryClient telemetry = new TelemetryClient();
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


        [LuisIntent("bookConfRoom")]
        public async Task BookConferenceRoom(IDialogContext context, LuisResult result)
        {
            IDialog<RoomBooking> roomBookingDialog = MakeRootDialog();
            context.Call(roomBookingDialog, RoomBookingComplete);
        }


        // creates RoomBooking form flow
        internal static IDialog<RoomBooking> MakeRootDialog()
        {
            // note PromptInStart immediately starts the form flow
            return Chain.From(() => FormDialog.FromForm(RoomBooking.BuildForm));
        }

        
        // after RoomBooking has finished
        private async Task RoomBookingComplete(IDialogContext context, IAwaitable<RoomBooking> result)
        {
            RoomBooking booking = null;

            try
            {
                booking = await result;
            }
            catch(OperationCanceledException)
            {
                await context.PostAsync("Booking cancelled");
                return;
            }

            if(booking != null)
            {
                // TODO - search for conference room availability...
                // as an example send a couple of possible rooms
                var reply = context.MakeMessage();
                List<Attachment> cards = new List<Attachment>();

                var confRoomOne = new ThumbnailCard
                {
                    Title = "Focus",
                    Subtitle = "9001 Building, D-Site, Derby",
                    Text = "Seats 12 with projector, whiteboard, network access, and audio conferencing facilities",
                    Buttons = new List<CardAction> { new CardAction(ActionTypes.PostBack, "Select", value: "Focus") },
                    Images = new List<CardImage> { new CardImage("https://www.discountmagnet.com/media/Blog%20Post%20Images/clean-design-conference-room.jpg", 
                                                   null, 
                                                   new CardAction(ActionTypes.ShowImage, value: "https://www.discountmagnet.com/media/Blog%20Post%20Images/clean-design-conference-room.jpg")) }
                };

                var confRoomTwo = new ThumbnailCard
                {
                    Title = "Integrity",
                    Subtitle = "9001 Building, D-Site, Derby",
                    Text = "Seats 4 with projector, audio conferencing and network access facilities",
                    Buttons = new List<CardAction> { new CardAction(ActionTypes.PostBack, "Select", value: "Integrity") },
                    Images = new List<CardImage> { new CardImage("https://c1.staticflickr.com/8/7540/16011025026_224f07771c_b.jpg",
                                                   null,
                                                   new CardAction(ActionTypes.OpenUrl, value: "https://c1.staticflickr.com/8/7540/16011025026_224f07771c_b.jpg")) }
                };

                cards.Add(confRoomTwo.ToAttachment());
                cards.Add(confRoomOne.ToAttachment());

                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                reply.Attachments = cards;

                await context.PostAsync("I've found 2 possible rooms for you");
                await context.PostAsync(reply);

                //await context.PostAsync("Your Room Booking: " + booking.ToString());
                context.Wait(MeetingRoomConfirmed);
            }
            else
            {
                await context.PostAsync("Form returned empty response, booking cancelled");
            }
        }

        private async Task MeetingRoomConfirmed(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            await context.PostAsync($"{ message.Text } meeting room booked, thank you for using the booking room service");
            context.Done("done");
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
