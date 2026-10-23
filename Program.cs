using FFMpegCore;
using FFMpegCore.Pipes;
using Lambdaniv.TTS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.Commands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
namespace Lambdaniv;

internal class Program
{
  static async Task Main(string[] args)
  {
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services
        .AddDiscordGateway(options =>
        options.Intents =
        GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates
        )
        .AddCommands(options =>
        options.Prefixes = ["$", "λ"]
        ).AddGatewayHandler<MessageCreateHandler>();
    TTSConfig.TikTokToken = builder.Configuration.GetValue<string>("Lambdaniv:TikTokToken")!;
    var host = builder.Build();
    host.AddCommandModule<TTSCommands>();
    await host.RunAsync();
  }
}