module GlobalPollenProject.Core.Aggregates.Taxonomy

open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Aggregate
open System

type Command =
| ImportFromBackbone of Import
| ConnectToExternalDatabase of TaxonId * ThirdParty

and Import = {
    Id: TaxonId
    Group: TaxonomicGroup
    Identity: TaxonomicIdentity
    Parent: TaxonId option
    Status: TaxonomicStatus
    Reference: (string * Url option) option
}

and ThirdParty =
| Neotoma
| GlobalBiodiversityInformationFacility

type Event =
| Imported of Imported
| EstablishedConnection of EstablishedConnection

and Imported = {
    Id: TaxonId
    Group: TaxonomicGroup
    Identity: TaxonomicIdentity
    Parent: TaxonId option
    Status: TaxonomicStatus
    Reference: (string * Url option) option
}

and EstablishedConnection = {
    Id: TaxonId
    LinkTo: ThirdParty
    ForeignId: string
}

type State =
| InitialState
| ValidatedByBackbone of TaxonState

and TaxonState = {
    Identity: TaxonomicIdentity
    Parent: TaxonId option
    Status: TaxonomicStatus
    Children: TaxonId list
    Links: ExternalLink list
    Reference: (string * Url option) option
    ValidatedAt: DateTime
}

and ExternalLink = {
    ServiceName: ThirdParty
    ServiceId: string
    LastChecked: DateTime
}


let import (command:Import) validateInBackbone state =
    match command.Identity with
    | Family l ->
        match command.Parent with
        | None -> printfn "Parent validation successful"
        | Some parent -> invalidArg "A family cannot have a parent" "parent"
    | Genus l
    | Species (l,_,_) ->
        match command.Parent with
        | None -> invalidArg "Must specify a parent taxon for a genus" "parent"
        | Some parent -> printfn "Disabled taxonomic parent checking..."
            // match validateInBackbone (ValidateById parent) with
            // | Some result -> printfn "Validation Successful"
            // | None -> invalidArg "Parent was not valid" "parent"

    [Imported { Id = command.Id
                Group = command.Group
                Identity = command.Identity
                Parent = command.Parent
                Status = command.Status
                Reference = command.Reference } ]

let connectDatabase id database connector state =
    []

let handle deps = 
    function
    | ImportFromBackbone c -> import c deps.ValidateTaxon
    | ConnectToExternalDatabase (id,db) -> connectDatabase id db deps.GetGbifId

type State with
    static member Evolve state = function
        | Imported event ->
            ValidatedByBackbone {
                ValidatedAt = DateTime.UtcNow
                Identity = event.Identity
                Parent = event.Parent
                Status = event.Status
                Reference = event.Reference
                Children = []
                Links = [] }

        | EstablishedConnection e ->
            match state with
            | InitialState -> invalidOp "Taxon does not exist"
            | ValidatedByBackbone vs ->
                let link = { ServiceName = e.LinkTo; ServiceId = e.ForeignId; LastChecked = DateTime.Now } //TODO remove DateTime from here
                ValidatedByBackbone { vs with Links = link :: vs.Links }

let private unwrap (TaxonId e) = e
let getId = function
    | ImportFromBackbone c -> unwrap c.Id
    | ConnectToExternalDatabase (c,req) -> unwrap c
