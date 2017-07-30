using Microsoft.Bot.Builder.Dialogs;
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
            context.UserData.TryGetValue<string>("Acronym", out acronym);
            context.UserData.TryGetValue<string>("Definition", out definition);

            // capitalise definition before adding
            definition = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(definition);

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
                Models.Acronym newAcronym = new Models.Acronym();
                newAcronym.Acronym1 = acronym;
                newAcronym.Definition = definition;
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