module GlobalPollenProject.Traits.Client.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client

type result<'a> = Result<'a,ServiceError>

/// Details required for the taxon summary card.
[<CLIMutable>]
type SimpleTaxonViewModel = {
    Id:                 Guid
    Family:             string
    Genus:              string
    Species:            string
    LatinName:          string
    Authorship:         string
    Rank:               string
    NeotomaId:          int
    GbifId:             int
    EolId:              int
    EolCache:           ReadModels.EncyclopediaOfLifeCache
    ReferenceName:      string
    ReferenceUrl:       string
}

type RequestedTrait =
    | Shape
    | Size
    | WallThickness
    | Pattern
    | Pores

type DelineationTaskViewModel = {
    Slide: ReadModels.SlideImage
    Taxon: SimpleTaxonViewModel
}

type TagTraitViewModel = {
    GrainId: Guid
    ImageId: int
    Image: ReadModels.SlideImage
    Taxon: SimpleTaxonViewModel
    Trait: RequestedTrait
}

type TraitQuestion =
    | TagQuestion of TagTraitViewModel
    | DelineateQuestion of DelineationTaskViewModel


/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/about">] Help

/// The Elmish application's model.
type Model =
    {
        page: Page
        error: string option
        question: option<TraitQuestion>
        signedInAs: option<string>
        signInFailed: bool
        activeTrait: RequestedTrait option
        traitQualitativeValue: string option
        traitQuantitativeValue1: float option
        traitQuantitativeValue2: float option
    }

let initModel =
    {
        page = Home
        error = None
        question = None
        signedInAs = None
        signInFailed = false
        activeTrait = None
        traitQualitativeValue = None
        traitQuantitativeValue1 = None
        traitQuantitativeValue2 = None
    }

/// Remote service definition.
type ITraitService =
    {
        /// Get a citizen science question from the server.
        getNextQuestion: unit -> Async<TraitQuestion>

        /// Add a book in the collection.
        delineate: DelineateSpecimenRequest -> Async<unit result>

        /// Try to submit a citizen science tag for a trait on an image.
        tagTrait: TagTraitRequest -> Async<unit result>

        /// Sign into the application.
        signIn : unit -> Async<option<string>>

        /// Get the user's name, or None if they are not authenticated.
        getUsername : unit -> Async<string>

        /// Sign out from the application.
        signOut : unit -> Async<unit>
    }

    interface IRemoteService with
        member __.BasePath = "/trait-service"

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | GetQuestion
    | GotQuestion of TraitQuestion
    | GetSignedInAs
    | RecvSignedInAs of option<string>
    | SendSignIn
    | RecvSignIn of option<string>
    | SendSignOut
    | RecvSignOut
    | Error of exn
    | ClearError
    | ChangeTraitQual of RequestedTrait * string
    | ChangeTraitQuant of RequestedTrait * float * float
    | SendTraitTag
    | RecvTraitTag of unit result

let optionToEmptyStr str =
    match str with
    | Some s -> s
    | None -> ""

let update remote message model =
    let onSignIn = function
        | Some _ -> Cmd.ofMsg GetQuestion
        | None -> Cmd.none
    match message with
    | SetPage page ->
        { model with page = page }, Cmd.none

    | GetQuestion ->
        let cmd = Cmd.OfAsync.either remote.getNextQuestion () GotQuestion Error
        { model with question = None }, cmd
    | GotQuestion question ->
        { model with question = Some question }, Cmd.none

    | GetSignedInAs ->
        model, Cmd.OfAuthorized.either remote.getUsername () RecvSignedInAs Error
    | RecvSignedInAs username ->
        { model with signedInAs = username }, onSignIn username
    | SendSignIn ->
        model, Cmd.OfAsync.either remote.signIn () RecvSignIn Error
    | RecvSignIn username ->
        { model with signedInAs = username; signInFailed = Option.isNone username }, onSignIn username
    | SendSignOut ->
        model, Cmd.OfAsync.either remote.signOut () (fun () -> RecvSignOut) Error
    | RecvSignOut ->
        { model with signedInAs = None; signInFailed = false }, Cmd.none

    | Error RemoteUnauthorizedException ->
        { model with error = Some "You have been logged out."; signedInAs = None }, Cmd.none
    | Error exn ->
        { model with error = Some exn.Message }, Cmd.none
    | ClearError ->
        { model with error = None }, Cmd.none
    
    | ChangeTraitQual(traitName, value) ->
        { model with 
            activeTrait = Some traitName; traitQualitativeValue = Some value
            traitQuantitativeValue1 = None; traitQuantitativeValue2 = None }, Cmd.ofMsg SendTraitTag
    | ChangeTraitQuant(traitName, val1, val2) ->
        { model with 
            activeTrait = Some traitName; traitQualitativeValue = None
            traitQuantitativeValue1 = Some val1; traitQuantitativeValue2 = Some val2 }, Cmd.none
    | SendTraitTag ->
        match model.question with
        | None -> { model with error = Some "There isn't an active question" }, Cmd.none
        | Some q ->
            match q with
            | TagQuestion tq ->
                match model.activeTrait with
                | Some t ->
                    let req = {
                        GrainId = tq.GrainId
                        ImageId = tq.ImageId
                        Trait = t.ToString()
                        Value = model.traitQualitativeValue
                        Value1 = model.traitQuantitativeValue1
                        Value2 = model.traitQuantitativeValue2
                    }
                    model, Cmd.OfAsync.either remote.tagTrait req RecvTraitTag Error
                | None -> { model with error = Some "No trait was set" }, Cmd.none
    | RecvTraitTag(_) -> failwith "Not Implemented"


