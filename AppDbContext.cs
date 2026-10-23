using Community.LiteDB.Aot.Collections;
using Community.LiteDB.Aot.Context;
using Community.LiteDB.Aot.ModelBuilder;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lambdaniv;
internal partial class AppDbContext : LiteDbContext
{
  public AotLiteCollection<UserPreference> UserPreferences => Collection<UserPreference>();
  public AppDbContext(string filename) : base(filename)
  {
    BsonMapper.Global.RegisterType<ulong>(
      uL => new BsonValue(unchecked((long)uL)),
      bson => unchecked((ulong)bson.AsInt64)
      );
  }
  protected override void OnModelCreating(EntityModelBuilder modelBuilder)
  {
    modelBuilder.Entity<UserPreference>(entity =>
    {
      entity.HasKey(x => x.Id);
      entity.Property(x => x.VoiceName);
      entity.Property(x => x.Pitch);
      entity.Property(x => x.Tempo);
    });
  }
}

