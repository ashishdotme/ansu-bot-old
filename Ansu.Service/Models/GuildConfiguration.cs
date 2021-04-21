using System;
using Ansu.Service.Models;

namespace Ansu.Bot.Service.Models
{
    public sealed class GuildConfiguration
    {
        internal GuildConfiguration()
        {
            Moderation = new ModerationOptions();
        }

        public ModerationOptions Moderation { get; set; }

    }
}
