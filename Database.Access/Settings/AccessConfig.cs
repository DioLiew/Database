using CSharpExt;

namespace Database.Access.Settings;

public sealed record class AccessConfig(string ConnectionString, Recurrence Recurrence) {
   public AccessConfig() : this(ConnectionString: string.Empty, Recurrence: new Recurrence()) { }
}
