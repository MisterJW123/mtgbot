﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheFiremind.Services;

class StartupService
{
    readonly DiscordSocketClient _client;
    readonly InteractionService _interactionService;
    readonly IHostEnvironment _environment;
    readonly IServiceProvider _services;

    string AuthToken => Configuration.GetValue<string>("TheFiremindDiscordAuthToken");

    internal IConfiguration Configuration { get; }

    public StartupService(DiscordSocketClient client, InteractionService interactionService, IHostEnvironment environment, IServiceProvider services, IConfiguration configuration)
    {
        _client = client;
        _interactionService = interactionService;
        _environment = environment;
        _services = services;
        Configuration = configuration;
    }

    internal void RegisterSocketClientEventHandlers()
    {
        _client.Log += message =>
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Log.Fatal(message.Exception, $"{nameof(LogSeverity.Critical)} {nameof(DiscordSocketClient)} Service Error - {message.Source}; {message.Message}");
                    break;
                case LogSeverity.Error:
                    Log.Fatal(message.Exception, $"{nameof(DiscordSocketClient)} Service {nameof(LogSeverity.Error)} - {message.Source}; {message.Message}");
                    break;
                case LogSeverity.Warning:
                    Log.Fatal(message.Exception, $"{nameof(DiscordSocketClient)} Service {nameof(LogSeverity.Warning)} - {message.Source}; {message.Message}");
                    break;
                case LogSeverity.Info:
                    Log.Information($"{nameof(DiscordSocketClient)} - {message.Source}; {message.Message}");
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose($"{nameof(DiscordSocketClient)} - {message.Source}; {message.Message}");
                    break;
                case LogSeverity.Debug:
                    Log.Debug($"{nameof(DiscordSocketClient)} - {message.Source}; {message.Message}");
                    break;
                default:
                    break;
            }

            return Task.CompletedTask;
        };

        _client.GuildAvailable += async guild =>
        {
            try
            {
                if (_environment.IsDevelopment())
                {
                    await _interactionService.RegisterCommandsToGuildAsync(guild.Id);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to register commands");
            }
        };

        _client.Connected += () =>
        {
            Log.Information("Connected to Discord");
            return Task.CompletedTask;
        };

        _client.Ready += () =>
        {
            Log.Information("Finished downloading guild data");
            return Task.CompletedTask;
        };
    }

    internal async Task LoadModulesAsync() => await _interactionService.AddModuleAsync<CommandModule>(_services);

    internal async Task ConnectToDiscordAsync()
    {
        Log.Debug("Logging in to Discord...");
        await _client.LoginAsync(TokenType.Bot, AuthToken);

        Log.Debug("Opening the socket connection to Discord...");
        await _client.StartAsync();
    }
}