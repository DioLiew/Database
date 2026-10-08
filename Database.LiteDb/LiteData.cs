using LanguageExt;
using LanguageExt.Traits;
using LiteDB;
using System.Linq.Expressions;

namespace Database.LiteDb;

using static LanguageExt.Prelude;

/// <summary>
/// This should be DI as singleton since got SemaphoreSlim.
/// </summary>
/// <param name="liteDb"></param>
public class LiteData(ILiteDatabase liteDb): ILiteData {
   const string atomicFail = "Fail begin transaction for LiteDb. Retry";
   public SemaphoreSlim Lock { get; } = new(1, 1); // This is to prevent multithread accessing lite database.

   /// <summary>
   /// Delete a document on this collection based on the _id index. Return true if document is deleted.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="id"></param>
   /// <returns></returns>
   public bool Delete<A>(BsonValue id) =>
      liteDb.GetCollection<A>().Delete(id);

   /// <summary>
   /// Operation wrapped in transaction with default no async.<br/>
   /// Using IO under the operation MUST run else will cause LiteDb to commit even exception is being throwed.
   /// </summary>
   /// <param name="operations"></param>
   /// <returns></returns>
   public IO<Unit> Atomic(params Func<LiteData, Action>[] operations) =>
      // Unit Test shows only one transaction is allowed. Any other transaction shall wait till any existing one being closed before it opens.
      from _begin in retryWhile(
         Schedule.exponential(10) | Schedule.spaced(200), // Duration = Milliseconds.
         unless(IO.lift(liteDb.BeginTrans), IO.fail<Unit>(atomicFail)),
         // liteDb.BeginTrans() ? unitIO : IO.fail<Unit>(atomicFail),
         // from begin in IO.lift(liteDb.BeginTrans)
         // select begin == true
         // ? unit
         // : failwith<Unit>(atomicFail),
         e => e.Message == atomicFail
      )
      from _operations in IO.lift(
         () => operations.AsIterable().Iter(x => x(this).Invoke())
      ) | @catch(
         e => {
            liteDb.Rollback(); // This always false. Not sure why.
            return IO.fail<Unit>(e);
         }
      )
         //from _commit in IO.lift(liteDb.Commit)
      from _commit in unless(IO.lift(liteDb.Commit), IO.fail<Unit>("Fail commit transaction for LiteDb."))
      select unit;

   // /// <summary>
   // /// Operation wrapped in transaction with default no async.<br/>
   // /// Using IO under the operation MUST run else will cause LiteDb to commit even exception is being throwed.
   // /// </summary>
   // /// <typeparam name="A"></typeparam>
   // /// <param name="operation"></param>
   // /// <returns></returns>
   // public IO<A> Atomic<A>(Func<LiteData, A> operation) where A : notnull =>
   //    // Unit Test shows only one transaction is allowed. Any other transaction shall wait till any existing one being closed before it opens.
   //    from _begin in retryWhile(
   //       Schedule.exponential(10) | Schedule.spaced(200), // Duration = Milliseconds.
   //       unless(IO.lift(liteDb.BeginTrans), IO.fail<Unit>(atomicFail)),
   //       //liteDb.BeginTrans() ? unitIO : IO.fail<Unit>(atomicFail),
   //       // from begin in IO.lift(liteDb.BeginTrans)
   //       // select begin == true
   //       // ? unit
   //       // : failwith<Unit>(atomicFail),
   //       e => e.Message == atomicFail
   //    )
   //    from a in IO.lift(() => operation(this)) | @catch(
   //       e => {
   //          liteDb.Rollback(); // This always false. Not sure why.
   //          return IO.fail<A>(e);
   //       }
   //    )
   //       //from _commit in IO.lift(liteDb.Commit)
   //    from _commit in unless(IO.lift(liteDb.Commit), IO.fail<Unit>("Fail commit transaction for LiteDb."))
   //    select a;

