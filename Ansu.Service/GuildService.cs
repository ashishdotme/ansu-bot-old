using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ansu.Bot.Service.Models;
using Ansu.Repository.Interfaces;
using Ansu.Service.Interfaces;
using Ansu.Service.Models;
using Serilog;

namespace Ansu.Bot.Service
{
    public class GuildService : IGuildService
    {
        private readonly IGuildRepository _guildRepository;
        private readonly ILogger _logger;

        public GuildService(IGuildRepository guildRepository, ILogger logger)
        {
            _guildRepository = guildRepository;
            _logger = logger;
        }

        public async Task DeleteGuild(ulong guildId)
        {
            await _guildRepository.DeleteGuild(guildId).ConfigureAwait(false);
        }

        public async Task<Guild> GetGuild(ulong guildId)
        {
            return await _guildRepository.GetGuild(guildId).ConfigureAwait(false);
        }

        public async Task<List<Guild>> GetAllGuilds()
        {
            var guilds = await _guildRepository.GetGuilds(new GuildContext()).ConfigureAwait(false);
            return guilds;
        }

        public async Task SaveGuild(Guild guild)
        {
            try
            {
                await _guildRepository.SaveGuild(guild).ConfigureAwait(false);
                _logger.Information($"Saved guild settings for {guild.Id}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save guild settings for {guild.Id}");
                _logger.Error($"Guild service exception : {ex.Message}");
            }
        }
    }
}
