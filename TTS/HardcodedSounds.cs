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
    return TTSConfig.SoundFolder + fileName + ".ogg";
  }
  private static string GetHardcodedFileName(string text)
  {
    static string RandomFile(string name, int count) => $"{name}{Random.Shared.Next() % count + 1}";
    return text switch
    {
      "luigi house but it mansion" => "luigihousebutitmansion",
      "pacman" => "pacman",
      "proceed" => "weird",
      "interlope" => RandomFile("interlope", 5),
      "dawnfinder" or "lawnfinder" => "dawnfinder",
      "android in the bios" => "androidinthebios",
      "press pp button now" => "ppbutton",
      "q" => "cat_meow",
      "2 oranges and colgate challenge in the middle lets go" => "oranges",
      "gaper" => RandomFile("gaper", 2),
      "i actually suck" => "iactuallysuck",
      "i see the shoes" => "shoes",
      "i see the money" => "money",
      "fiddlesticks" or "oh fiddlesticks what now" => "fiddlesticks",
      "fatty" => "fatty",
      "adopted fatty" => "adoptedfatty",
      "fatty fatty no parents" => "fattyfattynoparents",
      "!_" => "drown",
      "think fast chucklenuts" => "chucklenuts",
      "great king of evil ganondorf" => RandomFile("ganondorf", 3),
      "kindness luigi" => "kindnessluigi",
      "(_" => RandomFile("notif", 3),
      "what he say" => "whathesay",
      "audiojungle" => "audiojungle",
      "archipelago" => "archipelago",
      ">w<" => "b3313",
      ":3" => "rebelrush",
      ":3c" => "scream",
      "thats a bug" or "that's a bug" => "bug",
      "meteor happens" => "meteorhappens",
      "forge" => "startup",
      "about that beer I owed ya" => "thatbeer",
      "travis scott" or "travis touchdown" => "travisscott",
      "luigi i just want to touch you" => "pbgluigi",
      
      "yea what do you want" => "yeawhatdoyouwant",
      "you got any glazed donuts" => "yougotanyglazeddonuts",
      "nah were out of glazed donuts" => "nahwereoutofglazeddonuts",
      "you got any jelly donuts" => "yougotanyjellydonuts",
      "nah were out of jelly donuts" => "nahwereoutofjellydonuts",
      "you got any bavarian cream filled donuts" => "yougotanybavariancreamfilleddonuts",
      "nah were out of bavarian cream filled donuts" => "nahwereoutofbavariancreamfilleddonuts",
      "you got any cinnamon rolls" => "yougotanycinnamonrolls",
      "nah were out of cinnamon rolls" => "nahwereoutofcinnamonrolls",
      "you got any apple fritters" => "yougotanyapplefritters",
      "nah were out of apple fritters" => "nahwereoutofapplefritters",
      "you got any bear claws" => "yougotanybearclaws",
      "wait a minute ill go check" or "wait a minute i'll go check" => "waitaminuteillgocheck",
      "intermission" => "intermission",
      "nah were out of bear claws" => "nahwereoutofbearclaws",
      _ => string.Empty,
    };
  } 
}
