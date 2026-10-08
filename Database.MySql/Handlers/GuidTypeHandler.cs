using Dapper;
using System.Data;

namespace Database.MySql.Handlers;

/// <summary>
/// This has been taken care by MySqlConnector where it supports option guidformat = none where CHAR(36) treats as string.
/// </summary>
public class GuidTypeHandler: SqlMapper.TypeHandler<Guid> {
   // public override void SetValue(IDbDataParameter parameter, Guid guid) {
   //    parameter.Value = guid.ToString();
   // }

   // public override Guid Parse(object value) {
   //    return new Guid((string)value);
   // }

   public override void SetValue(IDbDataParameter parameter, Guid guid) =>
      parameter.Value = guid.ToString();

   public override Guid Parse(object value) =>
      Guid.Parse(value.ToString() ?? throw new Exception($"Fail parsing {value} to GUID."));

}