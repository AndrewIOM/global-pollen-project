module GlobalPollenProject.Core.Aggregates.User

open System
open GlobalPollenProject.Core.Types

// NB The domain user only contains PROFILE-related information
// No passwords, logins etc. are dealt with here.
// They are dealt with in GPP.Shared.Identity

type Command =
| Register of Register
| ActivatePublicProfile of UserId
| DisablePublicProfile of UserId
| JoinClub of UserId * ClubId

and Register = {
    Id: UserId
    Title: string
    FirstName: string
    LastName: string
}

type Event =
| UserRegistered of UserRegistered
| ProfileMadePublic of UserId
| ProfileHidden of UserId
| JoinedClub of UserId * ClubId

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
    CanDigitise: bool
    PrimaryClub: ClubId option
    OtherClubs: ClubId list
}

let register (command:Register) state =
    match state with
    | InitialState ->
        [ UserRegistered {  Id = command.Id
                            Title = command.Title
                            FirstName = command.FirstName
                            LastName = command.LastName }]
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

let joinClub command state =
    match state with
    | Registered s ->
        [ JoinedClub command ]
    | _ -> 
        invalidOp "User does not exist"

let handle deps = 
    function
    | Register command -> register command
    | ActivatePublicProfile command -> activateProfile command
    | DisablePublicProfile command -> deactivateProfile command
    //| JoinClub c,x -> joinClub c,x

let private unwrap (UserId e) = e
let getId = function
    | Register c -> unwrap c.Id
    | ActivatePublicProfile c -> unwrap c
    | DisablePublicProfile c -> unwrap c
    //| JoinClub c,_ -> unwrap c

type State with
    static member Evolve state = function

        | UserRegistered e ->
            Registered {
                FirstName = e.FirstName
                LastName = e.LastName
                Title = e.Title
                CanDigitise = false
                PrimaryClub = None
                OtherClubs = []
            }