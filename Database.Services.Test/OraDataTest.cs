using Database.Oracle;
using Database.Oracle.Settings;
using QueryKata = SqlKata.Query;
using SqlKata.Execution;
using System.Text.Json;
using Xunit.Abstractions;

namespace Database.Services.Test;

using static LanguageExt.List;
public class OraDataTest {
   readonly ITestOutputHelper output;
   readonly string jsonDirectory = @"E:\Box\Visual Studio\Net 9\LanguageExt v5\Database\Database.Services.Test";
   readonly IData ora;

   public OraDataTest(ITestOutputHelper output) {
      this.output = output;
      Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
      ora = (
         from sp in lift(
            WebApplication.CreateBuilder,
            b => b.Configuration.AddConfiguration(
               new ConfigurationBuilder()
               .SetBasePath(jsonDirectory)
               .AddJsonFile("appsettings.json")
               .Build()
            ),
            b => b.Services.Configure<OraConfig>(x => b.Configuration.GetSection(nameof(OraConfig)).Bind(x)),
            b => b.Services.AddTransient<IData, OraData>()
         )
         .Map(builder => builder.Build())
         .Map(app => app.Services.CreateScope().ServiceProvider)
         select sp.GetRequiredService<IData>()
      ).Function();
   }

   // ---------------------------------------------------------------------------------------------------
   // These are all the model class used to perform these tests.
   sealed record class Customer(
      string Id, string CustomerName, string? CustomerDesc, DateTime CreatedDate, string CreatedBy, string FacilityId, string? Prefix, short GrNumber, short ReturnPath, short MfgPn, short SerialAliasFlag, short ValidateReserialize, short ReelLocation, short SplitReelCheckWo, string? OtsPalletFormat, short? OtsRunningNumber, short RobotPacking, short RemoteAddrFilter
   ) {

      /// <summary>
      /// This is required by Dapper.
      /// </summary>
      Customer()
         : this(
            Id: string.Empty, CustomerName: string.Empty, CustomerDesc: default, CreatedDate: default, CreatedBy: string.Empty, FacilityId: string.Empty, Prefix: default, GrNumber: default,
            ReturnPath: default, MfgPn: default, SerialAliasFlag: default, ValidateReserialize: default, ReelLocation: default, SplitReelCheckWo: default, OtsPalletFormat: default, OtsRunningNumber: default, RobotPacking: default, RemoteAddrFilter: default
         ) { }
   }

   sealed record class ItemMaster(
      string Id, string ModelNum, string Revision, string EnggRevision, string? ItemMasterDesc, string? RouteId, string? RmaRouteId, string BoardType, string RunType, short? Xrows, short? Ycols, int PanelSerialLength, int ImageSerialLength, DateTime CreatedDate, string CreatedBy, string CustomerId, string? FamilyId, short BomUploaded, short IsActive, string? ModelCode, string? PrimaryItemMaster, short AutoDepanelize, short AutoPostpone, string? OfflineRouteId, short ReworkReplacementLimit, short XmlGeneration, string? CustomerPartNum, short RmaFlag, DateTime? Modified, short AllowCrossout, short DepanelizePanel, short? GenerateXmlResult, string? IpAddress, short? PrintTraceability, string? ModifiedBy
   ) {
      /// <summary>
      /// This is required by Dapper.
      /// </summary>
      ItemMaster()
         : this(
            Id: string.Empty, ModelNum: string.Empty, Revision: string.Empty, EnggRevision: string.Empty, ItemMasterDesc: default, RouteId: default, RmaRouteId: default, BoardType: string.Empty,
            RunType: string.Empty, Xrows: default, Ycols: default, PanelSerialLength: default, ImageSerialLength: default, CreatedDate: default, CreatedBy: string.Empty, CustomerId: string.Empty, FamilyId: default, BomUploaded: default, IsActive: default, ModelCode: default, PrimaryItemMaster: default, AutoDepanelize: default, AutoPostpone: default, OfflineRouteId: default, ReworkReplacementLimit: default, XmlGeneration: default, CustomerPartNum: default, RmaFlag: default, Modified: default, AllowCrossout: default, DepanelizePanel: default, GenerateXmlResult: default, IpAddress: default, PrintTraceability: default, ModifiedBy: default
         ) { }
   }
   // ---------------------------------------------------------------------------------------------------
   [Fact]
   public async Task RetrieveListIO() =>
      await (
         from xs in ora.Exec(
            qf => IO.liftAsync(
               () => qf.FromQuery(
                  new QueryKata("T_CUSTOMER").Where([new KeyValuePair<string, object>("CUSTOMER_NAME", "M01 - MICROS")])
               ).GetAsync<Customer>()
            )
         )
         select invoke(() => output.WriteLine(JsonSerializer.Serialize(xs)))
      ).Catch(e => failwith<Unit>(e.Message)).RunAsync().ToRef();

