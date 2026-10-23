using Lambdaniv.TTS;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lambdaniv;
public class MessageCreateHandler() : IMessageCreateGatewayHandler
{
  public ValueTask HandleAsync(Message message)
  {
    if (message.Guild is not { } guild)
    {
      return default;
    }
    try
    {
      if (guild.GetCurrentUserVoiceStateAsync().Result.ChannelId == message.ChannelId)
      {
        if (message.IsValid())
        {
          string hardcoded = HardcodedSounds.GetHardcodedFile(message.Content);
          if (hardcoded != string.Empty)
          {
            TTSCommands.Play(guild.Id, new FileSource(hardcoded));
          }
          else
          {
            UserPreference up = LiteDbProvider.GetPreference(message.Author.Id) ?? new UserPreference();
            string cleanContent = message.CleanContent().Result;
            TTSCommands.Play(guild.Id, new TikTokSource(cleanContent, up));
          }
        }
      }
    }
    catch (AggregateException) { }
    return default;
  }
}
