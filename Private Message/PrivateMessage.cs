using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace Willy_bot.commands
{
    public class PrivateMessage
    {
        private static readonly ulong authorizedTesterId = 686234087178108928; // Replace with the authorized tester's user ID

        public static async Task OnGuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
        {
            var member = await e.Guild.GetMemberAsync(e.Member.Id);
            var dmChannel = await member.CreateDmChannelAsync();
            await dmChannel.SendMessageAsync(BuildWelcomeMessage(e.Member.Username));
        }

        public class PrivateMessageCommands : ApplicationCommandModule
        {
            [SlashCommand("testwelcome", "Tests the welcome message.")]
            public async Task TestWelcome(InteractionContext ctx, [Option("user", "The user to send the test welcome message to.")] DiscordUser user)
            {
                if (ctx.User.Id != authorizedTesterId)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("🚫 You are not authorized to use this command."));
                    return;
                }

                var member = await ctx.Guild.GetMemberAsync(user.Id);
                var dmChannel = await member.CreateDmChannelAsync();
                await dmChannel.SendMessageAsync(BuildWelcomeMessage(user.Username));
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("✅ Test welcome message sent."));
            }
        }

        private static string BuildWelcomeMessage(string username)
        {
            return $"👋 **Hi {username}, welcome to Willy_Xpress!** 🎉\n\n" +
                   "We're thrilled to have you here. Make yourself at home and feel free to explore the channels. 😊\n\n" +
                   "📌 **Don't forget to check out the** https://discord.com/channels/1175050424257106020/1175051830561087668 **channel to choose your interests and stay updated!**\n\n" +
                   "If you have any questions, don't hesitate to ask. Enjoy your stay and have fun! 🌟\n\n" +
                   "Cheers,\nThe Willy_Xpress Team 🚀";
        }
    }
}
/*
Treat everyone with respect. Absolutely no harassment, witch hunting, or hate speech will be tolerated.

No spam or self-promotion (server invites, advertisements, etc) without permission from a staff member. This includes DMing fellow members.
No age-restricted or obscene content. This includes text, images, or links featuring nudity, sex, hard violence, or other graphically disturbing content.
If you see something against the rules or something that makes you feel unsafe, let staff know. We want this server to be a welcoming space!

*/