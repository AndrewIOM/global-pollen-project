module GlobalPollenProject.Core.Aggregates.User

open System
open GlobalPollenProject.Core.DomainTypes

// NB The domain user only contains PROFILE-related information
// No passwords, logins etc. are dealt with here.
// They are dealt with in GPP.Shared.Identity

[<AutoOpen>]
module Points =
    type Points = Points of float
    let create (amount:float) = Math.Round(amount, 1)

type UserContribution =
| TaxonomicIdentity of GrainId

type Command =
| Register of Register
| Contribute of UserId * UserContribution * Points
| ActivatePublicProfile of UserId
| DisablePublicProfile of UserId
| GrantCurationRights of UserId

and Register = {
    Id: UserId
    Title: string
    FirstName: string
    LastName: string
    PublicProfile: bool
    Organisation: ShortText
}

type Event =
| UserRegistered of UserRegistered
| ProfileMadePublic of UserId
| ProfileHidden of UserId
| JoinedOrganisation of UserId * ShortText
| BecameCurator of UserId

and UserRegistered = {
    Id: UserId
    Title: string
    FirstName: string
    LastName: string
}

type State =
| InitialState
| Registered of UserState

and UserState = {
    Title: string
    FirstName: string
    LastName: string
    PrimaryClub: ShortText option
    OtherClubs: ShortText list
    IsPubliclyVisible: bool
    Curator: bool
}

let register (command:Register) state =
    match state with
    | InitialState ->
        let registered = 
            [ UserRegistered {  Id = command.Id
                                Title = command.Title
                                FirstName = command.FirstName
                                LastName = command.LastName };
              JoinedOrganisation (command.Id,command.Organisation) ]
        match command.PublicProfile with
        | false -> registered
        | true -> (ProfileMadePublic command.Id) :: registered
    | _ -> 
        invalidOp "This user has already registered"

let activateProfile command state =
    match state with
    | Registered s ->
        [ ProfileMadePublic command ]
    | _ -> 
        invalidOp "User does not exist"

let deactivateProfile command state =
    match state with
    | Registered s ->
        [ ProfileHidden command ]
    | _ -> 
        invalidOp "User does not exist"

let joinOrganisation userId organisationName state =
    match state with
    | Registered s ->
        // Check that user is not already a member of this organisation
        [ JoinedOrganisation (userId,organisationName) ]
    | _ -> 
        invalidOp "User does not exist"

let contribute userId contribution points state =
    invalidOp "Not implemented"

let grantCuration userId state =
    match state with
    | Registered s ->
        match s.Curator with
        | true -> invalidOp <| sprintf "The user %A is already a curator" userId
        | false -> [ BecameCurator userId ]
    | _ -> invalidOp "User does not exist"

let handle deps = 
    function
    | Register command -> register command
    | ActivatePublicProfile command -> activateProfile command
    | DisablePublicProfile command -> deactivateProfile command
    | GrantCurationRights u -> grantCuration u
    | Contribute (u,c,p) -> contribute u c p

let private unwrap (UserId e) = e
let getId = function
    | Register c -> unwrap c.Id
    | ActivatePublicProfile c -> unwrap c
    | DisablePublicProfile c -> unwrap c
    | GrantCurationRights c -> unwrap c
    | Contribute (c,_,_) -> unwrap c

type State with
    static member Evolve state event =
        match state with
        | InitialState ->
            match event with
            | UserRegistered e ->
                Registered {
                    FirstName = e.FirstName
                    LastName = e.LastName
                    Title = e.Title
                    IsPubliclyVisible = false
                    PrimaryClub = None
                    Curator = false
                    OtherClubs = []
                }
            | _ -> invalidOp "User is not registered"
        | Registered regState ->
            match event with
            | UserRegistered _ -> invalidOp "User is already registered"
            | ProfileMadePublic _ -> Registered { regState with IsPubliclyVisible = true }
            | ProfileHidden _ -> Registered { regState with IsPubliclyVisible = false }
            | JoinedOrganisation (_,orgName) -> 
                match regState.PrimaryClub with
                | None -> Registered { regState with PrimaryClub = Some orgName }
                | Some _ -> Registered { regState with OtherClubs = orgName :: regState.OtherClubs }
            | BecameCurator _ -> Registered { regState with Curator = true }
