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
  IsAddingSlide: AddSlideModel option
}

// TODO Reference types from central location
and BackboneResult = {
    Id:Guid;
    Family:string
    Genus:string
    Species:string
    NamedBy:string
    LatinName:string
    Rank:string
    ReferenceName:string
    ReferenceUrl:string
}

and PagedResult<'TProjection> = {
    Items: 'TProjection list
    ItemTotal: int
    CurrentPage: int
    TotalPages: int
    ItemsPerPage: int
}

and SlideRequest = {
  BackboneTaxonId: Guid
  Collection: Guid
}

and AddSlideModel = {
  BackboneId: Guid option
  CollectionId: Guid option
  Rank: string
  Family: string
  Genus: string option
  Species: string option
  BackboneMatches: BackboneResult list
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

and TaxonSummary = {
    Id:Guid;
    Family:string
    Genus:string
    Species:string
    LatinName:string
    Rank:string
    SlideCount:int
    GrainCount:int
    ThumbnailUrl:string
}

and Frame = {
    Id: Guid
    Url: string
}

and SlideImage = {
    Id: int
    Frames: Frame list
    CalibrationImageUrl: string
    CalibrationFocusLevel: int
    PixelWidth: float
}

and Slide = {
    CollectionId: Guid
    CollectionSlideId: string
    Taxon: TaxonSummary
    IdentificationMethod: string
    FamilyOriginal: string
    GenusOriginal: string
    SpeciesOriginal: string
    IsFullyDigitised: bool
}

and RefCollectionDetail = {
    Id:Guid;
    User:Guid;
    Name:string;
    Status:string;
    Version: int;
    Description:string;
    Slides:Slide list;
}

type Message =
  | LoadCollections
  | EditCollection    of System.Guid
  | AddCollection
  | CreateCollection  of CreateCollectionMessage
  | AddSlide          of AddSlideMessage
  | OnListSuccess     of RefCollectionListItem list
  | OnDetailSuccess   of RefCollectionDetail
  | OnBackboneSuccess of PagedResult<BackboneResult>
  | OnSlideSubmitSuccess of obj
  | FetchFailure      of exn

and CreateCollectionMessage =
| CollectionName of string
| CollectionDescription of string
| SubmitCollection
| OnSubmitSuccess of obj
| OnSubmitFail of exn

and AddSlideMessage =
| BeginAddingSlide
| Rank of string
| Family of string
| Genus of string
| Species of string
| BackboneTaxon of Guid
| RequestBackboneMatches
| SubmitSlide

let init () =
  { Collections = []; IsEditing = None; IsCreatingCollection = None; IsAddingSlide = None }, Cmd.ofMsg LoadCollections

// -----------------------------------------------------------------------------------
// UPDATE
//

open Fable.Import
open Fable.Import.Browser
open Fable.PowerPack
open Fetch.Fetch_types

let queryBackbone name =
  promise {
    return! Fetch.fetchAs<PagedResult<BackboneResult>> (sprintf "/api/v1/backbone/search%s" name) [ Credentials RequestCredentials.Include ; Headers [ Cookie ".AspNetCore.Cookie" ] ]
  }
let getCollections f =
  promise {
    return! Fetch.fetchAs<RefCollectionListItem list> "/api/v1/collection/list" [ Credentials RequestCredentials.Include ; Headers [ Cookie ".AspNetCore.Cookie" ] ]
  }

let getDetail (id:Guid) =
  promise {
    return! Fetch.fetchAs<RefCollectionDetail> (sprintf "/api/v1/collection?id=%s" (id.ToString())) [ Credentials RequestCredentials.Include ; Headers [ Cookie ".AspNetCore.Cookie" ] ]
  }

let submitNewCollection (req: RefCollectionRequest) =
  promise {
    return! Fetch.postRecord "/api/v1/collection/start" req [ Credentials RequestCredentials.Include ; Headers [ Cookie ".AspNetCore.Cookie" ] ]
  }

let submitNewSlide (req: SlideRequest) =
  promise {
    return! Fetch.postRecord "/api/v1/collection/slide/add" req [ Credentials RequestCredentials.Include ; Headers [ Cookie ".AspNetCore.Cookie" ] ]
  }

let update msg model : Model * Cmd<Message> =
  match msg with

  // Commands
  | LoadCollections       -> { model with Collections = []}, Cmd.ofPromise getCollections "" OnListSuccess FetchFailure
  | EditCollection id     -> { model with IsEditing = None }, Cmd.ofPromise getDetail id OnDetailSuccess FetchFailure
  | AddCollection         -> { model with IsCreatingCollection = Some { Name = ""; Description = ""} }, []

  | OnListSuccess colList  -> { model with Collections = colList}, []
  | OnDetailSuccess detail -> { model with IsEditing = Some detail }, []
  | OnSlideSubmitSuccess s -> { model with IsAddingSlide = None }, []
  | FetchFailure ex        -> Browser.console.log (unbox ex.Message)
                              Browser.console.log "exception occured" |> ignore
                              model, []
  | OnBackboneSuccess s   ->  match model.IsAddingSlide with
                              | Some slide -> 
                                  Browser.console.log s
                                  { model with IsAddingSlide = Some { slide with BackboneMatches = s.Items } }, []
                              | None -> model, [] // Exn

  | CreateCollection m    -> match model.IsCreatingCollection with
                             | None -> model, []
                             | Some col -> 
                                  match m with
                                  | CollectionName name        -> { model with IsCreatingCollection = Some { col with Name = name } }, []
                                  | CollectionDescription desc -> { model with IsCreatingCollection = Some { col with Description = desc } }, []
                                  | SubmitCollection           -> model, Cmd.ofPromise submitNewCollection col OnListSuccess FetchFailure

  | AddSlide m            -> match model.IsAddingSlide with
                             | None ->
                                  match m with
                                  | BeginAddingSlide -> {model with IsAddingSlide = Some { BackboneId = None; BackboneMatches = []; CollectionId = None; Rank = "Species"; Family = ""; Genus = None; Species = None  } }, []
                                  | _ -> model, [] // Exception
                             | Some slide ->
                                  match m with
                                  | BeginAddingSlide -> model, [] // Exception
                                  | Rank r -> { model with IsAddingSlide = Some { slide with Rank = r } }, []
                                  | Family f -> { model with IsAddingSlide = Some { slide with Family = f } }, Cmd.ofMsg <| AddSlide RequestBackboneMatches
                                  | Genus g -> { model with IsAddingSlide = Some { slide with Genus = Some g } }, Cmd.ofMsg <| AddSlide RequestBackboneMatches
                                  | Species s -> { model with IsAddingSlide = Some { slide with Species = Some s } }, Cmd.ofMsg <| AddSlide RequestBackboneMatches
                                  | BackboneTaxon t -> { model with IsAddingSlide = Some { slide with BackboneId = Some t } }, Cmd.ofMsg <| AddSlide RequestBackboneMatches
                                  | SubmitSlide -> match slide.BackboneId with
                                                   | None -> model, [] //Exception
                                                   | Some bbid ->
                                                      match model.IsEditing with 
                                                      | None -> model, [] //Exception
                                                      | Some c -> model, Cmd.ofPromise submitNewSlide ({ Collection = c.Id; BackboneTaxonId = bbid }) OnSlideSubmitSuccess FetchFailure
                                  | RequestBackboneMatches -> 
                                      let query =
                                        match slide.Rank with
                                        | "Family" -> Some (sprintf "?Rank=Family&Family=%s&LatinName=%s" slide.Family slide.Family)
                                        | "Genus" -> 
                                            match slide.Genus with
                                            | None -> None
                                            | Some g -> Some (sprintf "?Rank=Genus&Family=%s&Genus=%s&LatinName=%s" slide.Family g g)
                                        | "Species" ->
                                            match slide.Genus with
                                            | None -> None
                                            | Some g ->
                                                match slide.Species with
                                                | None -> None        
                                                | Some s -> 
                                                    let latinName = sprintf "%s %s" g s
                                                    Some (sprintf "?Rank=Species&Family=%s&Genus=%s&Species=%s&LatinName=%s" slide.Family g s latinName)
                                        | _ -> None
                                      match query with
                                      | Some q -> model, Cmd.ofPromise queryBackbone q OnBackboneSuccess FetchFailure
                                      | None -> model, []


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
            R.button [ onClick (CreateCollection SubmitCollection); ClassName "btn btn-primary" ] [ R.str "Create" ]
            R.button [ ClassName "btn btn-secondary"; DataDismiss "modal" ] [ R.str "Cancel" ]
          ]
        ]
      ]
    ]

  let addSlide (c:RefCollectionDetail) (s:AddSlideModel) =

    let backboneResult =
      match s.BackboneMatches.Length with
      | 0 -> [ R.li [ ] [ R.str "No backbone matches." ] ]
      | _ ->
        s.BackboneMatches
        |> Seq.map (fun bbtaxon -> R.li [ onClick <| AddSlide (BackboneTaxon bbtaxon.Id) ] [ R.str bbtaxon.LatinName ] )
        |> Seq.toList
      
    let canSubmit = match s.BackboneId with
                    | Some b -> true
                    | None -> false

    R.div [ ClassName "modal fade show"; Id "addslide-modal"; TabIndex -1.; Role "Dialog" ] [
      R.div [ ClassName "modal-dialog modal-lg"; Role "document" ] [
        R.div [ ClassName "modal-content" ] [
          R.div [ ClassName "modal-header" ] [
            R.h5 [ ClassName "modal-title"; AriaLabel "Close" ] [ R.str (sprintf "%s: Add a slide" c.Name) ]
            R.button [ ClassName "close"; DataDismiss "modal" ] [ R.span [ AriaHidden true ] [ R.str "x" ] ]
          ]
          R.div [ ClassName "modal-body" ] [
            R.p [] [ R.str "This reference slide is of "
                     R.select [ ClassName "form-control input-sm inline-dropdown"; OnInput <| fun ev -> AddSlide (Rank (!!ev.target?value)) |> dispatch ] 
                          [ R.option [ Value (U2.Case1 "Species") ] [ R.str "Species" ]
                            R.option [ Value (U2.Case1 "Genus") ] [ R.str "Genus" ]
                            R.option [ Value (U2.Case1 "Family") ] [ R.str "Family" ] ]
                     R.str "rank." ]
            R.str "Please enter the original taxonomic identity given to the slide."
            R.div [ ClassName "row" ] [
              R.div [ ClassName "col-sm-4" ] [ R.input [ Type "text"; ClassName "form-control"; AutoComplete "off"; Placeholder "Family"; OnInput <| fun ev -> AddSlide (Family (!!ev.target?value)) |> dispatch ] [] ]
              R.div [ ClassName "col-sm-4" ] [ R.input [ Type "text"; ClassName "form-control"; AutoComplete "off"; Placeholder "Genus"; OnInput <| fun ev -> AddSlide (Genus (!!ev.target?value)) |> dispatch ] [] ]
              R.div [ ClassName "col-sm-4" ] [ R.input [ Type "text"; ClassName "form-control"; AutoComplete "off"; Placeholder "Species"; OnInput <| fun ev -> AddSlide (Species (!!ev.target?value)) |> dispatch ] [] ] ]
            R.small [ Id "taxon-help"; ClassName "form-text text-muted" ] [ R.str "This identity will be validated against the taxonomic backbone. If / when taxonomic changes occur, or have occurred, these will be reflected on this slide automatically." ]
            R.ul [ ClassName "backbone-list" ] backboneResult
            R.div [ ClassName "modal-footer" ] [
              R.button [ onClick (AddSlide SubmitSlide); Disabled (not canSubmit); ClassName "btn btn-primary" ] [ R.str "Create" ]
              R.button [ ClassName "btn btn-secondary"; DataDismiss "modal" ] [ R.str "Cancel" ] ] ] ] ] ]


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
          |> Seq.map (fun slide -> R.div [ ClassName "slide" ] [ (R.p [] [ R.str slide.FamilyOriginal] ); R.img [ Src slide.CollectionSlideId ] [] ] )
          |> Seq.toList

        let addSlide = 
          match state.IsAddingSlide with
          | Some s -> addSlide c s
          | None -> R.div [] []

        [ addSlide
          R.h3 [] [ R.str c.Name ]
          R.hr [] []
          R.button [ ClassName "btn btn-primary"; onClick (AddSlide BeginAddingSlide); DataToggle "modal"; Id "addslide-button"; DataTarget "#addslide-modal" ] [ R.str "Add New Slide" ]
          R.str c.Description
          R.table [ ClassName "table table-striped" ] [
            R.thead [] [
              R.tr [] [
                R.th [] [ R.str "#" ]
                R.th [] [ R.str "Identity" ]
                R.th [] [ R.str "Fully Digitised?" ] ] ]
            R.tbody [] slideList ] ]

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