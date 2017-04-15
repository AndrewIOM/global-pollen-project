#r "../node_modules/fable-core/Fable.Core.dll"
#r "../node_modules/fable-react/Fable.React.dll"
#r "../node_modules/fable-elmish/Fable.Elmish.dll"
#r "../node_modules/fable-elmish-react/Fable.Elmish.React.dll"
#load "../node_modules/fable-import-d3/Fable.Import.D3.fs"

open Elmish
open Elmish.React
open Fable.Import
open Fable.Import.Browser
open Fable.Core
open Fable.Core.JsInterop
open Fable.React

type Msg =
  | Increment
  | Decrement

let init () = 0

let update (msg:Msg) count =
  match msg with
  | Increment -> count + 1
  | Decrement -> count - 1


open Fable.Helpers.React.Props
module R = Fable.Helpers.React

let view count dispatch =
  let onClick msg =
    OnClick <| fun _ -> msg |> dispatch

  R.div []
    [ R.button [ onClick Decrement ] [ R.str "-" ]
      R.div [] [ R.str (string count) ]
      R.button [ onClick Increment ] [ R.str "+" ] ]


open Elmish.React

Program.mkSimple init update view
|> Program.withConsoleTrace
|> Program.withReact "elmish-app"
|> Program.run