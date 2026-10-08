using CSharpExt;
using Database.LiteDb.Settings;
using Database.LiteDb.Test.Entities;
using LanguageExt;
using LanguageExt.Common;
using LiteDB;
using Microsoft.Extensions.Options;
using System.Globalization;
using Xunit.Abstractions;

namespace Database.LiteDb.Test;

using static CSharpExt.Prelude;
using static LanguageExt.Prelude;

public class LiteDbTest {
   readonly string jsonDirectory = @"E:\Box\Net 10\Database\Database.LiteDb.Test";
   readonly ITestOutputHelper output;
   readonly ILiteData lite;

   // readonly string accessId = "af8ff372-fd0d-4641-aa1d-26c3e77397f5";
   // readonly string refreshId = "2ffe9ca8-a09e-4786-822b-4c7ec2ea7d0f";

   readonly Refresh refresh = new() {
      Id = "2ffe9ca8-a09e-4786-822b-4c7ec2ea7d0f",
      Scheme = "Scheme.Name",
      UserId = "14507325",
      Value = "eyabcRefresh",
      LoginId = Guid.NewGuid().ToString(),
      RoleId = Guid.NewGuid().ToString(),
      RoleName = "Administrator",
      PasswordChangedDate = DateTimeOffset.UtcNow.AddDays(-1),
      IssuedDate = DateTimeOffset.UtcNow,
      ExpiryDate = DateTimeOffset.UtcNow.AddDays(7),
      IsActive = true,
      IsFault = false,
      Accesses = []
   };

   readonly Access access = new() {
      Id = "af8ff372-fd0d-4641-aa1d-26c3e77397f5",
      Scheme = "SchemeA",
      UserId = "14507325",
      Value = "evyABC",
      ExpiryDate = DateTimeOffset.UtcNow.AddDays(7),
      IssuedDate = DateTimeOffset.UtcNow,
      Refresh = null,
      IsFault = false,
      IsActive = true
   };

   public LiteDbTest(ITestOutputHelper output) {
      this.output = output;
      lite = (
         from builder in lift(
            WebApplication.CreateBuilder,
            b => b.Configuration.AddConfiguration(
               new ConfigurationBuilder()
               .SetBasePath(jsonDirectory)
               .AddJsonFile("appsettings.json")
               .Build()
            ),
            b => b.Services.Configure<LiteConfig>(x => b.Configuration.GetSection(nameof(LiteConfig)).Bind(x)),
            b => b.Services.AddSingleton<ILiteDatabase, LiteDatabase>(
               sp => (
                  from config in lift(() => sp.GetRequiredService<IOptions<LiteConfig>>().Value)
                  from mapper in lift(
                     () => new BsonMapper { TrimWhitespace = true },
                     x => x.RegisterType<DateTimeOffset>(
                        serialize: dt => dt.ToString("o", CultureInfo.InvariantCulture),
                        deserialize: bsonValue => DateTime.ParseExact(
                           bsonValue.AsString,
                           "o",
                           CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind
                        )
                     ),
                     x => x.Entity<Access>().Id(x => x.Id).DbRef(x => x.Refresh),
                     x => x.Entity<Refresh>().Id(x => x.Id).DbRef(x => x.Accesses)
                  )
                  from liteDb in lift(
                     () => new LiteDatabase($"filename={config.FileName}; connection={config.Connection}", mapper) { Timeout = config.Timeout },
                     x => x.GetCollection<Access>().EnsureIndex(x => x.Value, true),
                     x => x.GetCollection<Refresh>().EnsureIndex(x => x.Value, true)
                  )
                  select liteDb
               ).Function()
            ),
            b => b.Services.AddSingleton<ILiteData, LiteData>()
         )
         from app in lift(() => builder.Build())
         select app.Services.CreateScope().ServiceProvider.GetRequiredService<ILiteData>()
      ).Function();
   }

   [Fact]
   public Task InsertTestAsyncLift() =>
         lite.Atomic(
            // new ParallelOptions { MaxDegreeOfParallelism = 3 }, // Do not use this since if exception being throw, system will need to wait for liteDb to timeout as a whole.
            c => () => c.Insert(
               access with {
                  Refresh = refresh with { Id = Guid.NewGuid().ToString() },
                  Id = Guid.NewGuid().ToString(),
                  Value = "another"
               }
            ),
            c => () => c.Insert(access with { Refresh = refresh }),
            c => () => c.Insert(refresh with { Accesses = [access] })
         ).Catch(e => invoke(() => output.WriteLine(e.Message))).RunAsync().ToRef();

   [Fact]
   public void InsertTestLift() =>
      _ = lite.Atomic(
         c => () => c.Insert(
            access with {
               Refresh = refresh with { Id = Guid.NewGuid().ToString() },
               Id = Guid.NewGuid().ToString(),
               Value = "another"
            }
         ),
         c => () => c.Insert(access with { Refresh = refresh }),
         c => () => c.Insert(refresh with { Accesses = [access] })
      ).Catch(e => invoke(() => output.WriteLine(e.Message))).Run();


   [Fact]
   public Task InsertTestAsync() =>
      lite.Atomic(
         c => () => c.Insert(
            access with {
               Refresh = refresh with { Id = Guid.NewGuid().ToString() },
               Id = Guid.NewGuid().ToString(),
               Value = "another"
            }
         ),
         c => () => c.Insert(access with { Refresh = refresh }),
         c => () => c.Insert(refresh with { Accesses = [access] })
      ).Catch(e => invoke(() => output.WriteLine(e.Message))).RunAsync().ToRef();

   [Fact]
   public void InsertTest() =>
      _ = lite.Atomic(
         c => () => c.Insert(
            access with {
               Refresh = refresh with { Id = Guid.NewGuid().ToString() },
               Id = Guid.NewGuid().ToString(),
               Value = "another"
            }
         ),
         c => () => c.Insert(access with { Refresh = refresh }),
         c => () => c.Insert(refresh with { Accesses = [access] })
      ).Catch(e => invoke(() => output.WriteLine(e.Message))).Run();


   [Fact]
   public Task InsertTestInnerAsync() =>
      lite.Atomic(
         c => () => run(
            IO.lift(
               () => invoke(
                  () => c.Insert(access with { Refresh = refresh }),
                  () => c.Insert(refresh with { Accesses = [access] })
               )
            )
         )
      ).Catch(e => invoke(() => output.WriteLine(e.Message))).RunAsync().ToRef();
}
