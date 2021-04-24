
using System;
using System.Threading.Tasks;
using Ansu.Bot.Service.Models;
using Ansu.Cache.Interfaces;
using Ansu.Redis.Client.Interfaces;
using Serilog;

namespace Ansu.Cache.Impl
{
    public class GuildCacheManager : IGuildCacheManager
    {
        private readonly IRedisClient _redisClient;
        private readonly ILogger _logger;

        public GuildCacheManager(IRedisClient redisClient, ILogger logger)
        {
            _redisClient = redisClient;
            _logger = logger;
        }

        public async Task ClearCache()
        {
            await _redisClient.Remove("guilds");
        }

        public async Task<Guild> GetGuild(ulong guildId)
        {
            try
            {
                var guild = await _redisClient.GetHash<Guild>("guilds", guildId.ToString());
                if (guild != null || guild != default(Guild))
                {
                    return guild;
                } else
                {
                    return default(Guild);
                }
            }
            catch(Exception ex)
            {
                _logger.Error($"Redis Exception: {ex.Message}");
                return default(Guild);
            }
        }

        public async Task SaveGuild(Guild guild)
        {
            try
            {
                await _redisClient.SetHash<Guild>("guilds", guild.Id.ToString(), guild);
            }
            catch (Exception ex)
            {
                _logger.Error($"Redis Exception: {ex.Message}");
            }
        }
    }
}
