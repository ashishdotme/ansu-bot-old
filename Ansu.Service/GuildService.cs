using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ansu.Bot.Service.Models;
using Ansu.Repository.Interfaces;
using Ansu.Service.Interfaces;
using Ansu.Service.Models;

namespace Ansu.Bot.Service
{
    public class GuildService : IGuildService
    {
        private readonly IGuildRepository _guildRepository;

        public GuildService(IGuildRepository guildRepository)
        {
            _guildRepository = guildRepository;
        }

        public async Task DeleteGuild(string guildId)
        {
            await _guildRepository.DeleteGuild(guildId).ConfigureAwait(false);
        }

        public async Task<List<Guild>> GetAllGuilds()
        {
            var guilds = await _guildRepository.GetGuilds(new GuildContext()).ConfigureAwait(false);
            return guilds;
        }

        public async Task SaveGuild(Guild guild)
        {
            await _guildRepository.SaveGuild(guild).ConfigureAwait(false);
        }
    }
}
