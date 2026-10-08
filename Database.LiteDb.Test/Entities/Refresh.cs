using System.Text.Json.Serialization;

namespace Database.LiteDb.Test.Entities;

public sealed record class Refresh: Key<string> {
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("scheme"),
              JsonPropertyOrder(1)]
   public required string Scheme { get; init; }

   /// <summary>
   /// This is required in DGS3 Authentication.
   /// </summary>
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("userId"),
              JsonPropertyOrder(2)]
   public required string? UserId { get; init; }

   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("value"),
              JsonPropertyOrder(3)]
   public required string Value { get; init; }

   /// <summary>
   /// This is required in DGS3 Authentication.
   /// </summary>
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("loginId"),
              JsonPropertyOrder(4)]
   public string? LoginId { get; init; }

   /// <summary>
   /// This is required in DGS3 Authentication.
   /// </summary>
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("roleId"),
              JsonPropertyOrder(5)]
   public required string? RoleId { get; init; }

   /// <summary>
   /// This is required for the creation of Access Token during reauthentication at DGS3 Authentication.
   /// </summary>
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("roleName"),
              JsonPropertyOrder(6)]
   public required string? RoleName { get; init; }

   /// <summary>
   /// This is required in DGS3 Authentication.
   /// </summary>
   /// <remarks>
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("PasswordChangedDate"),
              JsonPropertyOrder(7)]
   public required DateTimeOffset? PasswordChangedDate { get; init; }

   // DateTimeOffset is a value type where cannot be null.
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),
              JsonPropertyName("expiryDate"),
              JsonPropertyOrder(8)]
   public required DateTimeOffset ExpiryDate { get; init; }

   // DateTimeOffset is a value type where cannot be null.
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),
              JsonPropertyName("issuedDate"),
              JsonPropertyOrder(9)]
   public required DateTimeOffset IssuedDate { get; init; }

   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("accesses"),
              JsonPropertyOrder(10)]
   public required IList<Access> Accesses { get; init; } = [];

   /// <summary>
   /// This marked true only if first produced by Authentication or Reauthentication.
   /// </summary>
   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("isActive"),
              JsonPropertyOrder(11)]
   public required bool IsActive { get; init; } = false;

   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
              JsonPropertyName("deactivatedDate"),
              JsonPropertyOrder(12)]
   public DateTimeOffset? DeactivatedDate { get; init; }

   [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
           JsonPropertyName("isFault"),
           JsonPropertyOrder(13)]
   public required bool IsFault { get; init; } = false;
}
