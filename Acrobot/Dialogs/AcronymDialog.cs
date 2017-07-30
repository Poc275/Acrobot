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
            var acronym = "";
            List<string> results;

            // get acronym the user is searching for
            context.UserData.TryGetValue<string>("Acronym", out acronym);
            await context.PostAsync($"Searching for { acronym } ...");

            // query db for acronym definition
            results = GetAcronymDefinition(acronym);

            if(results.Count == 1)
            {
                // single result - just return the definition
                await context.PostAsync(results[0]);
            }
            else if(results.Count > 1)
            {
                // multiple possibilities - send a carousel
                var reply = context.MakeMessage();
                List<Attachment> cards = new List<Attachment>();

                foreach (var definition in results)
                {
                    var card = new ThumbnailCard
                    {
                        Title = acronym,
                        Text = definition
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
                await context.PostAsync("I'm sorry, I don't know what that means. " +
                    "If you find out let me know with something like 'TLA means Three Letter Acronym'");
            }

            context.Done("");
        }


        // function that searches the acronym db
        private List<string> GetAcronymDefinition(string acronym)
        {
            List<string> definitions = new List<string>();
            Models.AcronymDBEntities db = new Models.AcronymDBEntities();

            var query = (from Acronyms in db.Acronyms
                         where Acronyms.Acronym1 == acronym
                         select Acronyms.Definition);

            foreach (var definition in query)
            {
                definitions.Add(definition);
            }

            return definitions;
        }
    }
}