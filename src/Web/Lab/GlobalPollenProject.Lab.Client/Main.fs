module GlobalPollenProject.Lab.Client.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open ReadModels
open GlobalPollenProject.Lab.Client.Types

let update remote message model =
    let onSignIn = function
        | Some _ -> Cmd.ofMsg GetCollections
        | None -> Cmd.none
    match message with
    | SetPage page ->
        { model with page = page }, Cmd.none

    | Increment ->
        { model with counter = model.counter + 1 }, Cmd.none
    | Decrement ->
        { model with counter = model.counter - 1 }, Cmd.none
    | SetCounter value ->
        { model with counter = value }, Cmd.none

    | GetCollections ->
        let cmd = Cmd.ofAsync remote.getCollections () GotCollections Error
        { model with collections = None }, cmd
    | GotCollections collections ->
        { model with collections = Some collections }, Cmd.none

    | ChangeNewCollection col ->
        { model with draft = Some <| DraftCollection col }, Cmd.none
    | SendStartCollection ->
        match model.draft with
        | Some draft ->
            match draft with
            | DraftCollection col -> 
                let cmd = Cmd.ofAsync remote.startCollection col RecvStartCollection Error
                model, cmd
            | _ -> model, Cmd.none
        | None -> model, Cmd.none
    | RecvStartCollection id ->
        model, Cmd.none
    
    | SetUsername s ->
        { model with username = s }, Cmd.none
    | SetPassword s ->
        { model with password = s }, Cmd.none
    | GetSignedInAs ->
        model, Cmd.ofAuthorized remote.getUsername () RecvSignedInAs Error
    | RecvSignedInAs username ->
        { model with signedInAs = username }, onSignIn username
    | SendSignIn ->
        model, Cmd.ofAsync remote.signIn (model.username, model.password) RecvSignIn Error
    | RecvSignIn username ->
        { model with signedInAs = username; signInFailed = Option.isNone username }, onSignIn username
    | SendSignOut ->
        model, Cmd.ofAsync remote.signOut () (fun () -> RecvSignOut) Error
    | RecvSignOut ->
        { model with signedInAs = None; signInFailed = false }, Cmd.none

    | Error RemoteUnauthorizedException ->
        { model with error = Some "You have been logged out."; signedInAs = None }, Cmd.none
    | Error exn ->
        { model with error = Some exn.Message }, Cmd.none
    | ClearError ->
        { model with error = None }, Cmd.none

/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

type Main = Template<"wwwroot/main.html">

let homePage model dispatch =
    Main.Home().Elt()

let counterPage model dispatch =
    Main.Counter()
        .Decrement(fun _ -> dispatch Decrement)
        .Increment(fun _ -> dispatch Increment)
        .Value(model.counter, fun v -> dispatch (SetCounter v))
        .Elt()

let dataPage model (username: string) dispatch =
    Main.Data()
        .Reload(fun _ -> dispatch GetCollections)
        .Username(username)
        .SignOut(fun _ -> dispatch SendSignOut)
        .StartNewCol(fun _ -> dispatch <| SetPage StartCollection)
        .Rows(cond model.collections <| function
            | None ->
                Main.EmptyData().Elt()
            | Some collections ->
                forEach collections <| fun collection ->
                    tr [] [
                        td [] [text collection.Name]
                        td [] [text collection.Institution]
                        td [] [text (collection.LastEdited.ToString("yyyy-MM-dd"))]
                        td [] [text <| collection.SlideCount.ToString()]
                        td [] [button [ on.click (fun _ -> SetPage (collection.Id.ToString() |> ViewCollection) |> dispatch) ] [ text "Continue editing"]]
                    ])
        .Elt()

let signInPage model dispatch =
    Main.SignIn()
        .Username(model.username, fun s -> dispatch (SetUsername s))
        .Password(model.password, fun s -> dispatch (SetPassword s))
        .SignIn(fun _ -> dispatch SendSignIn)
        .ErrorNotification(
            cond model.signInFailed <| function
            | false -> empty
            | true ->
                Main.ErrorNotification()
                    .HideClass("is-hidden")
                    .Text("Sign in failed. Use any username and the password \"password\".")
                    .Elt()
        )
        .Elt()

let menuItem (model: Model) (page: Page) (text: string) =
    Main.MenuItem()
        .Active(if model.page = page then "is-active" else "")
        .Url(router.Link page)
        .Text(text)
        .Elt()

let view model dispatch =
    Main()
        .Menu(concat [
            menuItem model Home "Home"
            menuItem model Counter "Counter"
            menuItem model Collections "My Collections"
        ])
        .Body(
            cond model.page <| function
            | Home -> homePage model dispatch
            | Counter -> counterPage model dispatch
            | Collections ->
                cond model.signedInAs <| function
                | Some username -> dataPage model username dispatch
                | None -> signInPage model dispatch
            | StartCollection -> Views.Partials.addCollectionModal model.draft dispatch
            | AddSlideForm -> Views.Partials.recordSlide model.draft dispatch
            | ViewCollection id ->
                cond model.collections <| function
                | Some cols ->
                    match cols |> Seq.tryFind(fun c -> c.Id = Guid(id)) with
                    | Some c -> Views.activeCollection c dispatch
                    | None -> text "Not found"
                | None -> Main().Elt()
            | SlideDetailView (colId, slideId) ->
                cond model.collections <| function
                | Some cols ->
                    match cols |> Seq.tryFind(fun c -> c.Id = Guid(colId)) with
                    | Some c ->
                        match c.Slides |> Seq.tryFind(fun c -> c.CollectionSlideId = slideId) with
                        | Some slide -> Views.Partials.slideTabbedModal slide model dispatch
                        | None -> text "Not found"
                    | None -> text "Not found"
                | None -> Main().Elt()
            )
        .Error(
            cond model.error <| function
            | None -> empty
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
        let digitiseService = this.Remote<DigitiseService>()
        let update = update digitiseService
        Program.mkProgram (fun _ -> initModel, Cmd.ofMsg GetSignedInAs) update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
