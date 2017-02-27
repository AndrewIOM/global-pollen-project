module UserTests

open System
open Xunit
open GlobalPollenProject.Core.Types    
open GlobalPollenProject.Core.Aggregates.User

let a = {
    initial = State.InitialState
    evolve = State.Evolve
    handle = handle
    getId = getId 
}
let Given = Given a defaultDependencies

module ``When registering a new account`` =

    let id = UserId (Guid.NewGuid())
    let title = "Mr"
    let firstName = "Test"
    let lastName = "McTest"

    [<Fact>]
    let ``A new account is created`` =
        Given []
        |> When ( Register { Id = id; Title = title; FirstName = firstName; LastName = lastName } )
        |> Expect [ UserRegistered { Id = id; Title = title; FirstName = firstName; LastName = lastName } ]
    
    [<Fact>]
    let ``It fails if the account already exists`` =
        Given [ UserRegistered { Id = id; Title = title; FirstName = firstName; LastName = lastName } ]
        |> When ( Register { Id = id; Title = title; FirstName = firstName; LastName = lastName } )
        |> ExpectInvalidOp
