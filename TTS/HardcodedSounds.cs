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
    static string RandomFile(string name, int count) => $"{name}{Random.Shared.Next() % count + 1}.ogg";
    return text switch
    {
      "luigi house but it mansion" => "luigihousebutitmansion.ogg",
      "pacman" => "pacman.ogg",
      "proceed" => "weird.ogg",
      "interlope" => RandomFile("interlope", 5),
      "dawnfinder" or "lawnfinder" => "dawnfinder.ogg",
      "android in the bios" => "androidinthebios.ogg",
      "press pp button now" => "ppbutton.ogg",
      "q" => "cat_meow.ogg",
      "2 oranges and colgate challenge in the middle lets go" => "oranges.ogg",
      "gaper" => RandomFile("gaper", 2),
      "i actually suck" => "iactuallysuck.ogg",
      "i see the shoes" => "shoes.ogg",
      "i see the money" => "money.ogg",
      "fiddlesticks" or "oh fiddlesticks what now" => "fiddlesticks.ogg",
      "fatty" => "fatty.ogg",
      "adopted fatty" => "adoptedfatty.ogg",
      "fatty fatty no parents" => "fattyfattynoparents.ogg",
      "!_" => "drown.ogg",
      "think fast chucklenuts" => "chucklenuts.ogg",
      "great kind of evil ganondorf" => RandomFile("ganondorf", 3),
      "(_" => RandomFile("notif", 3),
      "what he say" => "whathesay.ogg",
      "audiojungle" => "audiojungle.ogg",
      _ => string.Empty,
    };
  } 
}
