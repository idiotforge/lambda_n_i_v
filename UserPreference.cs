namespace Lambdaniv;
public class UserPreference
{
  // really dumb but since discord uses ulong for user ids and litedb doesnt have support for it
  // since i cant use reflection based stuff with aot i have to make the generated files use long
  // for that this class has to use long
  
  // you have to access the db by unchecked casting the ulong to long :D
  public long Id { get; set; }
  public string VoiceName { get; set; } = "en_us_002";
  public float Pitch { get; set; } = 1;
  public float Tempo { get; set; } = 1;
}
