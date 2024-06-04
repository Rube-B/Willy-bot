/*using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using OpenAI_API;
using OpenAI_API.Chat;

namespace Willy_bot.commands
{
    public class AICommands : ApplicationCommandModule
    {
        private static readonly ulong aiChannelId = 1247621999111442523;
        private static readonly int maxRequestsPerMinute = 3;
        private static readonly int requestInterval = 60000 / maxRequestsPerMinute; // in milliseconds
        private static DateTime lastRequestTime = DateTime.MinValue;

        [SlashCommand("AI", "Talk directly to AI.")]
        public async Task AI(InteractionContext ctx, [Option("Message", "Message to AI")] string message)
        {
            try
            {
                Console.WriteLine("AI command received.");

                if (ctx.Channel.Id != aiChannelId)
                {
                    Console.WriteLine("AI command used in the wrong channel.");
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Please use the dedicated AI channel for this command."));
                    return;
                }

                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var currentTime = DateTime.UtcNow;
                var timeSinceLastRequest = (currentTime - lastRequestTime).TotalMilliseconds;

                if (timeSinceLastRequest < requestInterval)
                {
                    var waitTime = requestInterval - (int)timeSinceLastRequest;
                    Console.WriteLine($"Rate limit reached. Waiting for {waitTime} ms.");
                    await Task.Delay(waitTime);
                }

                string apiKey = "sk-proj-BjU50z2gJuc6IepwcinQT3BlbkFJUhaDj35oX3yxYGFdsYo2";
                if (string.IsNullOrEmpty(apiKey) || !apiKey.StartsWith("sk-"))
                {
                    throw new InvalidOperationException("You did not provide a valid API key. Please check your configuration.");
                }

                var api = new OpenAIAPI(apiKey);
                var chatRequest = new ChatRequest
                {
                    Model = "gpt-3.5-turbo",
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage(ChatMessageRole.User, message)
                    }
                };

                int retryCount = 0;
                const int maxRetries = 5;
                bool successful = false;

                while (!successful && retryCount < maxRetries)
                {
                    try
                    {
                        var chatResponse = await api.Chat.CreateChatCompletionAsync(chatRequest);
                        foreach (var choice in chatResponse.Choices)
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(choice.Message.Content));
                        }
                        successful = true;
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        retryCount++;
                        int delay = (int)Math.Pow(2, retryCount) * 1000;
                        Console.WriteLine($"Quota exceeded. Retrying in {delay} ms.");
                        await Task.Delay(delay);
                    }
                }

                if (!successful)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("The bot has exceeded its usage quota for OpenAI. Please try again later."));
                }

                lastRequestTime = DateTime.UtcNow;
                Console.WriteLine("AI command processed successfully.");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Configuration error: {ex.Message}");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Configuration error: {ex.Message}"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing AI command: {ex.Message}");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"An error occurred: {ex.Message}"));
            }
        }
    }
}*/