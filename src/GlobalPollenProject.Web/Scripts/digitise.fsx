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

let sass = importAll<obj> "../Styles/Apps/_Digitise.scss"

// -----------------------------------------------------------------------------------
// MODEL
//

type Url = string

and Model = {
  Collections: RefCollectionListItem list
  Editing: RefCollectionDetail option
}

and RefCollectionListItem = {
  Name: string
  Id: System.Guid
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
  | FetchSuccess      of RefCollectionListItem list
  | DetailSuccess     of RefCollectionDetail
  | FetchFailure      of exn

let init () =
  { Collections = []; Editing = None }, Cmd.ofMsg LoadCollections

// -----------------------------------------------------------------------------------
// UPDATE
//

open Fable.Import
open Fable.Import.Browser
open Fable.PowerPack

let getCollections f =
  promise {
    return [{ Name = "Fake collection"; Id = System.Guid.NewGuid() }]
    //return! Fetch.fetchAs<RefCollectionListItem list> "/Digitise/MyCollections" []
  }

let getDetail f =
  promise {
    return {Id = System.Guid.NewGuid(); CollectionName = "Fake Collection"; Slides = [ { Identity = "Fraxinus"; Image = "https://pollen.blob.core.windows.net/production/1e38ca3c-931c-4035-b6a6-fbaedf839794-thumb.png" } ]}
    //return! Fetch.fetchAs<RefCollectionDetail> "/Digitise/Collection" []
  }

let update msg model : Model * Cmd<Message> =
  match msg with

  // Commands
  | LoadCollections       -> { model with Collections = []}, Cmd.ofPromise getCollections "" FetchSuccess FetchFailure
  | EditCollection id     -> { model with Editing = None }, Cmd.ofPromise getDetail "" DetailSuccess FetchFailure
  | AddCollection         -> model, []

  | FetchSuccess colList  -> { model with Collections = colList}, []
  | DetailSuccess detail  -> { model with Editing = Some detail }, []
  | FetchFailure ex       -> Browser.console.log (unbox ex.Message)
                             Browser.console.log "exception occured" |> ignore
                             model, []

// -----------------------------------------------------------------------------------
// VIEW
//

open Fable.Helpers.React.Props
module R = Fable.Helpers.React

let view (state:Model) dispatch =
  let onClick msg =
    OnClick <| fun _ -> msg |> dispatch

  let ribbon =
    [ R.button [ onClick AddCollection ] [ R.str "New Reference Collection" ] ]

  let collectionList =
    state.Collections
    |> Seq.map (fun col -> R.li [ onClick (EditCollection col.Id ) ] [ R.str col.Name ] )
    |> Seq.toList

  let detail =
    match state.Editing with
    | None -> [ R.label [] [ R.str "Select a collection to edit." ] ]
    | Some c ->
        let slideList =
          c.Slides
          |> Seq.map (fun slide -> R.div [ ClassName "slide" ] [ (R.p [] [ R.str slide.Identity] ); R.img [ Src slide.Image ] [] ] )
          |> Seq.toList
        R.h3 [] [ R.str c.CollectionName ] :: slideList

  R.div []
    [ R.div [ ClassName "button-ribbon" ] ribbon
      R.div [ ClassName "row" ]
          [ R.div [ ClassName "col-md-3" ]
              [ R.label [] [ R.str "My Collections" ]
                R.ul [] collectionList ]
            R.div [ ClassName "col-md-9" ] detail ] ]


// -----------------------------------------------------------------------------------
// APP
//

Program.mkProgram init update view
|> Program.withConsoleTrace
|> Program.withReact "digitise-app"
|> Program.run