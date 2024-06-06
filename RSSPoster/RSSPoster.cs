using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Willy_bot
{
    public class RSSPoster
    {
        private Timer _timer;
        private readonly DiscordClient _client;
        private readonly ulong _channelId;
        private readonly List<string> _feedUrls;
        private DateTimeOffset _lastPublishedDate;
        private HashSet<string> _postedLinks;
        private readonly string _postedLinksFile = "posted_links.txt";

        public RSSPoster(DiscordClient client, ulong channelId, List<string> feedUrls)
        {
            _client = client;
            _channelId = channelId;
            _feedUrls = feedUrls;
            _lastPublishedDate = DateTimeOffset.MinValue; // Initialize with a very old date
            _postedLinks = new HashSet<string>(); // Initialize the HashSet

            // Load the initial set of posted links
            LoadPostedLinks();

            // Set up a timer that triggers every 60 seconds.
            _timer = new Timer(60000);
            _timer.Elapsed += (sender, e) => UpdatePostedLinks();
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void UpdatePostedLinks()
        {
            LoadPostedLinks();
            Console.WriteLine("Updated posted links from file.");
        }

        public async Task StartAsync()
        {
            Console.WriteLine("RSSPoster started.");
            while (true)
            {
                await CheckFeedsAsync();
                Console.WriteLine("Checked feeds.");
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private async Task CheckFeedsAsync()
        {
            Console.WriteLine("Checking feeds...");
            foreach (var feedUrl in _feedUrls)
            {
                await CheckFeedAsync(feedUrl);
            }
        }

        private async Task CheckFeedAsync(string feedUrl)
        {
            try
            {
                Console.WriteLine($"Fetching feed from {feedUrl}");
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetStringAsync(feedUrl);
                    using (var stringReader = new StringReader(response))
                    {
                        using (var xmlReader = XmlReader.Create(stringReader))
                        {
                            var feed = SyndicationFeed.Load(xmlReader);

                            if (feed == null)
                            {
                                Console.WriteLine($"Failed to load RSS feed from {feedUrl}.");
                                return;
                            }

                            var newItems = feed.Items
                                .Where(item => item.PublishDate > _lastPublishedDate && !_postedLinks.Contains(item.Id))
                                .OrderBy(item => item.PublishDate)
                                .ToList();

                            if (newItems.Any())
                            {
                                Console.WriteLine($"Found {newItems.Count} new items.");
                                _lastPublishedDate = newItems.Max(item => item.PublishDate);

                                var channel = await _client.GetChannelAsync(_channelId);

                                foreach (var item in newItems)
                                {
                                    var messageContent = $"{item.Links.FirstOrDefault()?.Uri.ToString()}";
                                    var imageUrl = GetImageUrl(item);
                                    if (!string.IsNullOrEmpty(imageUrl))
                                    {
                                        messageContent += $"\n{imageUrl}";
                                    }

                                    await channel.SendMessageAsync(messageContent);
                                    Console.WriteLine($"Posted: {messageContent}");

                                    _postedLinks.Add(item.Id);
                                    SavePostedLinks();

                                    await Task.Delay(TimeSpan.FromSeconds(60));
                                }
                            }
                            else
                            {
                                Console.WriteLine("No new items found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching the RSS feed from {feedUrl}: {ex.Message}");
            }
        }

        private string GetImageUrl(SyndicationItem item)
        {
            // Your implementation for extracting image URL if necessary
            return null;
        }

        private void LoadPostedLinks()
        {
            _postedLinks.Clear();
            if (File.Exists(_postedLinksFile))
            {
                var lines = File.ReadAllLines(_postedLinksFile);
                foreach (var line in lines)
                {
                    _postedLinks.Add(line);
                }
                Console.WriteLine($"Loaded {lines.Length} posted links.");
            }
            else
            {
                Console.WriteLine("No posted links file found.");
            }
        }

        private void SavePostedLinks()
        {
            File.WriteAllLines(_postedLinksFile, _postedLinks);
            Console.WriteLine("Posted links saved.");
        }

        public void ResetPostedLinks()
        {
            _postedLinks.Clear();
            File.WriteAllLines(_postedLinksFile, _postedLinks);
            Console.WriteLine("Posted links reset.");
        }
    }
}
