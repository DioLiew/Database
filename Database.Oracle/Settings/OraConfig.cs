using CSharpExt;

namespace Database.Oracle.Settings;

public sealed record class OraConfig(string ConnectionString, Recurrence Recurrence) {
   public OraConfig() : this(ConnectionString: string.Empty, Recurrence: new Recurrence()) { }
}

// public sealed record class OraConfig(string ConnectionString) {
//    public OraConfig() : this(ConnectionString: string.Empty) { }
// }