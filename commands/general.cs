using System.Net.Http;
using HtmlAgilityPack;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Willy_bot.TextFiles;
using Azure;
using Azure.AI.OpenAI;
using Newtonsoft.Json;

namespace Willy_bot.commands
{
    public class SlashGeneralCommands : ApplicationCommandModule
    {
        private static readonly HttpClient httpClient;
        private static readonly ulong aiChannelId = 123456789012345678; // Replace with your AI channel ID

        static SlashGeneralCommands()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }
        [SlashCommand("vatsimstats", "Fetches the current VATSIM statistics.")]
        public async Task VatsimStats(InteractionContext ctx, [Option("cid", "The CID of the User")] string cid)
        {
            string url = $"https://api.vatsim.dev/api/v2/members/{cid}/stats";

            try
            {
                var response = await httpClient.GetStringAsync(url);

                // Log the raw response for debugging
                Console.WriteLine("Raw response: " + response);

                // Check if the response is in HTML format
                if (response.StartsWith("<"))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("The API returned HTML content instead of JSON. Please check the API endpoint."));
                    return;
                }

                var json = JObject.Parse(response);

                var atcHours = json["atc_hours"]?.ToString() ?? "N/A";
                var pilotHours = json["pilot_hours"]?.ToString() ?? "N/A";
                var atcSessions = json["atc_sessions"]?.Count() ?? 0;
                var pilotSessions = json["pilot_sessions"]?.Count() ?? 0;

                var embed = new DiscordEmbedBuilder()
                {
                    Title = $"VATSIM Stats for CID: {cid}",
                    Color = DiscordColor.Azure
                };

                embed.AddField("ATC Hours", atcHours, true);
                embed.AddField("Pilot Hours", pilotHours, true);
                embed.AddField("ATC Sessions", atcSessions.ToString(), true);
                embed.AddField("Pilot Sessions", pilotSessions.ToString(), true);

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed)).ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Error fetching VATSIM stats. Please ensure the CID is correct and try again. Error: {e.Message}")).ConfigureAwait(false);
                Console.WriteLine(e.Message);
            }
            catch (JsonReaderException e)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Error parsing the API response. Please check the API response format. Error: {e.Message}")).ConfigureAwait(false);
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"An unexpected error occurred: {e.Message}")).ConfigureAwait(false);
                Console.WriteLine(e.Message);
            }
        }

        [SlashCommand("list", "Lists all available commands.")]
        public async Task ListCommands(InteractionContext ctx)
        {
            var commandNames = ctx.Client.GetSlashCommands()
                .RegisteredCommands
                .SelectMany(kvp => kvp.Value)
                .Select(cmd => cmd.Name)
                .Distinct();

            var formattedCommands = commandNames.Select(c => $"- {c}");
            var commandsList = string.Join("\n", formattedCommands);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Here are the available commands:\n{commandsList}"));
        }

        [SlashCommand("info", "Provides information.")]
        public async Task Information(InteractionContext ctx)
        {
            Text text = new Text();
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(text.Info));
        }

        [SlashCommand("metar", "Fetches METAR information.")]
        public async Task GetMetar(InteractionContext ctx, [Option("icao", "The ICAO code for the airport.")] string icao)
        {
            string url = $"https://metar-taf.com/{icao}";

            try
            {
                var response = await httpClient.GetStringAsync(url);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(response);

                var metarNode = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"metar\"]/div/div/div[4]/div[2]/div[2]/code");
                if (metarNode != null)
                {
                    string metarString = metarNode.InnerText.Trim();
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(metarString));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Could not find the METAR information on the page."));
                }
            }
            catch (HttpRequestException e)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Error fetching the METAR information: {e.Message}"));
            }
        }

        [SlashCommand("taf", "Fetches TAF information.")]
        public async Task GetTaf(InteractionContext ctx, [Option("icao", "The ICAO code for the airport.")] string icao)
        {
            string url = $"https://metar-taf.com/taf/{icao}";

            try
            {
                var response = await httpClient.GetStringAsync(url);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(response);

                var metarNode = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"taf\"]/div/div/div[5]/div/code");
                if (metarNode != null)
                {
                    string tafString = metarNode.InnerText.Trim();
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(tafString));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Could not find the TAF information on the page."));
                }
            }
            catch (HttpRequestException e)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Error fetching the TAF information: {e.Message}"));
            }
        }

        [SlashCommand("notams", "Provides a link to NOTAMs information.")]
        public async Task GetNotams(InteractionContext ctx, [Option("icao", "The ICAO code for the airport.")] string icao)
        {
            string url = $"https://metar-taf.com/notams/{icao}";
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Have a look on this website:\n{url}"));
        }

        [SlashCommand("join", "Joins the voice channel.")]
        public async Task Join(InteractionContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNext();
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Already connected in this channel."));
                return;
            }

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You need to be in a voice channel."));
                return;
            }

            vnc = await vnext.ConnectAsync(chn);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Connected to `{chn.Name}`"));
        }

        [SlashCommand("leave", "Leaves the voice channel.")]
        public async Task Leave(InteractionContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNext();
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("I wasn't even connected."));
                return;
            }

            vnc.Disconnect();
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("I left because it was too boring."));
        }
    }
}
