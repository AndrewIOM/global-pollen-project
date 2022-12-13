namespace GlobalPollenProject.Traits.Server

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Hosting
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open GlobalPollenProject.Traits
open Microsoft.AspNetCore.Authentication

type TraitService(ctx: IRemoteContext, env: IWebHostEnvironment, core:Connections.CoreMicroservice) =
    inherit RemoteHandler<Client.Main.TraitService>()

    override this.Handler =
        {
            getNextQuestion = fun () -> async {
                return Error Core }

            delineate = fun request -> async {
                return Error Core }
            
            tagTrait = fun request -> async {
                return Error Core }

            signIn = fun () -> async {
                do! ctx.HttpContext.ChallengeAsync() |> Async.AwaitTask
                return Some username
            }
            
            signOut = fun () -> async {
                return! ctx.HttpContext.AsyncSignOut()
            }

            getUsername = ctx.Authorize <| fun () -> async {
                return ctx.HttpContext.User.Identity.Name
            }
        }
