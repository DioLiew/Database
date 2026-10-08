using Dapper;
using Database.MySql;
using Database.MySql.Handlers;
using Database.MySql.Settings;
using QueryKata = SqlKata.Query;
using SqlKata.Execution;
using System.Text.Json;
using Xunit.Abstractions;

namespace Database.Services.Test;

using static CSharpExt.Prelude;

public class MySqlDataTest {
   readonly ITestOutputHelper output;
   readonly string jsonDirectory = @"E:\Box\NET 10\Database\Database.Services.Test";
   readonly IData mysql;

   public MySqlDataTest(ITestOutputHelper output) {
      this.output = output;
      mysql = (
         from _ in lift(
            () => SqlMapper.AddTypeHandler(new GuidTypeHandler()),
            () => SqlMapper.AddTypeHandler(new GuidTypeHandler()),
            () => SqlMapper.RemoveTypeMap(typeof(Guid)),
            () => SqlMapper.RemoveTypeMap(typeof(Guid?)),
            () => SqlMapper.AddTypeHandler(new DateTimeOffsetHandler()),
            () => SqlMapper.RemoveTypeMap(typeof(DateTimeOffset)),
            () => SqlMapper.RemoveTypeMap(typeof(DateTimeOffset?)),
            () => SqlMapper.AddTypeHandler(new DateOnlyTypeHandler()),
            () => SqlMapper.RemoveTypeMap(typeof(DateOnly)),
            () => SqlMapper.RemoveTypeMap(typeof(DateOnly?))
         )
         from sp in lift(
            WebApplication.CreateBuilder,
            b => b.Configuration.AddConfiguration(
               new ConfigurationBuilder()
               .SetBasePath(jsonDirectory)
               .AddJsonFile("appsettings.json")
               .Build()
            ),
            b => b.Services.Configure<MySqlConfig>(x => b.Configuration.GetSection(nameof(MySqlConfig)).Bind(x)),
            b => b.Services.AddTransient<IData, MySqlData>()
         )
         .Map(builder => builder.Build())
         .Map(app => app.Services.CreateScope().ServiceProvider)
         select sp.GetRequiredService<IData>()
      ).Function();
   }

   // ---------------------------------------------------------------------------------------------------
   // These are all the model class used to perform these tests.
   sealed record class Customer(string Id, string CustomerName, bool IsActive, DateTimeOffset CreatedDate, string CreatedBy, DateTimeOffset? ModifiedDate = default, string? ModifiedBy = default) {
      /// <summary>
      /// This is required by Dapper.
      /// </summary>
      Customer()
         : this(Id: string.Empty, CustomerName: string.Empty, IsActive: false, CreatedDate: default, CreatedBy: string.Empty, ModifiedDate: default, ModifiedBy: default) { }
   }

   sealed record class WorkOrder(string Id, string WorkOrderNum, int Quantity, string ItemMaster, string CustomerId, bool IsActive, DateTimeOffset StartDate, DateTimeOffset CreatedDate, string CreatedBy) {
      /// <summary>
      /// This is required by Dapper.
      /// </summary>
      WorkOrder()
         : this(Id: string.Empty, WorkOrderNum: string.Empty, Quantity: default, ItemMaster: string.Empty, CustomerId: string.Empty, IsActive: default, StartDate: default, CreatedDate: default, CreatedBy: string.Empty) { }
   }

   sealed record class Document(string Id, string CustomerId, string ModelId, string Title, string FileName, string Checksum, bool IsActive, DateTimeOffset CreatedDate, string CreatedBy, DateTimeOffset? ModifiedDate = default, string? ModifiedBy = default) {
      /// <summary>
      /// This is required by Dapper.
      /// </summary>
      Document()
         : this(Id: string.Empty, CustomerId: string.Empty, ModelId: string.Empty, Title: string.Empty, FileName: string.Empty, Checksum: string.Empty, IsActive: false, CreatedDate: default, CreatedBy: string.Empty, ModifiedDate: default, ModifiedBy: default) { }
   }

   // ---------------------------------------------------------------------------------------------------

   [Fact]
   public async Task RetrieveList_InnerToIO_Async() =>
      await (
         from xs in mysql.Exec(
            qf => IO.liftAsync(
               () => qf.FromQuery(new QueryKata("customer")).GetAsync<Customer>()
            )
         )
         select invoke(() => output.WriteLine(JsonSerializer.Serialize(xs)))
      ).Catch(e => failwith<Unit>(e.Message)).RunAsync().ToRef();


