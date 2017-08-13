module GlobalPollenProject.App.EventHandlers

open System

let deserialise<'a> json = 
    let unwrap (ReadStore.Json j) = j
    Serialisation.deserialiseCli<'a> (unwrap json)

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

    let getCol get (id:Guid) = 
        RepositoryBase.getSingle (id.ToString()) get deserialise<EditableRefCollection>

    let toGppTaxa (collection:EditableRefCollection) =
        collection.Slides
        |> List.choose (fun s -> s.CurrentTaxonId)
        |> List.map TaxonId

    let refreshPublishedTaxa get issueCommand colId =
        let taxa =
            colId 
            |> Converters.DomainToDto.unwrapRefId
            |> getCol get
            |> lift toGppTaxa
        taxa |> lift (List.map (refreshGbifConnection issueCommand)) |> ignore
        taxa |> lift (List.map (refreshNeotomaConnection issueCommand)) |> ignore

    let refresh get issueTaxonCommand (e:string*obj) =
        match snd e with
        | :? GlobalPollenProject.Core.Aggregates.ReferenceCollection.Event as e ->
            match e with
            | GlobalPollenProject.Core.Aggregates.ReferenceCollection.Event.CollectionPublished (id,d,v) -> refreshPublishedTaxa get issueTaxonCommand id
            | _ -> ()
        | _ -> ()