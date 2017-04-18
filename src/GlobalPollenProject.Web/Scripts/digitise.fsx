#r "../node_modules/fable-core/Fable.Core.dll"
#r "../node_modules/fable-elmish/Fable.Elmish.dll"
#r "../node_modules/fable-react/Fable.React.dll"
#r "../node_modules/fable-elmish-react/Fable.Elmish.React.dll"
#r "../node_modules/fable-powerpack/Fable.PowerPack.dll"
#load "../node_modules/fable-import-d3/Fable.Import.D3.fs"

open Fable.Core
open Fable.Core.JsInterop
open Elmish
open Elmish.React
open System

let sass = importAll<obj> "../Styles/Apps/_Digitise.scss"

// -----------------------------------------------------------------------------------
// MODEL
//

type Url = string

and Model = {
  Collections: RefCollectionListItem list
  IsEditing: RefCollectionDetail option
  IsCreatingCollection: RefCollectionRequest option
}

and RefCollectionRequest = {
  Name: string
  Description: string
}

and RefCollectionListItem = {
    Id:Guid;
    User:Guid;
    Name:string;
    Description:string;
    SlideCount:int;
}

and RefCollectionDetail = {
  Id: System.Guid
  CollectionName: string
  Slides: Slide list
}

and Slide = {
  Identity: string
  Image: string
}

type Message =
  | LoadCollections
  | EditCollection    of System.Guid
  | AddCollection
  | CreateCollection  of CreateCollectionMessage
  | FetchSuccess      of RefCollectionListItem list
  | DetailSuccess     of RefCollectionDetail
  | FetchFailure      of exn

and CreateCollectionMessage =
| CollectionName of string
| CollectionDescription of string
| Submit
| OnSubmitSuccess of obj
| OnSubmitFail of exn

let init () =
  { Collections = []; IsEditing = None; IsCreatingCollection = None }, Cmd.ofMsg LoadCollections

// -----------------------------------------------------------------------------------
// UPDATE
//

open Fable.Import
open Fable.Import.Browser
open Fable.PowerPack
open Fetch.Fetch_types

let getCollections f =
  promise {
    return! Fetch.fetchAs<RefCollectionListItem list> "/api/v1/collection/list" [ Credentials RequestCredentials.Include ; Headers [ Cookie ".AspNetCore.Cookie" ] ]
  }

let getDetail f =
  promise {
    return! Fetch.fetchAs<RefCollectionDetail> "/api/v1/collection/" [ Credentials RequestCredentials.Include ; Headers [ Cookie ".AspNetCore.Cookie" ] ]
  }

let submitNewCollection (req: RefCollectionRequest) =
  promise {
    return! Fetch.postRecord "/api/v1/collection/start" req [ Credentials RequestCredentials.Include ; Headers [ Cookie ".AspNetCore.Cookie" ] ]
  }

let update msg model : Model * Cmd<Message> =
  match msg with

  // Commands
  | LoadCollections       -> { model with Collections = []}, Cmd.ofPromise getCollections "" FetchSuccess FetchFailure
  | EditCollection id     -> { model with IsEditing = None }, Cmd.ofPromise getDetail "" DetailSuccess FetchFailure
  | AddCollection         -> { model with IsCreatingCollection = Some { Name = ""; Description = ""} }, []

  | FetchSuccess colList  -> { model with Collections = colList}, []
  | DetailSuccess detail  -> { model with IsEditing = Some detail }, []
  | FetchFailure ex       -> Browser.console.log (unbox ex.Message)
                             Browser.console.log "exception occured" |> ignore
                             model, []

  | CreateCollection m    -> match model.IsCreatingCollection with
                             | None -> model, []
                             | Some col -> 
                                  match m with
                                  | CollectionName name        -> { model with IsCreatingCollection = Some { col with Name = name } }, []
                                  | CollectionDescription desc -> { model with IsCreatingCollection = Some { col with Description = desc } }, []
                                  | Submit                     -> model, Cmd.ofPromise submitNewCollection col FetchSuccess FetchFailure

// -----------------------------------------------------------------------------------
// VIEW
//

