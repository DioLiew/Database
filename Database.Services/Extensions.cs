using SqlKata.Execution;

namespace Database.Services;

public static class Extensions {
   extension(QueryFactory self) {
      /// <summary>
      /// Apply tableName to QueryFactory.
      /// </summary>
      /// <param name="tableName"></param>
      /// <returns></returns>
      public SqlKata.Query From(string tableName) => self.Query(tableName);
   }

   extension(SqlKata.Query self) {
      /// <summary>
      /// A variant of WhereIn, with nullable list.
      /// </summary>
      /// <typeparam name="A"></typeparam>
      /// <param name="list"></param>
      /// <param name="colName"></param>
      /// <returns></returns>
      public SqlKata.Query WhereIn<A>(IList<A>? list, string colName) => list is null ? self
      : self.Where(
         q => list.Aggregate(
            q, (s, x) => s.OrWhere(colName, x)
         )
      );
   }
}
