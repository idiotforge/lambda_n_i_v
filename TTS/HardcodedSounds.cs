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
      "interlope" => $"interlope{Random.Shared.Next() % 5 + 1}.ogg",
      "dawnfinder" or "lawnfinder" => "dawnfinder.ogg",
      "android in the bios" => "androidinthebios.ogg",
      "press pp button now" => "ppbutton.ogg",
      "q" => "cat_meow.ogg",
      "2 oranges and colgate challenge in the middle lets go" => "oranges.ogg",
      "gaper" => $"gaper{Random.Shared.Next() % 2 + 1}.ogg",
      "i actually suck" => "iactuallysuck.ogg",
      "i see the shoes" => "shoes.ogg",
      "i see the money" => "money.ogg",
      "fiddlesticks" or "oh fiddlesticks what now" => "fiddlesticks.ogg",
      "fatty" => "fatty.ogg",
      "adopted fatty" => "adoptedfatty.ogg",
      "fatty fatty no parents" => "fattyfattynoparents.ogg",
      "!_" => "drown.ogg",
      "think fast chucklenuts" => "chucklenuts.ogg",
      _ => string.Empty,
    };
  } 
}
