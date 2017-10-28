using Microsoft.Bot.Builder.Dialogs;
using System.Linq;
using System.Threading.Tasks;

namespace Acrobot.Dialogs
{
    public class DuplicateDialog : IDialog<string>
    {
        int id;

        public async Task StartAsync(IDialogContext context)
        {
            context.UserData.TryGetValue<int>("Id", out id);
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