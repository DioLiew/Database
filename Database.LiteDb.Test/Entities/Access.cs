using System.Text.Json.Serialization;

namespace Database.LiteDb.Test.Entities;

public sealed record class Access: Key<string> {
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("scheme"),
              JsonPropertyOrder(1)]
   public required string Scheme { get; init; }

   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("userId"),
              JsonPropertyOrder(2)]
   public required string UserId { get; init; }

   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("value"),
              JsonPropertyOrder(3)]
   public required string Value { get; init; }

   // DateTimeOffset is a value type where cannot be null.
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),
              JsonPropertyName("expiryDate"),
              JsonPropertyOrder(4)]
   public required DateTimeOffset ExpiryDate { get; init; }

   // DateTimeOffset is a value type where cannot be null.
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),
              JsonPropertyName("issuedDate"),
              JsonPropertyOrder(5)]
   public required DateTimeOffset IssuedDate { get; init; }

   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("refresh"),
              JsonPropertyOrder(6)]
   public required Refresh? Refresh { get; init; }

   /// <summary>
   /// This marked true only if first produced by Authentication or Reauthentication.
   /// </summary>
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("isActive"),
              JsonPropertyOrder(7)]
   public required bool IsActive { get; init; } = false;

   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("deactivatedDate"),
              JsonPropertyOrder(8)]
   public DateTimeOffset? DeactivatedDate { get; init; }

   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("isFault"),
              JsonPropertyOrder(9)]
   public required bool IsFault { get; init; } = false;

}
