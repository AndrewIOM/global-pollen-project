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
| EncyclopediaOfLife

and ThirdPartyTaxonId =
| NeotomaId of int
| GbifId of int
| EncyclopediaOfLifeId of int

type Event =
| Imported of Imported
| EstablishedConnection of TaxonId * ThirdPartyTaxonId

and Imported = {
    Id: TaxonId
    Group: TaxonomicGroup
    Identity: TaxonomicIdentity
    Parent: TaxonId option
    Status: TaxonomicStatus
    Reference: (string * Url option) option
}

type State =
| InitialState
| ValidatedByBackbone of TaxonState

and TaxonState = {
    Id: TaxonId
    Identity: TaxonomicIdentity
    Parent: TaxonId option
    Status: TaxonomicStatus
    Children: TaxonId list
    Links: ThirdPartyTaxonId list
    Reference: (string * Url option) option
    ValidatedAt: DateTime
}

let import (command:Import) validateInBackbone state =
    match command.Identity with
    | Family l ->
        match command.Parent with
        | None -> ignore()
        | Some parent -> invalidArg "A family cannot have a parent" "parent"
    | Genus l
    | Species (l,_,_) ->
        match command.Parent with
        | None -> invalidArg "Must specify a parent taxon for a genus" "parent"
        | Some parent ->
            match validateInBackbone (ValidateById parent) with
            | Some result -> ignore()
            | None -> invalidArg "Parent was not valid" "parent"

    [Imported { Id = command.Id
                Group = command.Group
                Identity = command.Identity
                Parent = command.Parent
                Status = command.Status
                Reference = command.Reference } ]

let connect thirdParty (connector:TaxonId->Result<int option,string>) state =
    match state with
    | InitialState -> invalidOp "Taxon does not exist"
    | ValidatedByBackbone t ->
        let apiResult = connector t.Id
        match apiResult with
        | Error e -> []
        | Ok externalId ->
            match externalId with
            | Some i ->
                match thirdParty with
                | Neotoma -> [ EstablishedConnection (t.Id, NeotomaId i) ]
                | GlobalBiodiversityInformationFacility -> [ EstablishedConnection (t.Id, GbifId i) ]
                | EncyclopediaOfLife -> [ EstablishedConnection (t.Id, EncyclopediaOfLifeId i) ]
            | None -> []


let handle deps = 
    function
    | ImportFromBackbone c -> import c deps.ValidateTaxon
    | ConnectToExternalDatabase (id,db) ->
        match db with
        | Neotoma -> connect Neotoma deps.GetNeotomaId
        | GlobalBiodiversityInformationFacility -> connect GlobalBiodiversityInformationFacility deps.GetGbifId
        | EncyclopediaOfLife -> connect EncyclopediaOfLife deps.GetEolId

type State with
    static member Evolve state = function
        | Imported event ->
            ValidatedByBackbone {
                Id = event.Id
                ValidatedAt = DateTime.UtcNow
                Identity = event.Identity
                Parent = event.Parent
                Status = event.Status
                Reference = event.Reference
                Children = []
                Links = [] }

        | EstablishedConnection (id,exId) ->
            match state with
            | InitialState -> invalidOp "Taxon does not exist"
            | ValidatedByBackbone vs ->
                ValidatedByBackbone { vs with Links = exId :: vs.Links }

let private unwrap (TaxonId e) = e
let getId = function
    | ImportFromBackbone c -> unwrap c.Id
    | ConnectToExternalDatabase (c,req) -> unwrap c
