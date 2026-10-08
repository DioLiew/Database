using System.Text.Json.Serialization;

namespace Database.LiteDb.Test.Entities;

public abstract record class Key<A>: IKey<A> {
   [property: JsonPropertyName("id"),
              JsonPropertyOrder(0)]
   public required virtual A Id { get; init; }
}

interface IKey<A> {
   A Id { get; init; }
}
