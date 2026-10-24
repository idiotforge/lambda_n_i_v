using Lambdaniv.TTS;
using NetCord;
using NetCord.Gateway;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
namespace Lambdaniv;
internal static class MessageUtil
{
  // false if bot message, message that start with a common command prefix or system message
  public static bool IsValid(this Message message)
  {
    if (
      (message.Type != MessageType.Default && message.Type != MessageType.Reply)
      || message.Author.IsBot
      || message.Content.StartsWith('$')
    )
    {
      return false;
    }
    return true;
  }
  public static async Task<string> CleanContent(this Message message)
  {
    string SpeakAttachments()
    {
      if (message.Attachments.Count == 1)
      {
        string? type = message.Attachments[0].ContentType;
        // idk if thats how mime types work
        if (type != null)
        {
          string typeName = type.Split('/')[0];
          // this is wrong but also correct like 90% of the time
          if ("aeiouAEIOU".Contains(typeName.First()))
          {
            return $"an {typeName} file";
          }
          return $"a {typeName} file";
        }
        return "a file";
      }
      else if (message.Attachments.Count > 0)
      {
        return "multiple files";
      }
      return string.Empty;
    }
    string text = message.Content;
    if (text.Length > 0)
    {
      text = text.Replace('\n', ' ');
      text = EmojiJson.Demojify(text);
      Guild guild = message.Guild!;
      async Task<string> CleanWord(string word)
      {
        if (word.StartsWith("http"))
        {
          // links
          if (word.Contains("gif") || word.Contains("tenor"))
          {
            return "a gif";
          }
          return "a link";
        }
        if (word.StartsWith('<'))
        {
          switch (word[1])
          {
            case ':':
              // emoji
              return "emoji " + word[2..(word.LastIndexOf(':'))];
            case '#':
              // channel name
              ulong channelId = ulong.Parse(word[2..(word.Length - 1)]);
              if (guild.Channels.TryGetValue(channelId, out var channel))
              {
                return "hashtag " + channel.Name;
              }
              break;
            case '@':
              // username
              ulong userId = ulong.Parse(word[2..(word.Length - 1)]);
              if (await guild.GetUserAsync(userId) is not { } user)
              {
                break;
              }
              return "at " + (user.Nickname ?? user.GlobalName);
          }
        }
        return word;
      }
      text = text.Replace("<", " <")
        .Replace(">", "> ");
      string cleaned = "";
      foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
      {
        cleaned += await CleanWord(word) + ' ';
      }
      string attachments = SpeakAttachments();
      if (attachments != string.Empty)
      {
        cleaned += "with " + attachments;
      }
      return cleaned[0..(cleaned.Length - 1)] + '.';
    }
    else
    {
      if (message.Stickers.Count > 0)
      {
        return $"sticker {message.Stickers[0].Name}.";
      }
      return SpeakAttachments();
    }
  } 
}
