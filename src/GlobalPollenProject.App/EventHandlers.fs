module GlobalPollenProject.App.EventHandlers

open System

let inline deserialise< ^a> json = 
    let unwrap (ReadStore.Json j) = j
    Serialisation.deserialise< ^a> (unwrap json)

module ExternalConnections =

    open GlobalPollenProject.Core.Composition
    open GlobalPollenProject.Core.DomainTypes
    open GlobalPollenProject.Core.Aggregates.Taxonomy
    open GlobalPollenProject.Core.Aggregate
    open ReadStore
    open ReadModels

    let refreshNeotomaConnection issueTaxonCommand taxonId = 
        ConnectToExternalDatabase (taxonId,ThirdParty.Neotoma)
        |> issueTaxonCommand
        |> ignore

    let refreshGbifConnection issueTaxonCommand taxonId = 
        ConnectToExternalDatabase (taxonId,ThirdParty.GlobalBiodiversityInformationFacility)
        |> issueTaxonCommand
        |> ignore

    let refreshEolConnection issueTaxonCommand taxonId = 
        ConnectToExternalDatabase (taxonId,ThirdParty.EncyclopediaOfLife)
        |> issueTaxonCommand
        |> ignore

    let getCol get (id:Guid) = 
        RepositoryBase.getSingle (id.ToString()) get deserialise<EditableRefCollection>

    let nullableToOption (n:Nullable<'a>) =
        if n.HasValue then Some n.Value else None

    let getHeirarchyIds get (slide:SlideDetail) =
        match slide.CurrentTaxonId with
        | None -> Ok []
        | Some i ->
            RepositoryBase.getSingle<BackboneTaxon> (i.ToString()) get deserialise
            |> lift (fun t -> [Some t.FamilyId; nullableToOption t.GenusId; nullableToOption t.SpeciesId] )
            |> lift (List.choose id)

    let toGppTaxa get (collection:EditableRefCollection) =
        collection.Slides
        |> List.filter (fun s -> s.IsFullyDigitised)
        |> List.map (fun s -> getHeirarchyIds get s)
        |> mapResult id
        |> lift (fun x -> x |> List.concat)
        |> lift List.distinct
        |> lift (List.map TaxonId)

    let refreshPublishedTaxa get issueCommand colId =
        let taxa =
            colId 
            |> Converters.DomainToDto.unwrapRefId
            |> getCol get
            |> bind (toGppTaxa get)
        taxa |> lift (List.map (refreshGbifConnection issueCommand)) |> ignore
        taxa |> lift (List.map (refreshEolConnection issueCommand)) |> ignore
        taxa |> lift (List.map (refreshNeotomaConnection issueCommand)) |> ignore

    let refresh get issueTaxonCommand (e:string*obj*DateTime) =
        let ev (s,o,d) = o
        match e |> ev with
        | :? GlobalPollenProject.Core.Aggregates.ReferenceCollection.Event as e ->
            match e with
            | GlobalPollenProject.Core.Aggregates.ReferenceCollection.Event.CollectionPublished (id,d,v) -> refreshPublishedTaxa get issueTaxonCommand id
            | _ -> ()
        | _ -> ()