module GlobalPollenProject.Core.Aggregates.ReferenceCollection

open GlobalPollenProject.Core.Types
open System

type Command =
| CreateCollection
| AddSlide
| Publish

type Event =
| NewDraftCollection

type State =
| Initial
| Draft of RefState
| Complete of RefState

and RefState = {
    Owner: UserId
    Name: string
    Description: string
    Slides: SlideState list}

and SlideState = {
    Images: Image list
}