/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

type Main = Template<"wwwroot/main.html">

/// Display a button that contains an image
let imageButton src alt action =
    button {
        attr.``class`` "btn"
        on.click (fun _ -> action)
        img { 
            attr.src src
            attr.alt alt
        }
    }

/// View for question framing and associated inputs.
let traitQuestion (model:Model) dispatch =
    cond model.question <| function
    | Some q ->
        cond q <| function
        | TraitQuestion.DelineateQuestion delQ ->
            h2 { text "Are there pollen grains or spores in this image?" }
        | TraitQuestion.TagQuestion tagQ ->
            cond tagQ.Trait <| function
            | RequestedTrait.Pattern ->
                concat {
                    h2 { text "Can you see a surface pattern?" }
                    imageButton "images/buttons/pattern-patterned" "Patterned" (ChangeTraitQual (Pattern, "Patterned") |> dispatch)
                    imageButton "images/buttons/pattern-smooth" "Smooth" (ChangeTraitQual (Pattern, "Smooth") |> dispatch)
                    imageButton "images/buttons/pattern-unsure" "I'm not sure" (ChangeTraitQual (Pattern, "Unsure") |> dispatch)
                }
            | RequestedTrait.Shape ->
                concat {
                    h2 { text "What shape is the pollen or spore?" }
                    imageButton "images/buttons/shape-bisacchate" "Bisacchate" (ChangeTraitQual (Shape, "Bisacchate") |> dispatch)
                    imageButton "images/buttons/shape-circular" "Circular" (ChangeTraitQual (Shape, "Circular") |> dispatch)
                    imageButton "images/buttons/shape-ovular" "Ovular" (ChangeTraitQual (Shape, "Ovular") |> dispatch)
                    imageButton "images/buttons/shape-triangular" "Triangular" (ChangeTraitQual (Shape, "Triangular") |> dispatch)
                    imageButton "images/buttons/shape-trilobate" "Trilobate" (ChangeTraitQual (Shape, "Trilobate") |> dispatch)
                    imageButton "images/buttons/shape-pentagon" "Pentagon" (ChangeTraitQual (Shape, "Pentagon") |> dispatch)
                    imageButton "images/buttons/shape-hexagon" "Hexagon" (ChangeTraitQual (Shape, "Hexagon") |> dispatch)
                    imageButton "images/buttons/shape-unsure" "I'm not sure" (ChangeTraitQual (Shape, "Unsure") |> dispatch)
                }
            | RequestedTrait.Size -> concat {
                    h2 { text "How big is the pollen or spore?" }
                    p { text "On the image, make the cross shape fit over the longest and shotest diameters." }
                    button {
                        on.click (fun _ -> SendTraitTag )
                        text "I've finished"
                    }
                    button {
                        on.click (fun _ -> GetQuestion |> dispatch)
                        text "Skip this question"
                    }
                }
            | RequestedTrait.WallThickness -> concat {
                    h2 { text "Does it have a wall structure?" }
                }
            | RequestedTrait.Pores -> concat {
                    h2 { text "Does it have pores?" }
                }
    | None -> empty ()


/// Render the friendly taxon summary (common name, image, description).
let taxonCard model dispatch =
    empty ()

/// Render a javascript image viewer for manipulating trait values.
let imageViewer model dispatch =
    empty ()

let homePage model dispatch =
    Main.Home()
        .TaxonCard(taxonCard model dispatch)
        .TraitQuestion(traitQuestion model dispatch)
        .Viewer(imageViewer model dispatch)
        .Elt()

let helpPage model dispatch =
    Main.Help().Elt()

let menuItem (model: Model) (page: Page) (text: string) =
    Main.MenuItem()
        .Active(if model.page = page then "is-active" else "")
        .Url(router.Link page)
        .Text(text)
        .Elt()

let view model dispatch =
    Main()
        .Menu(concat {
            menuItem model Home "Home"
            menuItem model Help "Help"
        })
        .Body(
            cond model.page <| function
            | Home -> homePage model dispatch
            | Help -> helpPage model dispatch
        )
        .Error(
            cond model.error <| function
            | None -> empty()
            | Some err ->
                Main.ErrorNotification()
                    .Text(err)
                    .Hide(fun _ -> dispatch ClearError)
                    .Elt()
        )
        .Elt()

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let traitService = this.Remote<ITraitService>()
        let update = update traitService
        Program.mkProgram (fun _ -> initModel, Cmd.ofMsg GetSignedInAs) update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
