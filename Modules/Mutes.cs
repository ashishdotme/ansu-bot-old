﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ansu.Modules
{

    public static class Mutes
    {
        public static TimeSpan RoundToNearest(this TimeSpan a, TimeSpan roundTo)
        {
            long ticks = (long)(Math.Round(a.Ticks / (double)roundTo.Ticks) * roundTo.Ticks);
            return new TimeSpan(ticks);
        }
    }

    public class MuteCmds : BaseCommandModule
    {
        private readonly DiscordClient _client;

        public MuteCmds(DiscordClient client)
        {
            _client = client;
        }

        [Command("unmute")]
        [Description("Unmutes a previously muted user, typically ahead of the standard expiration time. See also: mute")]
        [HomeServer, RequireHomeserverPerm(ServerPermLevel.TrialMod)]
        public async Task UnmuteCmd(CommandContext ctx, [Description("The user you're trying to unmute.")] DiscordUser targetUser)
        {
            DiscordGuild guild = ctx.Guild;
            DiscordChannel logChannel = await _client.GetChannelAsync(Program.cfgjson.LogChannel);

            // todo: store per-guild
            DiscordRole mutedRole = guild.GetRole(Program.cfgjson.MutedRole);
            DiscordMember member = await guild.GetMemberAsync(targetUser.Id);

            if ((await Program.db.HashExistsAsync("mutes", targetUser.Id)) || member.Roles.Contains(mutedRole))
            {
                await UnmuteUserAsync(targetUser);
                await ctx.RespondAsync($"{Program.cfgjson.Emoji.Information} Successfully unmuted **{targetUser.Username}#{targetUser.Discriminator}**.");
            }
            else
                try
                {
                    await UnmuteUserAsync(targetUser);
                    await ctx.RespondAsync($"{Program.cfgjson.Emoji.Warning} According to Discord that user is not muted, but I tried to unmute them anyway. Hope it works.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    await ctx.RespondAsync($"{Program.cfgjson.Emoji.Error} That user doesn't appear to be muted, *and* an error ocurred while attempting to unmute them anyway. Please contact the bot owner, the error has been logged.");
                }
        }

        [Command("mute")]
        [Description("Mutes a user, preventing them from sending messages until they're unmuted. See also: unmute")]
        [HomeServer, RequireHomeserverPerm(ServerPermLevel.TrialMod)]
        public async Task MuteCmd(
            CommandContext ctx, [Description("The user you're trying to mute")] DiscordUser targetUser,
            [RemainingText, Description("Combined argument for the time and reason for the mute. For example '1h rule 7' or 'rule 10'")] string timeAndReason = "No reason specified."
        )
        {
            DiscordMember targetMember;
            try
            {
                targetMember = await ctx.Guild.GetMemberAsync(targetUser.Id);
            }
            catch (DSharpPlus.Exceptions.NotFoundException)
            {
                // TODO: Rework mutes to allow this
                await ctx.Message.DeleteAsync();
                var msg = await ctx.Channel.SendMessageAsync($"{Program.cfgjson.Emoji.Error} The user you're trying to mute is not in the server!");
                await Task.Delay(3000);
                await msg.DeleteAsync();
                return;
            }

            if (Warnings.GetPermLevel(ctx.Member) == ServerPermLevel.TrialMod && (Warnings.GetPermLevel(targetMember) >= ServerPermLevel.TrialMod || targetMember.IsBot))
            {
                await ctx.Channel.SendMessageAsync($"{Program.cfgjson.Emoji.Error} {ctx.User.Mention}, as a Trial Moderator you cannot perform moderation actions on other staff members or bots.");
                return;
            }

            await ctx.Message.DeleteAsync();

            TimeSpan muteDuration = default;
            string possibleTime = timeAndReason.Split(' ').First();
            if (possibleTime.Length != 1)
            {
                string reason = timeAndReason;
                // Everything BUT the last character should be a number.
                string possibleNum = possibleTime.Remove(possibleTime.Length - 1);
                if (int.TryParse(possibleNum, out int timeLength))
                {
                    char possibleTimePeriod = possibleTime.Last();
                    muteDuration = ModCmds.ParseTime(possibleTimePeriod, timeLength);
                }
                else
                {
                    muteDuration = default;
                }

                if (muteDuration != default || possibleNum == "0")
                {
                    if (!timeAndReason.Contains(" "))
                        reason = "No reason specified.";
                    else
                    {
                        reason = timeAndReason.Substring(timeAndReason.IndexOf(' ') + 1, timeAndReason.Length - (timeAndReason.IndexOf(' ') + 1));
                    }
                }

                // await ctx.Channel.SendMessageAsync($"debug: {possibleNum}, {possibleTime}, {muteDuration.ToString()}, {reason}");
                _ = MuteUserAsync(targetMember, reason, ctx.User.Id, ctx.Guild, ctx.Channel, muteDuration, true);
            }
        }


        public async Task<bool> MuteUserAsync(DiscordMember naughtyMember, string reason, ulong moderatorId, DiscordGuild guild, DiscordChannel channel = null, TimeSpan muteDuration = default, bool alwaysRespond = false)
        {
            bool permaMute = false;
            DiscordChannel logChannel = await _client.GetChannelAsync(Program.cfgjson.LogChannel);
            DiscordRole mutedRole = guild.GetRole(Program.cfgjson.MutedRole);
            DateTime? expireTime = DateTime.Now + muteDuration;
            DiscordMember moderator = await guild.GetMemberAsync(moderatorId);

            if (muteDuration == default)
            {
                permaMute = true;
                expireTime = null;
            }

            MemberPunishment newMute = new MemberPunishment()
            {
                MemberId = naughtyMember.Id,
                ModId = moderatorId,
                ServerId = guild.Id,
                ExpireTime = expireTime
            };

            await Program.db.HashSetAsync("mutes", naughtyMember.Id, JsonConvert.SerializeObject(newMute));

            try
            {
                await naughtyMember.GrantRoleAsync(mutedRole, $"[Mute by {moderator.Username}#{moderator.Discriminator}]: {reason}");
            }
            catch
            {
                return false;
            }

            try
            {
                if (permaMute)
                {
                    await logChannel.SendMessageAsync($"{Program.cfgjson.Emoji.Muted} {naughtyMember.Mention} was successfully muted by `{moderator.Username}#{moderator.Discriminator}` (`{moderatorId}`).\nReason: **{reason}**");
                    await naughtyMember.SendMessageAsync($"{Program.cfgjson.Emoji.Muted} You have been muted in **{guild.Name}**!\nReason: **{reason}**");
                }

                else
                {
                    await logChannel.SendMessageAsync($"{Program.cfgjson.Emoji.Muted} {naughtyMember.Mention} was successfully muted for {Warnings.TimeToPrettyFormat(muteDuration, false)} by `{moderator.Username}#{moderator.Discriminator}` (`{moderatorId}`).\nReason: **{reason}**");
                    await naughtyMember.SendMessageAsync($"{Program.cfgjson.Emoji.Muted} You have been muted in **{guild.Name}** for {Warnings.TimeToPrettyFormat(muteDuration, false)}!\nReason: **{reason}**");
                }
            }
            catch
            {
                // A DM failing to send isn't important, but let's put it in chat just so it's somewhere.
                if (!(channel is null))
                {
                    if (muteDuration == default)
                        await channel.SendMessageAsync($"{Program.cfgjson.Emoji.Muted} {naughtyMember.Mention} has been muted: **{reason}**");
                    else
                        await channel.SendMessageAsync($"{Program.cfgjson.Emoji.Muted} {naughtyMember.Mention} has been muted for **{Warnings.TimeToPrettyFormat(muteDuration, false)}**: **{reason}**");
                    return true;
                }
            }

            if (!(channel is null) && alwaysRespond)
            {
                reason = reason.Replace("`", "\\`").Replace("*", "\\*");
                if (muteDuration == default)
                    await channel.SendMessageAsync($"{Program.cfgjson.Emoji.Muted} {naughtyMember.Mention} has been muted: **{reason}**");
                else
                    await channel.SendMessageAsync($"{Program.cfgjson.Emoji.Muted} {naughtyMember.Mention} has been muted for **{Warnings.TimeToPrettyFormat(muteDuration, false)}**: **{reason}**");
            }
            return true;
        }

        public async Task<bool> UnmuteUserAsync(DiscordUser targetUser)
        {
            DiscordGuild guild = await _client.GetGuildAsync(Program.cfgjson.ServerID);
            DiscordChannel logChannel = await _client.GetChannelAsync(Program.cfgjson.LogChannel);

            // todo: store per-guild
            DiscordRole mutedRole = guild.GetRole(Program.cfgjson.MutedRole);
            DiscordMember member = null;
            try
            {
                member = await guild.GetMemberAsync(targetUser.Id);
            }
            catch
            {
                // they probably left :(
            }

            if (member == null)
            {
                await logChannel.SendMessageAsync($"{Program.cfgjson.Emoji.Information} Attempt to remove Muted role from <@{targetUser.Id}> failed because the user could not be found.\nThis is expected if the user was banned or left.");
            }
            else
            {
                // Perhaps we could be catching something specific, but this should do for now.
                try
                {
                    await member.RevokeRoleAsync(mutedRole);
                    await logChannel.SendMessageAsync($"{Program.cfgjson.Emoji.Information} Successfully unmuted <@{targetUser.Id}>!");
                }
                catch
                {
                    await logChannel.SendMessageAsync($"{Program.cfgjson.Emoji.Error} Attempt to removed Muted role from <@{targetUser.Id}> failed because of a Discord API error!" +
                    $"\nIf the role was removed manually, this error can be disregarded safely.");
                }
            }
            // Even if the bot failed to remove the role, it reported that failure to a log channel and thus the mute
            //  can be safely removed internally.
            await Program.db.HashDeleteAsync("mutes", targetUser.Id);

            return true;
        }

        public async Task<bool> CheckMutesAsync(bool includeRemutes = false)
        {
            DiscordChannel logChannel = await _client.GetChannelAsync(Program.cfgjson.LogChannel);
            Dictionary<string, MemberPunishment> muteList = Program.db.HashGetAll("mutes").ToDictionary(
                x => x.Name.ToString(),
                x => JsonConvert.DeserializeObject<MemberPunishment>(x.Value)
            );
            if (muteList == null | muteList.Keys.Count == 0)
                return false;
            else
            {
                // The success value will be changed later if any of the unmutes are successful.
                bool success = false;
                foreach (KeyValuePair<string, MemberPunishment> entry in muteList)
                {
                    MemberPunishment mute = entry.Value;
                    if (DateTime.Now > mute.ExpireTime)
                        await UnmuteUserAsync(await _client.GetUserAsync(mute.MemberId));
                    else if (includeRemutes)
                    {
                        try
                        {
                            var guild = await _client.GetGuildAsync(mute.ServerId);
                            var member = await guild.GetMemberAsync(mute.MemberId);
                            if (member != null)
                            {
                                var muteRole = guild.GetRole(Program.cfgjson.MutedRole);
                                await member.GrantRoleAsync(muteRole);
                            }
                        }
                        catch
                        {
                            // nothing
                        }

                    }
                }
#if DEBUG
                Console.WriteLine($"Checked mutes at {DateTime.Now} with result: {success}");
#endif
                return success;
            }
        }
    }
}
