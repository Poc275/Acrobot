using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Acrobot.Dialogs;
using System;
using System.Web;

namespace Acrobot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity isTypingReply = activity.CreateReply();
                isTypingReply.Type = ActivityTypes.Typing;
                await connector.Conversations.ReplyToActivityAsync(isTypingReply);

                await Conversation.SendAsync(activity, () => new RootDialog());
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels

                // when a user connects to the bot, show a welcome msg
                // ConversationUpdate activity is triggered twice upon a new conversation,
                // once when a connection to the bot is established, and another
                // when a new user joins the conversation
                IConversationUpdateActivity update = message;
                var client = new ConnectorClient(new Uri(message.ServiceUrl), new MicrosoftAppCredentials());
                if(update.MembersAdded != null)
                {
                    foreach(var newMember in update.MembersAdded)
                    {
                        if(newMember.Id == message.Recipient.Id)
                        {
                            var welcome = message.CreateReply();
                            welcome.Text = "Hi! I'm the Acrobot, ready to handle all of your acronym needs! \n\n" + 
                                            "Try asking me: \n\n" +
                                            "- What does EEC stand for? \n\n" +
                                            "- What does ETOPS mean? \n\n" +
                                            "- TLA stands for Three Letter Acronym \n\n" +
                                            "- BRB = Be Right Back \n\n" +
                                            "- If you're still unsure say Hi for a quick tutorial \n\n" +
                                            "- Or just type anything, I'm ready to chat!";

                            client.Conversations.ReplyToActivityAsync(welcome);
                        }
                    }
                }
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}