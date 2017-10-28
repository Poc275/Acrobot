using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Threading.Tasks;

namespace Acrobot.Dialogs
{
    [Serializable]
    public class GreetingDialog : IDialog<string>
    {
        private int attempts = 3;

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("Ok, try something like 'What does TLA stand for?'");
            context.Wait(MessageReceivedAsync);
        }


        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            // check the user actually typed TLA
            if (message.Text.Contains("TLA"))
            {
                await context.PostAsync("TLA stands for Three Letter Acronym 😏");
                await context.PostAsync("You can also let me know what an acronym stands for. E.g. 'BRB stands for Be Right Back'");
                context.Done(message.Text);
            }
            else
            {
                --attempts;
                if (attempts > 0)
                {
                    await context.PostAsync("Not quite, type 'TLA'");
                    context.Wait(MessageReceivedAsync);
                }
                else
                {
                    context.Fail(new TooManyAttemptsException("You didn't follow my instructions 😒"));
                }
            }
        }
    }
}