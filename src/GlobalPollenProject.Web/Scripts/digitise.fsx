#r "../node_modules/fable-core/Fable.Core.dll"
#r "../node_modules/fable-react/Fable.React.dll"
#r "../node_modules/fable-elmish/Fable.Elmish.dll"
#r "../node_modules/fable-elmish-react/Fable.Elmish.React.dll"
#load "../node_modules/fable-import-d3/Fable.Import.D3.fs"
#load "../node_modules/fable-import-fetch/Fable.Import.Fetch.fs"
#load "../node_modules/fable-import-fetch/Fable.Helpers.Fetch.fs"

open Elmish
open Elmish.React
open Fable.Import
open Fable.Import.Browser
open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.Import.Fetch
open Fable.Helpers.Fetch
let sass = importAll<obj> "../Styles/main.scss"

type TaxonSummary = {
    Id:System.Guid;
    Family:string
    Genus:string
    Species:string
    LatinName:string
    Rank:string
    SlideCount:int
    GrainCount:int
    ThumbnailUrl:string
}

// Infrastructure

let fetch =
  let baseUrl = "http://localhost:5000"
  async { 
      try 
          let! records = fetchAs<TaxonSummary[]> ( sprintf "%s/api/taxon" baseUrl, [] )
          return Some records
      with
      | error -> 
        printfn "Error downloading taxonomy"
        return None
  }

// Types
type Msg =
  | Increment
  | Decrement

let init () = 0


// Handlers
let update (msg:Msg) count =
  match msg with
  | Increment -> count + 1
  | Decrement -> count - 1


// View
open Fable.Helpers.React.Props
module R = Fable.Helpers.React

let view count dispatch =
  let onClick msg =
    OnClick <| fun _ -> msg |> dispatch

  R.div []
    [ R.button [ onClick Decrement ] [ R.str "-" ]
      R.div [] [ R.str (string count) ]
      R.button [ onClick Increment ] [ R.str "+" ] ]

// App
Program.mkSimple init update view
|> Program.withConsoleTrace
|> Program.withReact "elmish-app"
|> Program.run