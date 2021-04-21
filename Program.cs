using Ansu.Bot.Config.Models;
using Ansu.Bot.EventHandlers;
using Ansu.Modules;
using Ansu.Redis.Client.Impl;
using Ansu.Redis.Client.Interfaces;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Ansu.Repository;
using Ansu.Repository.Interfaces;
using Ansu.Service.Interfaces;
using Ansu.Bot.Service;
using Ansu.MongoDB.Client.Impl;
using Ansu.MongoDB.Client.Interfaces;
using MongoSettings = Ansu.Bot.Config.Models.MongoSettings;
using Ansu.MongoDB.Client;
using Ansu.Bot.Service.Models;
using Ansu.Service.Models;
using System.Collections.Generic;

namespace Ansu
{
    class Program : BaseCommandModule
    {
        private InteractivityExtension Interactivity { get; }
        public IServiceProvider services { get; set; }
        public static DiscordClient discord;
        static CommandsNextExtension commands;
        public static Random rnd = new Random();
        public static ConfigJson cfgjson;
        public static ConnectionMultiplexer redis;
        public static IDatabase db;
        private BotEventHandler _events;

        public static void Main(string[] args)
        {
            var prog = new Program();
            prog.MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
#if DEBUG
            LogLevel MinimumLogLevel = LogLevel.Debug;
#else
            LogLevel MinimumLogLevel = LogLevel.Error;
#endif
            services = new ServiceCollection()
            .AddLogging(o => o.AddConsole())
            .AddLogging(o => o.SetMinimumLevel(MinimumLogLevel))
            .AddSingleton<IRedisSettings>(config => new RedisSettings()
            {
                Host = cfgjson.Redis.Host,
                Port = cfgjson.Redis.Port,
                Password = cfgjson.Redis.Password,
                Database = "1",
                Timeout = "0"
            })
            .AddSingleton<IMongoSettings>(config => new MongoSettings()
            {
                ConnectionString = cfgjson.MongoDB.ConnectionString,
                Database = cfgjson.MongoDB.Database,
                Collection = cfgjson.MongoDB.Collection
            })
            .AddTransient<ModCmds>()
            .AddSingleton<IRedisClient, RedisClient>()
            .AddTransient<IGuildRepository, GuildRepository>()
            .AddTransient<IGuildService, GuildService>()
            .AddMongoCustomClient()
            .AddTransient<IMongoFilter, MongoFilter>()
            .BuildServiceProvider(true);

            string token;
            var json = "";

            string configFile = "config.json";
#if DEBUG
            configFile = "config.dev.json";
#endif

            using (var fs = File.OpenRead(configFile))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json);

            var keys = cfgjson.WordListList.Keys;
            foreach (string key in keys)
            {
                var listOutput = File.ReadAllLines($"Lists/{key}");
                cfgjson.WordListList[key].Words = listOutput;
            }

            if (Environment.GetEnvironmentVariable("ANSU_TOKEN") != null)
                token = Environment.GetEnvironmentVariable("ANSU_TOKEN");
            else
                token = cfgjson.Core.Token;

            string redisHost;
            if (Environment.GetEnvironmentVariable("REDIS_DOCKER_OVERRIDE") != null)
                redisHost = "redis";
            else
                redisHost = cfgjson.Redis.Host;
            redis = ConnectionMultiplexer.Connect($"{redisHost}:{cfgjson.Redis.Port}, password={cfgjson.Redis.Password}");
            db = redis.GetDatabase();
            db.KeyDelete("messages");

            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
                Intents = DiscordIntents.All
            });


            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = cfgjson.Core.Prefixes,
                Services = services
            }); ;

            commands.RegisterCommands<Warnings>();
            commands.RegisterCommands<MuteCmds>();
            commands.RegisterCommands<UserRoleCmds>();
            commands.RegisterCommands<ModCmds>();


            AddEvents();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private void AddEvents()
        {
            var _redisClient = services.GetService<IRedisClient>();
            var _guildService = services.GetService<IGuildService>();
            var _modCmds = services.GetService<ModCmds>();
            _events = new BotEventHandler(_redisClient, _modCmds, discord, _guildService);
        }

    }

}
