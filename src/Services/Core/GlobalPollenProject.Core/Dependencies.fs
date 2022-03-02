module GlobalPollenProject.Core.Dependencies

open System
open GlobalPollenProject.Core.DomainTypes

/// Get the IDs of parent taxa using the `getUpper` function
/// and return as a list with lowest resolution (family) first.
let rec idToIdHeirarchy getUpper h taxonId =
    match getUpper taxonId with
    | Error e -> Error e
    | Ok p ->
        match p with
        | Some t -> idToIdHeirarchy getUpper (taxonId :: h) t
        | None -> taxonId :: h |> Ok

type TaxonIdHeirarchyNode = private { Family: TaxonId; Genus: TaxonId option; Species: TaxonId option }

/// Organise the heirarchy into family, genus, and species.
let heirarchyToRanked (h:list<list<TaxonId>>) =
    h
    |> List.map(fun h ->
        match h.Length with
        | 1 -> { Family = h.[0]; Genus = None; Species = None } |> Ok
        | 2 -> { Family = h.[0]; Genus = Some h.[1]; Species = None } |> Ok
        | 3 -> { Family = h.[0]; Genus = Some h.[1]; Species = Some h.[2] } |> Ok
        | _ -> Error "There cannot be more than three ranks in the heirarchy" )
    |> mapResult id

/// Gets the weighting to give to different identification methods,
/// from the most certain (botanical) to least certain (morphological).
let weights id =
    match id with
    | Botanical (_,i,_) -> 
        match i with
        | HerbariumVoucher _ -> 0.95
        | LivingCollection _ -> 0.95
        | Field _ -> 0.70
        | Unknown -> 0.70
    | Environmental _ -> 0.25
    | Morphological _ -> 0.25

/// Calculate if an ID is accepted at a particular taxonomic rank.
let acceptedId identifications =
    if identifications |> List.isEmpty then None
    else 
        let idWeights = identifications |> List.map(fun (i,t) -> t, weights i )
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

/// Calculates a 'confirmed' taxonomic identity based on a set of proposed
/// taxonomic identities. 
let calculateTaxonomicIdentity (getParentId:TaxonId -> Result<TaxonId option,string>) (ids:TaxonIdentification list) =
    
    let heirarchies = 
        ids |> Seq.map (fun id -> match id with
                                  | Botanical (b,i,_) -> 
                                        match i with
                                        | HerbariumVoucher _ -> idToIdHeirarchy getParentId [] b
                                        | LivingCollection _ -> idToIdHeirarchy getParentId [] b
                                        | Field _ -> idToIdHeirarchy getParentId [] b
                                        | Unknown -> idToIdHeirarchy getParentId [] b
                                  | Environmental (e,_) -> idToIdHeirarchy getParentId [] e
                                  | Morphological (m,_) -> idToIdHeirarchy getParentId [] m ) 
            |> Seq.toList 
            |> mapResult id
            |> bind heirarchyToRanked
            |> lift (List.zip ids)
    
    match heirarchies with
    | Error e -> Error e
    | Ok h ->
        let species = h |> List.where(fun (_,t) -> t.Species.IsSome ) |> List.map(fun (i,t) -> i, t.Species.Value) |> acceptedId
        let genus = h |> List.where(fun (_,t) -> t.Genus.IsSome ) |> List.map(fun (i,t) -> i, t.Genus.Value) |> acceptedId
        let family = h |> List.map(fun (i,t) -> i, t.Family) |> acceptedId
        if species.IsSome then Ok species
        else if genus.IsSome then Ok genus
        else Ok family


let calculatePointsScore (currentTime:DateTime) (timeSubmitted:DateTime) =
    let t = float (currentTime - timeSubmitted).Days
    let s0 = 1.0
    let r = 0.0015
    let l = 10.
    let k = 20.
    let scoreAtTime = Math.Floor(l + (s0 * (k - l)) / (s0 + (k - l - s0) * Math.Exp(-r * t)))
    if t < 5. then scoreAtTime * (1. - (0.2 * t))
    else if t >= 5. && t < 8. then scoreAtTime * (0.2 * (8. - t))
    else scoreAtTime