using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Lambdaniv.Aniv;

public static class AnivModel
{
  private static readonly Dictionary<string, List<string>> dict = new();
  private static readonly Random rand = new();
  private static int keySize;
  static AnivModel()
  {
    LoadModel("./Aniv/textmodel.txt", 3);
  }
  static string Join(string a, string b)
  {
    return a + " " + b;
  }
  static void LoadModel(string filePath, int ks)
  {
    keySize = ks;
    string body;
    using (StreamReader sr = new(filePath))
    {
      body = sr.ReadToEnd();
    }
    var words = body.Split();
    for (int i = 0; i < words.Length - keySize; i++)
    {
      var key = words.Skip(i).Take(keySize).Aggregate(Join);
      string value;
      if (i + keySize < words.Length)
      {
        value = words[i + keySize];
      }
      else
      {
        value = "";
      }

      if (dict.ContainsKey(key))
      {
        dict[key].Add(value);
      }
      else
      {
        dict.Add(key, new List<string>() { value });
      }
    }
  }
  private static string Markov(int outputSize)
  {
    List<string> output = new List<string>();
    int n = 0;
    int rn = rand.Next(dict.Count);
    string prefix = dict.Keys.Skip(rn).Take(1).Single();
    output.AddRange(prefix.Split());

    while (true)
    {
      var suffix = dict[prefix];
      if (suffix.Count == 1)
      {
        if (suffix[0] == "")
        {
          return output.Aggregate(Join);
        }
        output.Add(suffix[0]);
      }
      else
      {
        rn = rand.Next(suffix.Count);
        output.Add(suffix[rn]);
      }
      if (output.Count >= outputSize)
      {
        return output.Take(outputSize).Aggregate(Join);
      }
      n++;
      prefix = output.Skip(n).Take(keySize).Aggregate(Join);
    }
  }
  public static string MakeString()
  {
    string sentence = Markov(20 + rand.Next() % 20).Replace("  ", " ");
    if (sentence[0] == ' ')
    {
      sentence = sentence[1..];
    }
    return sentence;
  }
}
