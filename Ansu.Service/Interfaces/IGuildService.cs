using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ansu.Bot.Service.Models;

namespace Ansu.Service.Interfaces
{
    public interface IGuildService
    {
        Task SaveGuild(Guild guild);

        Task<List<Guild>> GetAllGuilds();

        Task DeleteGuild(string guildId);
    }
}
