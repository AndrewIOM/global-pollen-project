module GlobalPollenProject.Core.Aggregates.ReferenceCollection

open GlobalPollenProject.Core.Types
open System

type Command =
| CreateCollection of CreateCollection
| AddSlide of AddSlide
| Publish of CollectionId

and CreateCollection = {Id:CollectionId; Name:string; Owner:UserId}
and AddSlide = {Id:CollectionId; Images: Image list; Taxon: TaxonId}

type Event =
| DigitisationStarted of DigitisationStarted

and DigitisationStarted = {Id: CollectionId; Name: string; Owner: UserId}

type State =
| Initial
| Draft of RefState
| Complete of RefState

and RefState = {
    Owner: UserId
    Name: string
    Description: string option
    Slides: SlideState list}

and SlideState = {
    Images: Image list
}

let create (command:CreateCollection) state =
    [DigitisationStarted {Id = command.Id; Name = command.Name; Owner = command.Owner}]

let handle deps = 
    function
    | CreateCollection c -> create c
    // | AddSlide c -> 2.
    // | Publish c -> 2.

type State with
    static member Evolve state = function
        | DigitisationStarted event ->
            Draft {
                Name = event.Name
                Owner = event.Owner
                Description = None
                Slides = []
            }

let getId = 
    let unwrap (CollectionId e) = e
    function
    | CreateCollection c -> unwrap c.Id
    | AddSlide c -> unwrap c.Id
    | Publish c -> unwrap c
