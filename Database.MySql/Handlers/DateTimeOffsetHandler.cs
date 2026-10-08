using Dapper;
using System.Data;

namespace Database.MySql.Handlers;

// public class DateTimeUtcHandler: SqlMapper.TypeHandler<DateTime> {
//    public override void SetValue(IDbDataParameter parameter, DateTime value) =>
//       parameter.Value = value;

//    public override DateTime Parse(object value) =>
//       DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
// }

/// <summary>
/// Always treat DateTime Utc from mysql.
/// </summary>
public class DateTimeOffsetHandler: SqlMapper.TypeHandler<DateTimeOffset> {
   // public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) =>
   //    parameter.Value = value;

   // public override DateTimeOffset Parse(object value) =>
   //    DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);

   // saves as UTC kind to database.
   public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) =>
      parameter.Value = value.ToUniversalTime();

   // read as local kind from database.
   public override DateTimeOffset Parse(object value) =>
      DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc).ToLocalTime();
}
