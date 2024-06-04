using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Willy_bot.commands;
using Willy_bot.config;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus.EventArgs;
using System;
using DSharpPlus.Entities;

namespace Willy_bot
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static SlashCommandsExtension SlashCommands { get; set; }
        private static VoiceNextExtension Voice { get; set; }

        // Dictionary to keep track of the last number posted in each monitored channel
        private static Dictionary<ulong, int> LastNumberInChannel { get; set; } = new Dictionary<ulong, int>();

        private static ulong monitorChannelId = 1247594255082586133; // Replace with your channel ID

        static async Task Main(string[] args)
        {
            var jsonreader = new JSONReader();
            await jsonreader.ReadJSON();

            var discordConfig = new DiscordConfiguration
            {
                Intents = DiscordIntents.All,
                Token = jsonreader.token,
                TokenType = DSharpPlus.TokenType.Bot,
                AutoReconnect = true,
            };

            Client = new DiscordClient(discordConfig);

            // Enable VoiceNext
            Voice = Client.UseVoiceNext(new VoiceNextConfiguration
            {
                AudioFormat = AudioFormat.Default,
                EnableIncoming = false // We are not receiving audio
            });

            //Client.Ready += Client_Ready;
            //Client.ClientErrored += Client_Errored;
            Client.MessageCreated += Counting;

            // Register event handler for new members
            Client.GuildMemberAdded += PrivateMessage.OnGuildMemberAdded;

            // Slash commands configuration
            SlashCommands = Client.UseSlashCommands();
            SlashCommands.RegisterCommands<SlashGeneralCommands>(1175050424257106020); // Replace with your guild ID for testing
            SlashCommands.RegisterCommands<PrivateMessage.PrivateMessageCommands>(1175050424257106020); // Register the new command

            await Client.ConnectAsync();

            // Start the periodic role check
            _ = DefaultRole.StartPeriodicRoleCheck(Client);

            // Start the RSS poster with multiple feeds
            var rssFeeds = new List<string>
            {
                "https://www.fselite.net/feed/",
                "https://www.thresholdx.net/news/rss.xml"
            };
            var rssPoster = new RSSPoster(Client, 1175094941014294578, rssFeeds);
            await rssPoster.StartAsync();

            // Initialize the last number in the monitored channel
            await InitializeLastNumberInChannel(monitorChannelId); // Replace with your channel ID

            await Task.Delay(-1);
        }
        /*
        private static async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            await SetChannelPermissions(monitorChannelId, allowSendMessages: true);
        }

        private static async Task Client_Errored(DiscordClient sender, ClientErrorEventArgs e)
        {
            await SetChannelPermissions(monitorChannelId, allowSendMessages: false);
        }
        
        private static async Task SetChannelPermissions(ulong channelId, bool allowSendMessages)
        {
            var channel = await Client.GetChannelAsync(channelId);
            var permissions = allowSendMessages ? Permissions.SendMessages : Permissions.None;

            // Modify the permissions for the @everyone role
            var everyoneRole = channel.Guild.EveryoneRole;
            await channel.AddOverwriteAsync(everyoneRole, permissions, Permissions.None);
        }
        */
        private static async Task InitializeLastNumberInChannel(ulong channelId)
        {
            var channel = await Client.GetChannelAsync(channelId);
            var messages = await channel.GetMessagesAsync(100);

            foreach (var message in messages.OrderBy(m => m.Id))
            {
                if (int.TryParse(message.Content, out int number))
                {
                    LastNumberInChannel[channelId] = number;
                }
            }
        }

        private static async Task Counting(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Channel.Id == monitorChannelId)
            {
                if (int.TryParse(e.Message.Content, out int number))
                {
                    if (LastNumberInChannel.TryGetValue(e.Channel.Id, out int lastNumber))
                    {
                        if (number == lastNumber + 1)
                        {
                            LastNumberInChannel[e.Channel.Id] = number;
                            await e.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":white_check_mark:"));
                        }
                        else
                        {
                            await e.Message.DeleteAsync();
                        }
                    }
                    else
                    {
                        LastNumberInChannel[e.Channel.Id] = number;
                        await e.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":white_check_mark:"));
                    }
                }
                else
                {
                    await e.Message.DeleteAsync();
                }
            }
        }
    }
}
