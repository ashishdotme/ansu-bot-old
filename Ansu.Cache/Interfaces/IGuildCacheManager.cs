using System;
using System.Threading.Tasks;
using Ansu.Bot.Service.Models;

namespace Ansu.Cache.Interfaces
{
    public interface IGuildCacheManager
    {
        Task<Guild> GetGuild(ulong guildId);

        Task SaveGuild(Guild guild);

        Task ClearCache(string guildId);
    }
}
