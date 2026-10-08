using LanguageExt;
using LanguageExt.Traits;
using QueryKata = SqlKata.Query;
using SqlKata;
using SqlKata.Execution;
using System.Data;

namespace Database.Services;

public interface IData {

   // [Obsolete(message: "Use variant K<IO, A> instead.")]
   // Task<A?> Exec<A>(Func<QueryFactory, Lift<Task<A?>>> f, CancellationToken ct = default);

   // [Obsolete(message: "Use variant K<IO, A> instead.")]
   // Task<IEnumerable<A>> Exec<A>(Func<QueryFactory, Lift<Task<IEnumerable<A>>>> f, CancellationToken ct = default);

   /// <summary>
   /// No retry feature inside this implementation.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="f"></param>
   /// <param name="ct"></param>
   /// <returns></returns>
   Task<A> Exec<A>(Func<IDbTransaction, QueryFactory, Lift<Task<A>>> f, CancellationToken ct = default);

   K<IO, A> Exec<A>(Func<QueryFactory, K<IO, A>> f, CancellationToken ct = default);

   K<IO, A> Exec<A>(Func<IDbTransaction, QueryFactory, K<IO, A>> f, CancellationToken ct = default);

   SqlResult Compile(Func<QueryKata> f);
}
