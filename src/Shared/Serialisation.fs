module Serialisation

open Microsoft.FSharp.Reflection
open Microsoft.FSharpLu.Json

let serialise (o:'a) = 
    try Compact.serialize o |> Ok
    with
    | _ -> Error "Could not serialise record"

let inline deserialise< ^T> json = 
    try BackwardCompatible.deserialize< ^T> json |> Ok
    with 
    | _ -> Error "Could not deserialise JSON"

// Test of shared service definitions

type Slide = int

type CoreUseCase = {
    Taxonomy: TaxonomyUseCase
}

/// Digitisation tools
and TaxonomyUseCase = {

    /// Get the list of all books in the collection.
    getSlide: string -> string -> Async<Slide>

}