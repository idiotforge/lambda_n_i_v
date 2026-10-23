using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lambdaniv;
internal static class LiteDbProvider
{
  private static AppDbContext db = new("preferences.db");
  // is this actually how people use nosql???
  public static void SetPreferenceVoice(ulong userId, string voiceName)
  {
    UserPreference? current = db.UserPreferences.FindById(new BsonValue(unchecked((long)userId)));
    if (current != null)
    {
      current.VoiceName = voiceName;
      db.UserPreferences.Update(current);
      return;
    }
    UserPreference up = new()
    {
      Id = unchecked((long)userId),
      VoiceName = voiceName,
    };
    db.UserPreferences.Insert(up);
  }
  public static void SetPreferencePitch(ulong userId, float pitch)
  {
    UserPreference? current = db.UserPreferences.FindById(new BsonValue(unchecked((long)userId)));
    if (current != null)
    {
      current.Pitch = pitch;
      db.UserPreferences.Update(current);
      return;
    }
    UserPreference up = new()
    {
      Id = unchecked((long)userId),
      Pitch = pitch,
    };
    db.UserPreferences.Insert(up);
  }
  public static void SetPreferenceTempo(ulong userId, float tempo)
  {
    UserPreference? current = db.UserPreferences.FindById(new BsonValue(unchecked((long)userId)));
    if (current != null)
    {
      current.Tempo = tempo;
      db.UserPreferences.Update(current);
      return;
    }
    UserPreference up = new()
    {
      Id = unchecked((long)userId),
      Tempo = tempo,
    };
    db.UserPreferences.Insert(up);
  }
  public static void SetPreferenceSpeed(ulong userId, float speed)
  {
    UserPreference? current = db.UserPreferences.FindById(new BsonValue(unchecked((long)userId)));
    if (current != null)
    {
      current.Tempo = speed;
      current.Pitch = speed;
      db.UserPreferences.Update(current);
      return;
    }
    UserPreference up = new()
    {
      Id = unchecked((long)userId),
      Tempo = speed,
      Pitch = speed
    };
    db.UserPreferences.Insert(up);
  }
  public static UserPreference? GetPreference(ulong userId)
  {
    return db.UserPreferences.FindById(new BsonValue(unchecked((long)userId)));
  }
}