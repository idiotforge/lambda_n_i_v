using NetCord.Gateway.Voice;
using NetCord.Services;
using System.Diagnostics;

namespace Lambdaniv.TTS;
internal sealed class VoiceInstance(VoiceClient client) : IDisposable
{
  public VoiceClient Client => client;
  private CancellationTokenSource cancellationTokenSource = new();
  public readonly Queue<TTSSource> TTSQueue = new();
  private byte isPlaying = 0;
  public async void EnqueueAndPlay(TTSSource source)
  {
    if (isPlaying != 0)
    {
      TTSQueue.Enqueue(source);
      return;
    }
    Play(source);
  }
  public async void Play(TTSSource source)
  {
    using var job = TryEnterJob();
    if (job is not { CancellationToken: var cancellationToken })
    {
      return;
    }
    var voiceClient = client;
    try
    {
      await voiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone));
    }
    catch (TaskCanceledException)
    {
      voiceClient.Abort();
      return;
    }
    using var voiceStream = voiceClient.CreateVoiceStream();
    using OpusEncodeStream opusEncodeStream = new(voiceStream,
      PcmFormat.Short,
      VoiceChannels.Stereo,
      OpusApplication.Audio
      );
    var ffmpeg = await source.GetFFMpegArgs(opusEncodeStream, cancellationToken);
    if (ffmpeg is not null)
    {
      try
      {
        await ffmpeg.ProcessAsynchronously();
        await opusEncodeStream.FlushAsync(cancellationToken);
      }
      catch (Exception ex)
      {
        if (ex is not OperationCanceledException and not AggregateException { InnerException: OperationCanceledException })
        {
          throw;
        }
      }
    }
  }
  public Job? TryEnterJob()
  {
    return Interlocked.CompareExchange(ref isPlaying, 1, 0) is 0
      ? new Job(this, cancellationTokenSource.Token)
      : null;
  }
  public bool IsPlaying()
  {
    return isPlaying != 0;
  }
  public void Skip()
  {
    TTSQueue.Clear();
    cancellationTokenSource.Cancel();
    cancellationTokenSource.Dispose();
    cancellationTokenSource = new CancellationTokenSource();
  }

  public void Dispose()
  {
    var tokenSource = cancellationTokenSource;
    TTSQueue.Clear();
    tokenSource.Cancel();
    tokenSource.Dispose();
    client.Dispose();
  }
  public void OnStopPlaying()
  {
    if (TTSQueue.Count > 0)
    {
      Play(TTSQueue.Dequeue());
    }
  }

  public readonly record struct Job(VoiceInstance Instance,
    CancellationToken CancellationToken
    ) : IDisposable
  {
    public void Dispose()
    {
      Interlocked.Exchange(ref Instance.isPlaying, 0);
      Instance.OnStopPlaying();
    }
  }
}