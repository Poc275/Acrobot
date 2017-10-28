using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Acrobot.Dialogs
{
    [Serializable]
    public class AcronymDialog : IDialog<string>
    {
        public async Task StartAsync(IDialogContext context)
        {
            List<Models.Acronym> results;

            // get acronym the user is searching for
            context.UserData.TryGetValue<string>("Acronym", out string acronym);
            await context.PostAsync($"Searching for { acronym } ...");

            // query db for acronym definition
            results = GetAcronymDefinition(acronym);

            if(results.Count == 1)
            {
                // single result - just return the definition
                await context.PostAsync(results[0].Definition);
            }
            else if(results.Count > 1)
            {
                // multiple possibilities - send a carousel
                var reply = context.MakeMessage();
                List<Attachment> cards = new List<Attachment>();

                foreach (var acronymResult in results)
                {
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction duplicateButton = new CardAction()
                    {
                        Value = "Duplicate: " + acronymResult.Id,
                        Type = ActionTypes.ImBack,
                        Title = "Duplicated?",
                    };
                    cardButtons.Add(duplicateButton);

                    var card = new ThumbnailCard
                    {
                        Title = acronym,
                        Text = acronymResult.Definition,
                        Buttons = cardButtons
                    };

                    cards.Add(card.ToAttachment());
                }

                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                reply.Attachments = cards;

                await context.PostAsync($"{ acronym } could be one of either:");
                await context.PostAsync(reply);
            }
            else
            {
                var reply = context.MakeMessage();
                List<Attachment> cards = new List<Attachment>();

                List<CardAction> cardButtons = new List<CardAction>();
                CardAction googleButton = new CardAction()
                {
                    Type = ActionTypes.OpenUrl,
                    Title = "Ask Google?",
                    Value = "http://www.google.com/search?q=acronym " + acronym
                };
                cardButtons.Add(googleButton);

                var card = new ThumbnailCard
                {
                    Title = "I'm sorry, I don't know what that acronym means 🙁",
                    Text = "If you find out let me know with something like 'TLA means Three Letter Acronym'",
                    Buttons = cardButtons
                };

                cards.Add(card.ToAttachment());
                reply.Attachments = cards;

                await context.PostAsync(reply);
            }

            context.Done("");
        }


        // function that searches the acronym db
        private List<Models.Acronym> GetAcronymDefinition(string acronym)
        {
            List<Models.Acronym> definitions = new List<Models.Acronym>();
            Models.AcronymDBEntities db = new Models.AcronymDBEntities();

            var query = (from Acronyms in db.Acronyms
                         where Acronyms.Acronym1 == acronym
                         select Acronyms);

            foreach (var definition in query)
            {
                definitions.Add(definition);
            }

            return definitions;
        }
    }
}