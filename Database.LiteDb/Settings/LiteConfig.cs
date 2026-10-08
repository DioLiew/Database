namespace Database.LiteDb.Settings;

public sealed record class LiteConfig(string FileName, string Connection, TimeSpan Timeout) {
   public LiteConfig() : this(FileName: string.Empty, Connection: string.Empty, Timeout: new TimeSpan(0)) { }
}