using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Acrobot.Dialogs
{
    [Serializable]
    public class DuplicateDialog : IDialog<string>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> message)
        {
            var duplicateMessage = await message;
            int.TryParse(duplicateMessage.Text, out int id);
            TagDuplicateAcronym(id);
            await context.PostAsync("Thankyou. This will help me provide better results");
            context.Done("");
        }


        // function that tags the acronym as duplicate
        private void TagDuplicateAcronym(int id)
        {
            Models.AcronymDBEntities db = new Models.AcronymDBEntities();

            var query = (from Acronyms in db.Acronyms
                         where Acronyms.Id == id
                         select Acronyms);

            foreach (var definition in query)
            {
                definition.Duplicate = true;
            }

            db.SaveChanges();
        }
    }
}