   // /// <summary>
   // /// Operation wrapped in transaction. Since there is no async in LiteDb, use Lift instead. <br/>
   // /// Using IO under the operation MUST run else will cause LiteDb to commit even exception is being throwed.
   // /// </summary>
   // /// <typeparam name="A"></typeparam>
   // /// <param name="operation"></param>
   // /// <returns></returns>
   // public IO<A> Atomic<A>(Func<LiteData, Lift<A>> operation) where A : notnull =>
   //    // Unit Test shows only one transaction is allowed. Any other transaction shall wait till any existing one being closed before it opens.
   //    from _begin in retryWhile(
   //       Schedule.exponential(10) | Schedule.spaced(200), // Duration = Milliseconds.
   //       unless(IO.lift(liteDb.BeginTrans), IO.fail<Unit>(atomicFail)),
   //       e => e.Message == atomicFail
   //    )
   //    from a in operation(this).ToIO() | @catch(
   //       e => {
   //          liteDb.Rollback(); // This always false. Not sure why.
   //          return IO.fail<A>(e);
   //       }
   //    )
   //       // from _commit in IO.lift(liteDb.Commit)
   //    from _commit in unless(IO.lift(liteDb.Commit), IO.fail<Unit>("Fail commit transaction for LiteDb."))
   //    select a;

   /// <summary>
   /// Find a document using the bsonExpresion predicate, option for bsonExpresion include.
   /// Remember to concrete(.ToList()) if finding document and later for deletion.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="bsonExpPred"></param>
   /// <param name="bsonInclusion"></param>
   /// <returns></returns>
   public IEnumerable<A> Find<A>(string bsonExpPred, string? bsonInclusion = default) =>
      bsonInclusion is null ? liteDb.GetCollection<A>().Find(BsonExpression.Create(bsonExpPred))
      : liteDb.GetCollection<A>().Include(BsonExpression.Create(bsonInclusion)).Find(BsonExpression.Create(bsonExpPred));

   /// <summary>
   /// Find a document using the predicate, option for bsonExpresion include.
   /// Remember to concrete(.ToList()) if finding document and later for deletion.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="pred"></param>
   /// <param name="bsonInclusion"></param>
   /// <returns></returns>
   public IEnumerable<A> Find<A>(Expression<Func<A, bool>> pred, string? bsonInclusion = default) =>
      bsonInclusion is null ? liteDb.GetCollection<A>().Find(pred)
      : liteDb.GetCollection<A>().Include(BsonExpression.Create(bsonInclusion)).Find(pred);

   /// <summary>
   /// Find the first document using predicate bson expression. Return null if not found.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="bsonExpPred"></param>
   /// <param name="bsonInclusion"></param>
   /// <returns></returns>
   public Option<A> FindOne<A>(string bsonExpPred, string? bsonInclusion = default) =>
      bsonInclusion is null ? Optional(liteDb.GetCollection<A>().FindOne(BsonExpression.Create(bsonExpPred)))
      : Optional(liteDb.GetCollection<A>().Include(BsonExpression.Create(bsonInclusion)).FindOne(BsonExpression.Create(bsonExpPred)));

   /// <summary>
   /// Find the first document using predicate expression. Return null if not found.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="pred"></param>
   /// <param name="bsonInclusion"></param>
   /// <returns></returns>
   public Option<A> FindOne<A>(Expression<Func<A, bool>> pred, string? bsonInclusion = default) =>
      bsonInclusion is null ? Optional(liteDb.GetCollection<A>().FindOne(pred))
      : Optional(liteDb.GetCollection<A>().Include(BsonExpression.Create(bsonInclusion)).FindOne(pred));

   /// <summary>
   /// Insert a new entity to this collection. Document Id must be new value in the collection. Returns document Id.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="a"></param>
   /// <returns></returns>
   public BsonValue Insert<A>(A a) =>
      liteDb.GetCollection<A>().Insert(a);

   // public bool Rollback() =>
   //    liteDb.Rollback();

