using CSharpExt;

namespace Database.MySql.Settings;

public sealed record class MySqlConfig(string ConnectionString, Recurrence Recurrence) {
   public MySqlConfig() : this(ConnectionString: string.Empty, Recurrence: new Recurrence()) { }
}