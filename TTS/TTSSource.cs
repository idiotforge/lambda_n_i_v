using FFMpegCore;
using FFMpegCore.Pipes;
using System.Diagnostics;

namespace Lambdaniv.TTS;
public abstract class TTSSource
{
  public virtual async Task<FFMpegArgumentProcessor?> GetFFMpegArgs(Stream output, CancellationToken cancellationToken)
  {
    return null;
  }
}
public class FileSource(string filename) : TTSSource
{
  public override async Task<FFMpegArgumentProcessor?> GetFFMpegArgs(Stream output, CancellationToken cancellationToken)
  {
    return FFMpegArguments.FromFileInput(filename).OutputToPipe(new StreamPipeSink(output), options => options
    .ForceFormat("s16le")
    .WithAudioSamplingRate(48000)
    .WithCustomArgument("-ac 2")).CancellableThrough(cancellationToken);
  }
}
public class WebSource(string url) : TTSSource
{
  public override async Task<FFMpegArgumentProcessor?> GetFFMpegArgs(Stream output, CancellationToken cancellationToken)
  {
    try
    {
      return FFMpegArguments.FromUrlInput(new Uri(url)).OutputToPipe(new StreamPipeSink(output), options => options
      .ForceFormat("s16le")
      .WithAudioSamplingRate(48000)
      .WithCustomArgument("-ac 2")).CancellableThrough(cancellationToken);
    }
    catch (Exception)
    {
      return null;
    }
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
      .Replace('/', '+')
      .Replace('\\', '+')
      .Replace("&", "and")
      .Replace("#", "hashtag");
    pitch = up.Pitch;
    tempo = up.Tempo;
    url = $"{TTSConfig.ApiUrl}?text_speaker={up.VoiceName}&req_text={text}&speaker_map_type=0&aid=1233";
  }

  public override async Task<FFMpegArgumentProcessor?> GetFFMpegArgs(Stream output, CancellationToken cancellationToken)
  {
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("User-Agent", TTSConfig.UserAgent);
    client.DefaultRequestHeaders.Add("Cookie", $"sessionid={TTSConfig.TikTokToken}");
    try
    {
      using var response = await client.PostAsync(url, null, cancellationToken);
      string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
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
      ).CancellableThrough(cancellationToken);
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
  public override async Task<FFMpegArgumentProcessor?> GetFFMpegArgs(Stream output, CancellationToken cancellationToken)
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
    if (ytdlp == null)
    {
      return null;
    }
    cancellationToken.Register(() => ytdlp.Kill());
    return FFMpegArguments.FromPipeInput(new StreamPipeSource(ytdlp.StandardOutput.BaseStream)).OutputToPipe(new StreamPipeSink(output), options => options
      .ForceFormat("s16le")
      .WithAudioSamplingRate(48000)
      .WithCustomArgument("-ac 2")).CancellableThrough(cancellationToken);
  }
}