   /// <summary>
   /// Update a document in this collection. Returns false if document not found to update.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="a"></param>
   /// <returns></returns>
   public bool Update<A>(A a) =>
      liteDb.GetCollection<A>().Update(a);

   /// <summary>
   /// Update many documents.<br/>
   /// The extend does not support with initialization or any of prebuild function of initialization. Support only pure new() { ... }
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="extend"></param>
   /// <param name="predicate"></param>
   /// <returns></returns>
   public int UpdateMany<A>(Expression<Func<A, A>> extend, Expression<Func<A, bool>> predicate) =>
      liteDb.GetCollection<A>().UpdateMany(extend, predicate);
}

public interface ILiteData {
   // /// <summary>
   // /// Delete a document on this collection based on the _id index. Return true if document is deleted.
   // /// </summary>
   // /// <typeparam name="A"></typeparam>
   // /// <param name="id"></param>
   // /// <returns></returns>
   // IO<bool> Delete<A>(BsonValue id);

   SemaphoreSlim Lock { get; }

   /// <summary>
   /// Operation wrapped in transaction with default no async.<br/>
   /// Using IO under the operation will cause LiteDb to commit even exception is being throwed.
   /// </summary>
   /// <param name="operations"></param>
   /// <returns></returns>
   IO<Unit> Atomic(params Func<LiteData, Action>[] operations);

   // /// <summary>
   // /// Operation wrapped in transaction with default no async.<br/>
   // /// Using IO under the operation MUST run else it will cause LiteDb to commit even exception is being throwed.<br/>
   // /// CUD operation MUST include validation.
   // /// </summary>
   // /// <typeparam name="A"></typeparam>
   // /// <param name="operation"></param>
   // /// <returns></returns>
   // IO<A> Atomic<A>(Func<LiteData, A> operation) where A : notnull;

   // /// <summary>
   // /// Operation wrapped in transaction. Since there is no async in LiteDb, use Lift instead.<br/>
   // /// Using IO under the operation MUST run else it will cause LiteDb to commit even exception is being throwed.<br/>
   // /// CUD operation MUST include validation.
   // /// </summary>
   // /// <typeparam name="A"></typeparam>
   // /// <param name="operation"></param>
   // /// <returns></returns>
   // IO<A> Atomic<A>(Func<LiteData, Lift<A>> operation) where A : notnull;

   /// <summary>
   /// Find a document using the bsonExpresion predicate, option for bsonExpresion include.<br/>
   /// Remember to concrete(.ToList()) if finding document and later for deletion.<br/>
   /// No lock read even there is an active transaction.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="bsonExpPred"></param>
   /// <param name="bsonInclusion"></param>
   /// <returns></returns>
   IEnumerable<A> Find<A>(string bsonExpPred, string? bsonInclusion = default);

   /// <summary>
   /// Find a document using the predicate, option for bsonExpresion include.<br/>
   /// Remember to concrete(.ToList()) if finding document and later for deletion.<br/>
   /// No lock read even there is an active transaction.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="pred"></param>
   /// <param name="bsonInclusion"></param>
   /// <returns></returns>
   IEnumerable<A> Find<A>(Expression<Func<A, bool>> pred, string? bsonInclusion = default);

   /// <summary>
   /// Find the first document using predicate bson expression. Return null if not found.<br/>
   /// No lock read even there is an active transaction.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="bsonExpPred"></param>
   /// <param name="bsonInclusion"></param>
   /// <returns></returns>
   Option<A> FindOne<A>(string bsonExpPred, string? bsonInclusion = default);

   /// <summary>
   /// Find the first document using predicate expression. Return null if not found.<br/>
   /// No lock read even there is an active transaction.
   /// </summary>
   /// <typeparam name="A"></typeparam>
   /// <param name="pred"></param>
   /// <param name="bsonInclusion"></param>
   /// <returns></returns>
   Option<A> FindOne<A>(Expression<Func<A, bool>> pred, string? bsonInclusion = default);
}