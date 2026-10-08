using Database.MySql.Settings;
using Database.Services;
using LanguageExt;
using LanguageExt.Traits;
using Microsoft.Extensions.Options;
using MySqlConnector;
using QueryKata = SqlKata.Query;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;
using System.Data;

namespace Database.MySql;

using static CSharpExt.Prelude;
using static LanguageExt.Prelude;

/// <summary>
/// Must use MySqlConnector instead of MySql.Data.MySqlClient as it supports option guidformat = none where CHAR(36) treats as string.
/// </summary>
/// <param name="_config"></param>
public sealed class MySqlData(IOptions<MySqlConfig> _config): IData {
   readonly MySqlConfig config = _config.Value;

   // public async Task<A?> Exec<A>(Func<QueryFactory, Lift<Task<A?>>> f, CancellationToken ct = default) {
   //    await using var cn = new MySqlConnection(config.ConnectionString);
   //    await cn.OpenAsync(ct);
   //    using var qf = new QueryFactory(cn, new MySqlCompiler());
   //    var ret = await f(qf).Function();
   //    await cn.CloseAsync();
   //    return ret;
   // }

   // public async Task<IEnumerable<A>> Exec<A>(Func<QueryFactory, Lift<Task<IEnumerable<A>>>> f, CancellationToken ct = default) {
   //    await using var cn = new MySqlConnection(config.ConnectionString);
   //    await cn.OpenAsync(ct);
   //    using var qf = new QueryFactory(cn, new MySqlCompiler());
   //    var ret = await f(qf).Function();
   //    await cn.CloseAsync();
   //    return ret;
   // }

   public async Task<A> Exec<A>(Func<IDbTransaction, QueryFactory, Lift<Task<A>>> f, CancellationToken ct = default) {
      await using var cn = new MySqlConnection(config.ConnectionString);
      await cn.OpenAsync(ct);
      await using var tx = await cn.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
      using var qf = new QueryFactory(cn, new MySqlCompiler());
      var ret = await f(tx, qf).Function();
      await tx.CommitAsync(ct);
      await cn.CloseAsync();
      return ret;
   }

   /// <summary>
   /// Need to verify if this still valid if A is unit.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="f"></param>
   /// <param name="ct"></param>
   /// <returns></returns>
   public K<IO, A> Exec<A>(Func<QueryFactory, K<IO, A>> f, CancellationToken ct = default) =>
      bracketIO(
         from cn in use(() => new MySqlConnection(config.ConnectionString))
         from _open in liftIO(() => cn.OpenAsync(ct))
         from qf in use(() => new QueryFactory(cn, new MySqlCompiler())) // Disposing QueryFactory will eventually dispose MySqlConnection.
         from a in f(qf)
         from _close in liftIO(cn.CloseAsync)
         select a
      );

   public K<IO, A> Exec<A>(Func<IDbTransaction, QueryFactory, K<IO, A>> f, CancellationToken ct = default) =>
      retryWhile(
         Schedule.recurs(config.Recurrence.Times) | Schedule.fibonacci(new Duration(config.Recurrence.Delay)),
         bracketIO(
            from cn in use(() => new MySqlConnection(config.ConnectionString))
            from _open in liftIO(() => cn.OpenAsync(ct))
            from qf in use(() => new QueryFactory(cn, new MySqlCompiler())) // Disposing QueryFactory will eventually dispose MySqlConnection.
            from a in bracketIO(
               from tx in use(IO.liftAsync(() => cn.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct).ToRef()))
               from a in f(tx, qf)
               from _commit in liftIO(() => tx.CommitAsync(ct))
               select a
            )
            from _close in liftIO(cn.CloseAsync)
            select a
         ),
         e => e.Exception.Bind(ex => cast(() => ex as MySqlException)).Match(
            None: () => false,
            Some: ex => config.Recurrence.ErrorCodes.Any(x => x == ex.Number)
         )
      );

   // public K<Eff, A> Exec<A>(Func<QueryFactory, K<Eff, A>> f, CancellationToken ct = default) =>
   //    bracketIO(
   //       from cn in use(() => new MySqlConnection(config.ConnectionString))
   //       from _open in liftIO(() => cn.OpenAsync(ct))
   //       from qf in use(() => new QueryFactory(cn, new MySqlCompiler())) // Disposing QueryFactory will eventually dispose MySqlConnection.
   //       from a in f(qf).RunIO()
   //       from _close in liftIO(cn.CloseAsync)
   //       select a
   //    ).ToEff();

   // public K<Eff, A> Exec<A>(Func<IDbTransaction, QueryFactory, K<Eff, A>> f, CancellationToken ct = default) =>
   //    retryWhile(
   //       Schedule.recurs(config.Recurrence.Times) | Schedule.fibonacci(new Duration(config.Recurrence.Delay)),
   //       bracketIO(
   //          from cn in use(() => new MySqlConnection(config.ConnectionString))
   //          from _open in liftIO(() => cn.OpenAsync(ct))
   //          from qf in use(() => new QueryFactory(cn, new MySqlCompiler()))
   //          from a in bracketIO(
   //             from tx in use(IO.liftAsync(() => cn.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct).ToRef()))
   //             from a in f(tx, qf).RunIO()
   //             from _commit in liftIO(() => tx.CommitAsync(ct))
   //             select a
   //          )
   //          from _close in liftIO(cn.CloseAsync)
   //          select a
   //       ).ToEff(),
   //       e => e.Exception.Bind(ex => cast(() => (MySqlException)ex)).Match(
   //          None: () => false,
   //          Some: ex => config.Recurrence.ErrorCodes.Any(x => x == ex.Number)
   //       )
   //    );
   public SqlResult Compile(Func<QueryKata> f) {
      var cpl = new MySqlCompiler();
      var query = f();
      return cpl.Compile(query);
   }
}