   [Fact]
   public async Task RetrieveAllListIO() =>
      await (
         from xs in ora.Exec(
            qf => IO.liftAsync(
               () => qf.FromQuery(
                  new QueryKata("T_CUSTOMER")
               ).GetAsync<Customer>()
            )
         )
         select invoke(() => output.WriteLine(JsonSerializer.Serialize(xs)))
      ).Catch(e => failwith<Unit>(e.Message)).RunAsync().ToRef();

   [Fact]
   public async Task RetrieveIO() =>
      await (
         from xs in ora.Exec(
            qf => IO.liftAsync(
               () => qf.FromQuery(
                  new QueryKata("T_CUSTOMER").Where([new KeyValuePair<string, object>("CUSTOMER_NAME", "M01 - MICROS")])
               ).FirstOrDefaultAsync<Customer>()
            )
         )
         select invoke(() => output.WriteLine(JsonSerializer.Serialize(xs)))
      ).Catch(e => failwith<Unit>(e.Message)).RunAsync().ToRef();

   /// <summary>
   /// This will cause SqlKata generate query where ID =@id0 AND ID =@id1.
   /// </summary>
   /// <returns></returns>
   [Fact]
   public async Task RetrieveIMs() =>
      await (
         from xs in ora.Exec(
            qf => IO.liftAsync(
               () => qf.FromQuery(
                  new QueryKata("T_ITEM_MASTER").Where(
                     [
                        KeyValuePair.Create<string, object>("ID", "4f337d0d-e2a8-4b57-a672-392e5900073a"),
                        KeyValuePair.Create<string, object>("ID", "4fcc3ce5-13d0-4ef2-8325-3c845900073a")
                     ]
                  )
               ).GetAsync<ItemMaster>()
            )
         )
         select invoke(() => output.WriteLine(JsonSerializer.Serialize(xs)))
      ).Catch(e => failwith<Unit>(e.Message)).RunAsync().ToRef();

   [Fact]
   public void CompileSqlWhereIn() =>
      output.WriteLine(
         ora.Compile(
            () => new QueryKata("T_ITEM_MASTER")
            .WhereIn("ID", ["4f337d0d-e2a8-4b57-a672-392e5900073a", "4fcc3ce5-13d0-4ef2-8325-3c845900073a"])
         ).Sql
      );

   readonly IList<string> ids = ["4f337d0d-e2a8-4b57-a672-392e5900073a", "4fcc3ce5-13d0-4ef2-8325-3c845900073a"];
   readonly IList<string> revs = ["4f337d0d-e2a8-4b57-a672-392e5900073a", "4fcc3ce5-13d0-4ef2-8325-3c845900073a"];

   // [Fact]
   // public void CompileSqlWhereOr() =>
   //    ora.Compile(qf =>
   //       qf.Table("T_ITEM_MASTER", q => { ids.Iter(id => q.OrWhere("Id", id)); },
   //                                 q => { revs.Iter(rev => q.OrWhere("Rev", rev)); }))
   //         .Map(query => invoke(() => output.WriteLine(query.Sql))).Function();

   [Fact]
   public void CompileSqlWhereOr() =>
      output.WriteLine(
         ora.Compile(
            () => new QueryKata("T_ITEM_MASTER")
            .Where(
               x => {
                  iter(ids, id => x.OrWhere("Id", id));
                  return x;
               }
            )
            .Where(
               x => {
                  iter(revs, rev => x.OrWhere("Rev", rev));
                  return x;
               }
            )
         ).Sql
      );
}