   [Fact]
   public async Task RetrieveList_InnerToIO_AsyncThrowCallback() =>
      await (
         from xs in mysql.Exec(
            qf => IO.liftAsync(
               () => qf.FromQuery(new QueryKata("customer")).GetAsync<Customer>()
            )
         )
         select raise<Unit>(Error.New("Testing"))
      ).CatchIO(e => liftIO(() => Task.Run(() => Assert.Equal("Testing", e.Message)))).RunAsync().ToRef();

   [Fact]
   public async Task RetrieveList_InnerToIO_AsyncThrowAlternative() {
      var x = await (
         from xs in mysql.Exec(
            qf => IO.liftAsync(
               () => qf.FromQuery(new QueryKata("customer")).GetAsync<Customer>()
            )
         )
         select raise<int>(Error.New("Testing"))
         ).Catch(1).RunAsync().ToRef();
      Assert.Equal(1, x);
   }

   [Fact]
   public async Task InsertTask() =>
      await (
         mysql.Exec((tx, qf) =>
            from customer in lift(() => new { Id = Guid.NewGuid(), CustomerName = "Test Customer", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "14507325" })
            from a in IO.liftAsync(() => qf.FromQuery(new QueryKata("Customer")).InsertAsync(customer, tx, 15))
            from b in IO.liftAsync(
               () => qf.FromQuery(new QueryKata("Model")).InsertAsync(
                  new { Id = Guid.NewGuid(), CustomerId = customer.Id, ModelDescription = "Test Model Desc", ModelNumber = "Test Model", IsActive = true, CreatedDate = DateTime.Now, CreatedBy = "14507325" }, tx, 15
               )
            )
            select a + b
         )
      ).Catch(e => raise<int>(e)).RunAsync().ToRef();

   static QueryKata notExists(WorkOrder wo) =>
      new QueryKata().WhereNotExists(
         q => q
            .From("WorkOrder")
            .Where("Id", wo.Id)
      )
      .FromRaw(
         "(VALUES (?, ?, ?, ?, ?, ?, ?)) AS t (Id, WorkOrderNum, Quantity, ItemMaster, IsActive, CreatedDate, CreatedBy)",
         wo.Id, wo.WorkOrderNum, wo.Quantity, wo.ItemMaster, wo.IsActive, wo.CreatedDate, wo.CreatedBy
      )
      .Select("t.Id", "t.WorkOrderNum", "t.Quantity", "t.ItemMaster", "t.IsActive", "t.CreatedDate", "t.CreatedBy");

   static readonly string[] columns = ["Id", "WorkOrderNum", "Quantity", "ItemMaster", "IsActive", "CreatedDate", "CreatedBy"];

   [Fact]
   public void CompileSqlInsertIgnore() =>
      output.WriteLine(
         mysql.Compile(
            () => new QueryKata("WorkOrder")
            .AsInsert(
               columns,
               notExists(
                  new WorkOrder(
                     Id: Guid.NewGuid().ToString(),
                     WorkOrderNum: "WOABC",
                     Quantity: 10,
                     ItemMaster: "ModelA B:C",
                     CustomerId: Guid.NewGuid().ToString(),
                     StartDate: DateTimeOffset.Now.AddDays(-7),
                     IsActive: true,
                     CreatedDate: DateTimeOffset.Now,
                     CreatedBy: "14507325"
                  )
               )
            )
         ).Sql
      );

   [Fact]
   public async Task RetrieveListUsingIdAsString() =>
      await (
         from xs in mysql.Exec(
            qf => IO.liftAsync(
               () => qf.FromQuery(
                  new QueryKata("document")
                  .Where(
                     [
                        KeyValuePair.Create<string, object>("Id", "24adb96b-db5b-4df1-8a24-f3282293f6f0"),
                        KeyValuePair.Create<string, object>("ModelId", "d3c73009-a4bc-404b-973a-4fc42ee9dee3")
                     ]
                  )
               ).GetAsync<Document>()
            )
         )
         select invoke(() => output.WriteLine(JsonSerializer.Serialize(xs)))
      ).Catch(e => failwith<Unit>(e.Message)).RunAsync().ToRef();

   [Fact]
   public async Task RetrieveCustomer() =>
      await (
         from xs in mysql.Exec(
            (tx, qf) => IO.liftAsync(
               () => qf.FromQuery(
                  new QueryKata("customer").Where("CustomerName", "MICROS")
               ).FirstOrDefaultAsync<Customer>(tx)
            )
         )
         select invoke(() => output.WriteLine(JsonSerializer.Serialize(xs)))
      ).Catch(e => failwith<Unit>(e.Message)).RunAsync().ToRef();
}