using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Lambdaniv.TTS;
public class TTSSource
{
  public virtual async Task<FFMpegArgumentProcessor?> GetFFMpegArgs(Stream output)
  {
    return null;
  }
}
public class FileSource(string filename) : TTSSource
{
  public readonly string Filename = filename;
  public override async Task<FFMpegArgumentProcessor?> GetFFMpegArgs(Stream output)
  {
    return FFMpegArguments.FromFileInput(Filename).OutputToPipe(new StreamPipeSink(output), options => options
    .ForceFormat("s16le")
    .WithAudioSamplingRate(48000)
    .WithCustomArgument("-ac 2"));
  }
}
public class TikTokSource : TTSSource
{
  private string url = "";
  private float pitch;
  private float tempo;
  public TikTokSource(string text, UserPreference up)
  {
    text = text
      .Replace("+", "plus")
      .Replace(' ', '+')
      .Replace("&", "and")
      .Replace("#", "hashtag");
    pitch = up.Pitch;
    tempo = up.Tempo;
    url = $"{TTSConfig.ApiUrl}?text_speaker={up.VoiceName}&req_text={text}&speaker_map_type=0&aid=1233";
  }

  public override async Task<FFMpegArgumentProcessor?> GetFFMpegArgs(Stream output)
  {
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("User-Agent", TTSConfig.UserAgent);
    client.DefaultRequestHeaders.Add("Cookie", $"sessionid={TTSConfig.TikTokToken}");
    try
    {
      using var response = await client.PostAsync(url, null);
      string responseJson = await response.Content.ReadAsStringAsync();
      // i hate json so im just getting the data string by hand lol
      int startIndex = responseJson.IndexOf("v_str") + 8;
      int length = responseJson.IndexOf('"', startIndex) - startIndex;
      string voiceBase64 = responseJson.Substring(startIndex, length);
      var stream = new MemoryStream(Convert.FromBase64String(voiceBase64));
      return FFMpegArguments.FromPipeInput(new StreamPipeSource(stream)).OutputToPipe(new StreamPipeSink(output),
        options =>
        {
          if (tempo == 1 && pitch == 1)
          {
            options.WithAudioSamplingRate(48000);
          }
          else
          {
            options.WithCustomArgument($"-af \"asetrate=24000*{pitch},atempo={tempo / pitch},aresample=48000\"");
          }
          options.ForceFormat("s16le").WithCustomArgument("-ac 2");
        }
      );
    }
    catch
    {
      return null;
    }
  }
}
public class YTSource(string url) : TTSSource
{
  private readonly string url = url;
  public override async Task<FFMpegArgumentProcessor?> GetFFMpegArgs(Stream output)
  {
    var ytdlp = Process.Start(new ProcessStartInfo
    {
      FileName = "./yt-dlp",
      ArgumentList =
      {
        "-o", "-",
        "-x", url
      },
      RedirectStandardOutput = true
    });
    //var stream = new MemoryStream();
    //ytdlp.StandardOutput.BaseStream.CopyTo(stream);
    return FFMpegArguments.FromPipeInput(new StreamPipeSource(ytdlp.StandardOutput.BaseStream)).OutputToPipe(new StreamPipeSink(output), options => options
      .ForceFormat("s16le")
      .WithAudioSamplingRate(48000)
      .WithCustomArgument("-ac 2"));
  }
}