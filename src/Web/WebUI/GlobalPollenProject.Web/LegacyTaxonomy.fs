module GlobalPollenProject.Web.LegacyTaxonomy

/// The original published version of GPP contained an integer-indexed taxonomy.
/// The below functions polyfill a lookup to the new guid identifiers for
/// master reference collection taxon identifiers.

open System.IO

type TaxonLookup = { 
    OriginalId: int
    Rank: string
    Family: string
    Genus: string
    Species: string 
} with
    static member FromFile file = 
        file
        |> File.ReadAllLines
        |> Seq.skip 1
        |> Seq.map (fun s-> s.Split ',' |> fun a -> {OriginalId=int a.[0]; Rank=a.[1]; Family = a.[2]; Genus = a.[3]; Species = a.[4]})

let taxonLookup = TaxonLookup.FromFile @"Lookups/taxonlookup.csv"
