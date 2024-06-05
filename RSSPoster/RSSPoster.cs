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

namespace Willy_bot
{
    public class RSSPoster
    {
        private readonly DiscordClient _client;
        private readonly ulong _channelId;
        private readonly List<string> _feedUrls;
        private DateTimeOffset _lastPublishedDate;
        private readonly HashSet<string> _postedLinks; // To keep track of posted links
        private const string PostedLinksFile = "posted_links.txt"; // File to store posted links

        public RSSPoster(DiscordClient client, ulong channelId, List<string> feedUrls)
        {
            _client = client;
            _channelId = channelId;
            _feedUrls = feedUrls;
            _lastPublishedDate = DateTimeOffset.MinValue; // Initialize with a very old date
            _postedLinks = new HashSet<string>(); // Initialize the HashSet

            LoadPostedLinks();
        }

        public async Task StartAsync()
        {
            var timer = new System.Threading.Timer(async _ => await CheckFeedsAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        }

        private async Task CheckFeedsAsync()
        {
            foreach (var feedUrl in _feedUrls)
            {
                await CheckFeedAsync(feedUrl);
            }
        }

        private async Task CheckFeedAsync(string feedUrl)
        {
            try
            {
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
                                _lastPublishedDate = newItems.Max(item => item.PublishDate);

                                var channel = await _client.GetChannelAsync(_channelId);

                                foreach (var item in newItems)
                                {
                                    var messageContent = $"{item.Links.FirstOrDefault()?.Uri.ToString()}";
                                    var imageUrl = GetImageUrl(item);
                                    if (!string.IsNullOrEmpty(imageUrl))
                                    {
                                        // Add the image URL directly to the message content
                                        messageContent += $"\n{imageUrl}";
                                    }

                                    // Send the message and add a line separator
                                    await channel.SendMessageAsync(messageContent);
                                    //await channel.SendMessageAsync("--------------------------------------------------");

                                    // Add the link to the HashSet and save to file to avoid reposting
                                    _postedLinks.Add(item.Id);
                                    SavePostedLinks();

                                    // Wait for 60 seconds before posting the next item
                                    await Task.Delay(TimeSpan.FromSeconds(60));
                                }
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
            /*
            // Check if the item has media content (e.g., media:content or media:thumbnail)
            var mediaContent = item.ElementExtensions
                .Where(ext => ext.OuterName == "content" && ext.OuterNamespace == "http://search.yahoo.com/mrss/")
                .Select(ext => ext.GetObject<XmlElement>())
                .FirstOrDefault();

            if (mediaContent != null)
            {
                return mediaContent.GetAttribute("url");
            }

            // Check if the item has an enclosure with a type that indicates an image
            var enclosure = item.Links.FirstOrDefault(link => link.RelationshipType == "enclosure" && link.MediaType.StartsWith("image/"));
            if (enclosure != null)
            {
                return enclosure.Uri.ToString();
            }

            // Check if the item has an <image> element in its content
            var content = item.Content as TextSyndicationContent;
            if (content != null && content.TextContains("<img"))
            {
                var doc = new XmlDocument();
                doc.LoadXml(content.Text);
                var imgNode = doc.SelectSingleNode("//img");
                if (imgNode != null && imgNode.Attributes["src"] != null)
                {
                    return imgNode.Attributes["src"].Value;
                }
            }
            */

            return null;
        }

        private void LoadPostedLinks()
        {
            if (File.Exists(PostedLinksFile))
            {
                var lines = File.ReadAllLines(PostedLinksFile);
                foreach (var line in lines)
                {
                    _postedLinks.Add(line);
                }
            }
        }

        private void SavePostedLinks()
        {
            File.WriteAllLines(PostedLinksFile, _postedLinks);
        }

        public void ResetPostedLinks()
        {
            _postedLinks.Clear();
            File.WriteAllLines(PostedLinksFile, _postedLinks);
        }
    }
}
