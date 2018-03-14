using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Threading.Tasks;

namespace Acrobot.Dialogs
{
    [Serializable]
    public class CreateDialog : IDialog<string>
    {
        string acronym = "";
        string definition = "";

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> message)
        {
            var definitionMessage = await message;
            string[] definitionString = definitionMessage.Text.Split('=');
            acronym = definitionString[0];
            definition = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(definitionString[1]);

            // let user confirm the definition before submitting
            string confirmationMsg = String.Format("Create definition for {0}: {1} ?", acronym, definition);

            PromptDialog.Confirm(context,
                CreateAcronymAsync,
                confirmationMsg,
                "What was that sorry?");
        }


        private async Task CreateAcronymAsync(IDialogContext context, IAwaitable<bool> result)
        {
            var confirm = await result;

            if (confirm)
            {
                // user said yes, add the definition to the db
                Models.AcronymDBEntities db = new Models.AcronymDBEntities();
                Models.Acronym newAcronym = new Models.Acronym()
                {
                    Acronym1 = acronym,
                    Definition = definition
                };
                db.Acronyms.Add(newAcronym);
                db.SaveChanges();

                await context.PostAsync($"{ acronym } saved, thank you.");
            }
            else
            {
                // user said no, help them form a better input
                await context.PostAsync("I'm sorry. " +
                    "Try something like 'TLA stands for Three Letter Acronym' to make it easier for me to understand.");
            }

            context.Done("");
        }
    }
}