open Fable.Helpers.React.Props
module R = Fable.Helpers.React

let view (state:Model) dispatch =
  let onClick (msg:Message) =
    OnClick <| fun _ -> msg |> dispatch

  let createCollection =
    R.div [ ClassName "modal fade show"; Id "create-modal"; TabIndex -1.; Role "Dialog" ] [
      R.div [ ClassName "modal-dialog modal-lg"; Role "document" ] [
        R.div [ ClassName "modal-content" ] [
          R.div [ ClassName "modal-header" ] [
            R.h5 [ ClassName "modal-title"; AriaLabel "Close" ] [ R.str "Digitise a Collection" ]
            R.button [ ClassName "close"; DataDismiss "modal" ] [ R.span [ AriaHidden true ] [ R.str "x" ] ]
          ]
          R.div [ ClassName "modal-body" ] [
            R.str "Please tell us about the reference collection you wish to digitise. You can edit this information later if necessary."
            R.div [ ClassName "form-group" ] [
              R.label [] [ R.str "Collection Name" ]
              R.input [ ClassName "form-control"
                        Id "name"
                        OnInput <| fun ev -> CreateCollection (CollectionName (!!ev.target?value)) |> dispatch ] []
              R.small [ Id "name-help"; ClassName "form-text text-muted" ] [ R.str "Use a name specific to the collection." ]
            ]
            R.div [ ClassName "form-group" ] [
              R.label [] [ R.str "Description" ]
              R.textarea [ Rows 3.
                           ClassName "form-control"
                           Id "description"
                           OnInput <| fun ev -> CreateCollection (CollectionDescription (!!ev.target?value)) |> dispatch ] []
              R.small [ Id "description-help"; ClassName "form-text text-muted" ] [ R.str "Your collection description could include the motivation for creating the collection, geographical coverage, or the nature of the material, for example." ]
            ]
          ]
          R.div [ ClassName "modal-footer" ] [
            R.button [ onClick (CreateCollection Submit); ClassName "btn btn-primary" ] [ R.str "Create" ]
            R.button [ ClassName "btn btn-secondary"; DataDismiss "modal" ] [ R.str "Cancel" ]
          ]
        ]
      ]
    ]


  let ribbon =

    [ R.button [ onClick AddCollection; ClassName "btn btn-secondary"; DataToggle "modal"; Id "create-button"; DataTarget "#create-modal" ] [ R.str "New Reference Collection" ]
      R.button [ ClassName "btn btn-secondary"; DataToggle "modal"; Id "calibrate-button"; DataTarget "#calibrate-modal" ] [ R.str "Calibrate" ] ]

  let collectionList =
    match state.Collections.Length with
    | 0 -> [ R.li [ ClassName "list-group-item" ] [ R.str "You have yet to digitise a reference collection." ] ]
    | _ ->
      state.Collections
      |> Seq.map (fun col -> R.li [ onClick (EditCollection col.Id); ClassName "list-group-item" ] [ R.str col.Name ] )
      |> Seq.toList

  let detail =
    match state.IsEditing with
    | None -> [ R.label [] [ R.str "Select a collection to edit." ] ]
    | Some c ->
        let slideList =
          c.Slides
          |> Seq.map (fun slide -> R.div [ ClassName "slide" ] [ (R.p [] [ R.str slide.Identity] ); R.img [ Src slide.Image ] [] ] )
          |> Seq.toList
        R.h3 [] [ R.str c.CollectionName ] :: slideList

  R.div []
    [ createCollection
      R.div [ ClassName "btn-toolbar mb-3"; Role "toolbar" ] ribbon
      R.div [ ClassName "row" ]
          [ R.div [ ClassName "col-md-3" ]
              [ R.label [] [ R.str "My Collections" ]
                R.ul [ ClassName "list-group" ] collectionList ]
            R.div [ ClassName "col-md-9" ] detail ] ]


// -----------------------------------------------------------------------------------
// APP
//

Program.mkProgram init update view
|> Program.withConsoleTrace
|> Program.withReact "digitise-app"
|> Program.run