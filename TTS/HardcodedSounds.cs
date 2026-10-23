using System;
using System.Collections.Generic;
using System.Text;

namespace Lambdaniv.TTS;
internal static class HardcodedSounds
{
  public static string GetHardcodedFile(string text)
  {
    string fileName = GetHardcodedFileName(text.ToLower());
    if (fileName == string.Empty)
    {
      return string.Empty;
    }
    return TTSConfig.SoundFolder + fileName;
  }
  private static string GetHardcodedFileName(string text)
  {
    return text switch
    {
      "luigi house but it mansion" => "luigihousebutitmansion.ogg",
      "pacman" => "pacman.ogg",
      "proceed" => "weird.ogg",
      "interlope" => "interlope" + (Random.Shared.Next() % 5 + 1) + ".ogg",
      "dawnfinder" or "lawnfinder" => "dawnfinder.ogg",
      "android in the bios" => "androidinthebios.ogg",
      _ => string.Empty,
    };
  } 
}
