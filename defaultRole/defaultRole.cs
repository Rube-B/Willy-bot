using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Azure.AI.OpenAI;

namespace Willy_bot.commands
{
    public class DefaultRole : BaseCommandModule
    {
        private static DiscordRole memberRole;

        // List of user IDs to be excluded from getting the "Member" role
        private static List<ulong> excludedUserIds = new List<ulong>
        {
            // Add the user IDs you want to exclude here
            1247184625739104327, // Example User ID 1
            155149108183695360,  // Example User ID 2
            184405311681986560,
            375805687529209857,
            235148962103951360
        };

        public static async Task OnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
        {
            // Fetch the "Member" role if it's not already fetched
            if (memberRole == null)
            {
                var guild = e.Guild;
                memberRole = guild.GetRole(ulong.Parse("1175059435752792116")); // Replace with your Member role ID
            }

            // Check if the user is in the exclusion list
            if (excludedUserIds.Contains(e.Member.Id))
            {
                Console.WriteLine($"User {e.Member.Username} is excluded from getting the Member role.");
                return;
            }

            // Check if the bot has the necessary permissions
            if (e.Guild.CurrentMember.Permissions.HasPermission(Permissions.ManageRoles))
            {
                await e.Member.GrantRoleAsync(memberRole);
            }
            else
            {
                Console.WriteLine("Bot does not have Manage Roles permission.");
            }
        }

        public static async Task StartPeriodicRoleCheck(DiscordClient client)
        {
            var guild = await client.GetGuildAsync(ulong.Parse("1175050424257106020")); // Replace with your Guild ID

            // Fetch the "Member" role
            memberRole = guild.GetRole(ulong.Parse("1175059435752792116")); // Replace with your Member role ID

            var timer = new System.Threading.Timer(async e => await EnsureAllMembersHaveRole(guild), null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        private static async Task EnsureAllMembersHaveRole(DiscordGuild guild)
        {
            var members = await guild.GetAllMembersAsync();
            foreach (var member in members)
            {
                // Skip role assignment for members in the exclusion list
                if (excludedUserIds.Contains(member.Id))
                {
                    Console.WriteLine($"User {member.Username} is excluded from getting the Member role.");
                    continue;
                }

                if (!member.Roles.Contains(memberRole))
                {
                    await member.GrantRoleAsync(memberRole);
                }
            }
        }

        [Command("checkroles")]
        public async Task CheckRolesCommand(CommandContext ctx)
        {
            var guild = ctx.Guild;
            memberRole = guild.GetRole(ulong.Parse("1175059435752792116")); // Replace with your Member role ID
            await EnsureAllMembersHaveRole(guild);
            await ctx.Channel.SendMessageAsync("Checked and ensured all members have the Member role.");
        }
    }
}
