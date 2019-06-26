module Account

open System
open System.Text
open Microsoft.Extensions.Logging
open Giraffe
open GlobalPollenProject.Web
open ModelValidation
open ReadModels
open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.WebUtilities
open System.Security.Principal
open System.Security.Claims
open System.Net.Http

open Connections
open Urls
open Handlers

type IdentityParser() =

    member __.Parse(principal:IPrincipal) = 
        match principal with
        | :? ClaimsPrincipal as claims ->
            { Firstname =  claims.Claims |> Seq.tryFind(fun x -> x.Type = "name")  |> Option.bind(fun x -> Some x.Value)
              Lastname = claims.Claims |> Seq.tryFind(fun x -> x.Type = "lastname") |> Option.bind(fun x -> Some x.Value) }
        | _ -> invalidOp "The principal was not a claims principal"

module Identity =

    let identityToValidationError' (e:IdentityError) =
        {Property = e.Code; Errors = [e.Description] }

    let identityToValidationError (errs:IdentityError seq) =
        errs
        |> Seq.map(fun e -> {Property = e.Code; Errors = [e.Description] })
        |> Seq.toList

    let challengeWithProperties (authScheme : string) properties _ (ctx : HttpContext) =
        task {
            do! ctx.ChallengeAsync(authScheme,properties)
            return Some ctx }

    let login onError loginRequest : HttpHandler = 
        fun next ctx ->
            task {
                let authService = ctx.GetService<AuthenticationService>()
                let! result = authService.Login loginRequest
                match result with
                | Ok _ ->
                   let logger = ctx.GetLogger()
                   logger.LogInformation "User logged in."
                   return! next ctx
                | Error e -> return! (onError [] loginRequest) finish ctx
            }

    let register onError (model:NewAppUserRequest) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let authService = ctx.GetService<AuthenticationService>()
                let! result = authService.Register model
                match result with
                | Ok s -> return! next ctx
                | Error e -> return! (onError e model) next ctx
            }

///////////////////
/// HTTP Handlers
///////////////////

let bindAndValidate<'T> procedure (onError:ValidationError list -> 'T -> HttpHandler) onComplete : HttpHandler =
    fun next ctx ->
        bindForm<'T> None (fun m -> 
            requiresValidModel (onError []) m
            >=> procedure onError m
            >=> onComplete
        ) next ctx

let htmlViewWithModel view errors vm = view errors vm |> htmlView

// let grantCurationHandler (id:string) : HttpHandler =
//     fun next ctx ->
//         task {
//             let userManager = ctx.GetService<UserManager<ApplicationUser>>()
//             match User.grantCuration id with
//             | Ok _ ->
//                 let! existing = userManager.FindByIdAsync id
//                 if existing |> isNull 
//                     then return! htmlView HtmlViews.StatusPages.error next ctx
//                     else
//                         let! _ = userManager.AddToRoleAsync(existing, "Curator")
//                         return! redirectTo false "/Admin/Users" next ctx
//             | Error _ -> return! htmlView HtmlViews.StatusPages.error next ctx
//         }

module Manage =

    open System.ComponentModel.DataAnnotations

    [<CLIMutable>]
    type ChangePublicProfileViewModel = {
        [<Required>] [<Display(Name="Title")>] Title: string
        [<Required>] [<Display(Name="Forename(s)")>] FirstName: string
        [<Required>] [<Display(Name="Surname")>] LastName: string
        [<Required>] [<Display(Name="Organisation")>] Organisation: string
    }

    let profile : HttpHandler =
        fun next ctx ->
            ctx.BindFormAsync<ChangePublicProfileViewModel>() |> Async.AwaitTask |> Async.RunSynchronously
            |> ignore
            invalidOp "Not implemented"
