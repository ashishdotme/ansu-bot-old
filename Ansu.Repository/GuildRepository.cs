using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ansu.Bot.Service.Models;
using Ansu.MongoDB.Client.Impl;
using Ansu.Repository.Converters;
using Ansu.Repository.Exceptions;
using Ansu.Repository.Interfaces;
using Ansu.Service.Models;
using Microsoft.Extensions.Logging;

namespace Ansu.Repository
{
    public class GuildRepository : IGuildRepository
    {
        private readonly IMongoCustomClient _mongoCustomClient;
        private readonly IMongoFilter _mongoFilter;
        private readonly ILogger _logger;

        public GuildRepository(IMongoCustomClient mongoCustomClient, IMongoFilter mongoFilter)
        {
            _mongoCustomClient = mongoCustomClient;
            _mongoFilter = mongoFilter;
        }

        public Task DeleteGuild(string guildId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Guild>> GetGuilds(GuildContext filterContext)
        {
            var filter = _mongoFilter.CreateFilterDefinition(filterContext);
            var items = await _mongoCustomClient.GetAllItemsAsync(filter).ConfigureAwait(false);
            var guilds = ModelConverter.ToGuilds(items.ToList());
            return guilds;
        }

        public async Task SaveGuild(Guild guild)
        {
            var filter = _mongoFilter.CreateFilterDefinition(new GuildContext { Id = guild.Id });
            var _guild = await _mongoCustomClient.GetAllItemsAsync(filter).ConfigureAwait(false);
            if (_guild.Any())
            {
                throw new DuplicateGuildException(guild.Id);
            }
            var mongoGuild = ModelConverter.ToMongoModel(guild);
            try
            {
                await _mongoCustomClient.InsertOneAsync(mongoGuild).ConfigureAwait(false);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
