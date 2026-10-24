using FFMpegCore.Enums;
using NetCord;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lambdaniv.TTS;

public class TTSCommands : CommandModule<CommandContext>
{
  static ConcurrentDictionary<ulong, VoiceInstance> voiceInstances = [];
  [Command("join")]
  public async void Join(IVoiceGuildChannel? channel = null)
  {
    if (Context.Guild is not { } guild)
    {
      return;
    }

    var user = Context.User;

    ulong channelId;
    if (channel is not null)
    {
      channelId = channel.Id;
    }
    else if (guild.VoiceStates.TryGetValue(user.Id, out var voiceState))
    {
      channelId = voiceState.ChannelId.GetValueOrDefault();
    }
    else
    {
      await Context.Message.ReplyAsync("have you tried connecting to a vc first?");
      return;
    }

    var guildId = guild.Id;

    if (!voiceInstances.TryAdd(guildId, null))
    {
      await Context.Message.ReplyAsync("already connected btw");
      return;
    }

    VoiceClient? voiceClient;
    try
    {
      voiceClient = await Context.Client.JoinVoiceChannelAsync(guildId, channelId, new ()
      {
        Logger = new ConsoleLogger(),
      });
    }
    catch (TimeoutException)
    {
      voiceInstances.TryRemove(item: new(guildId, null));

      await Context.Client.UpdateVoiceStateAsync(new(guildId, null));

      throw;
    }

    VoiceInstance voiceInstance = new(voiceClient);

    if (!voiceInstances.TryUpdate(guildId, voiceInstance, null))
    {
      voiceInstance.Dispose();

      await Context.Client.UpdateVoiceStateAsync(new(guildId, null));

      await Context.Message.ReplyAsync("something went horribly wrong");
      return;
    }

    voiceClient.Disconnect += args =>
    {
      if (args.Reconnect)
        return default;

      if (voiceInstances.TryRemove(item: new(guildId, voiceInstance)))
        voiceInstance.Dispose();

      return default;
    };

    try
    {
      await voiceClient.StartAsync();
    }
    catch
    {
      if (voiceInstances.TryRemove(item: new(guildId, voiceInstance)))
      {
        voiceInstance.Dispose();

        await Context.Client.UpdateVoiceStateAsync(new VoiceStateProperties(guildId, null));
      }
      return;
    }
    await Context.Message.AddReactionAsync(Emojis.Checkmark);
  }
  public static async void Play(ulong guildId, TTSSource source)
  {
    if (!voiceInstances.TryGetValue(guildId, out var voiceInstance) || voiceInstance is null)
    {
      return;
    }
    voiceInstance.EnqueueAndPlay(source);
  }
  [Command("test")]
  public async void Test()
  {
    if (Context.Guild is not { } guild)
    {
      await Context.Message.ReplyAsync("not in a guild");
      return;
    }

    ulong guildId = guild.Id;

    Play(guildId, new FileSource(TTSConfig.SoundFolder + "test.mp3"));
  }
  [Command("play")]
  public async void PlayFile(string url = "")
  {
    if (Context.Guild is not { } guild)
    {
      await Context.Message.ReplyAsync("not in a guild");
      return;
    }
    
    ulong guildId = guild.Id;
    if (url.StartsWith("https"))
    {
      Play(guildId, new WebSource(url));
      return;
    }
    if (Context.Message.Attachments.Count > 0)
    {
      if (Context.Message.Attachments[0].ContentType?.StartsWith("audio") == true)
      {
        Play(guildId, new WebSource(Context.Message.Attachments[0].ProxyUrl));
        return;
      }
    }
  }
  [Command("yt")]
  public async void PlayYT(string text)
  {
    if (Context.Guild is not { } guild)
    {
      await Context.Message.ReplyAsync("not in a guild");
      return;
    }

    ulong guildId = guild.Id;

    Play(guildId, new YTSource(text));
  }
  [Command("skip")]
  public async void Skip()
  {
    if (Context.Guild is not { } guild)
    {
      await Context.Message.ReplyAsync("not in a guild");
      return;
    }
    ulong guildId = guild.Id;
    if (!voiceInstances.TryGetValue(guildId, out var voiceInstance) || voiceInstance is null)
    {
      return;
    }

    if (voiceInstance.IsPlaying())
    {
      await Context.Message.AddReactionAsync(Emojis.Checkmark);
      voiceInstance.Skip();
    }
    else
    {
      await Context.Message.ReplyAsync("what are you even trying to skip?");
    }
  }
  private bool isValueValid(float value)
  {
    if (value > 3 || value < 0.5)
    {
      return false;
    }
    return true;
  }
  [Command("pitch")]
  public async void SetPitch(float pitch)
  {
    if (!isValueValid(pitch))
    {
      await Context.Message.ReplyAsync("invalid value");
    }
    LiteDbProvider.SetPreferencePitch(Context.User.Id, pitch);
    await Context.Message.AddReactionAsync(Emojis.Checkmark);
  }
  [Command("tempo")]
  public async void SetTempo(float tempo)
  {
    if (!isValueValid(tempo))
    {
      await Context.Message.ReplyAsync("invalid value");
    }
    LiteDbProvider.SetPreferenceTempo(Context.User.Id, tempo);
    await Context.Message.AddReactionAsync(Emojis.Checkmark);
  }
  [Command("speed")]
  public async void SetSpeed(float speed)
  {
    if (!isValueValid(speed))
    {
      await Context.Message.ReplyAsync("invalid value");
    }
    LiteDbProvider.SetPreferenceSpeed(Context.User.Id, speed);
    await Context.Message.AddReactionAsync(Emojis.Checkmark);
  }
  [Command("voice")]
  public async void SetVoice(string voice)
  {
    if (TTSConfig.Voices.Contains(voice))
    {
      LiteDbProvider.SetPreferenceVoice(Context.User.Id, voice);
      await Context.Message.AddReactionAsync(Emojis.Checkmark);
      return;
    }
    await Context.Message.ReplyAsync("invalid voice");
  }
  [Command("debuggetuserpreference")]
  public async void SetVoice(ulong userId)
  {
    if (LiteDbProvider.GetPreference(userId) is not { } up)
    {
      await ReplyAsync("not in db");
      return;
    }
    await ReplyAsync($"voicename = {up.VoiceName}, pitch = {up.Pitch}, tempo = {up.Tempo}");
  }
  [Command("kill")]
  public async void Kill()
  {
    if (Context.Guild is not { } guild)
    {
      await Context.Message.ReplyAsync("not in a guild.");
      return;
    }

    ulong guildId = guild.Id;

    if (!voiceInstances.TryGetValue(guildId, out var voiceInstance) || voiceInstance is null)
    {
      await Context.Message.ReplyAsync("not connected");
      return;
    }

    if (voiceInstances.TryRemove(item: new(guildId, voiceInstance)))
    {
      try
      {
        await voiceInstance.Client.CloseAsync();
      }
      finally
      {
        voiceInstance.Dispose();
        await Context.Client.UpdateVoiceStateAsync(new VoiceStateProperties(guildId, null));
      }
    }
  }
}
