module GlobalPollenProject.Core.Dependencies

open System
open GlobalPollenProject.Core.DomainTypes

let calculateTaxonomicIdentity backbone (ids:TaxonIdentification list) =

    let idWeights = 
        ids |> Seq.map (fun id -> match id with
                                  | Botanical (b,i,p) -> b, 0.95
                                  | Environmental e -> e, 0.25
                                  | Morphological m -> m, 0.25 ) |> Seq.toList

    let taxonWeights = 
        idWeights 
        |> List.groupBy fst
        |> List.map (fun x -> fst x, snd x |> List.map snd |> List.sum )
        |> List.sortByDescending snd
    
    let totalWeight = taxonWeights |> List.sumBy snd

    //70% agreement required, accounting for weighting of different ID types
    let highestTaxonPercentage = (snd taxonWeights.Head / totalWeight) * 100.
    match highestTaxonPercentage with
    | agreement when agreement >= 70.00 -> Some <| fst taxonWeights.Head
    | _ -> None
