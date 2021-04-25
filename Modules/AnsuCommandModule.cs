using System;
using System.Threading.Tasks;
using Ansu.Bot.Utils;
using ColorHelper;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Ansu.Bot.Modules
{
    public class AnsuCommandModule : BaseCommandModule
    {
        protected virtual async Task<DiscordMessage> Embed(CommandContext ctx, DiscordEmbedBuilder embed, string message = null)
        {
            return await ctx.Channel.SendMessageAsync(embed).ConfigureAwait(false);
        }

        protected virtual async Task<DiscordMessage> Error(CommandContext ctx, string title, string error)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithDescription(error)
                .WithTimestamp(DateTime.Now)
                .WithColor(DiscordColor.Red);

            return await Embed(ctx, embedBuilder).ConfigureAwait(false);
        }

        protected virtual async Task<DiscordMessage> Success(CommandContext ctx, string title, string message)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithDescription(message)
                .WithTimestamp(DateTime.Now)
                .WithColor(new DiscordColor(ColorGenerator.GetLightRandomColor<RGB>().ToString()));

            return await Embed(ctx, embedBuilder).ConfigureAwait(false);
        }
    }
}
