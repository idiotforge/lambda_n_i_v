using SimdJson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lambdaniv.TTS;
internal class EmojiJson
{
  private static Dictionary<string, string> emojiDictionary = new Dictionary<string, string>();
  static EmojiJson()
  {
    // simd is here because it doesnt use reflection
    using var doc = SimdJsonParser.Shared.Parse(File.ReadAllText("./TTS/data-by-emoji.json"));
    using var obj = doc.GetObject();
    if (obj != null)
    {
      foreach (JsonProperty item in obj)
      {
        string value = item.Value.GetField("name").GetString();
        emojiDictionary.Add(item.Name, value);
      }
    }
  }
  public static string Demojify(string input)
  {
    foreach (KeyValuePair<string, string> item in emojiDictionary)
    {
      input = input.Replace(item.Key, " emoji " + item.Value + " ");
    }
    return input;
  }
}