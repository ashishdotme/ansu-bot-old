﻿using System;
using System.Linq;
using Ansu.Bot.Service.Models;
using Ansu.Repository.Interfaces;
using Ansu.Repository.Models;
using Ansu.Repository.Utils;
using MongoDB.Driver;

namespace Ansu.Repository
{
    public class GuildUpdateDefinitionCreator : IGuildUpdateDefinitionCreator
    {

        public UpdateDefinition<MongoGuild> CreateUpdateDefinition(Guild guild)
        {
            return Builders<MongoGuild>.Update.Set(x => x.Document.Reminders, guild.Reminders);
        }
    }
}
