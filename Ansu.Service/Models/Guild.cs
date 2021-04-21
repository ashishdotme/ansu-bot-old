using System;
using System.Text.Json;

namespace Ansu.Bot.Service.Models
{
    public class Guild
    {
        public Guild()
        {
            Configuration = new GuildConfiguration();
        }

        public ulong Id { get; set; }

        public ulong OwnerId { get; set; }

        public GuildConfiguration Configuration { get; set; }
    }
}
