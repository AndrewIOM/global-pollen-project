namespace GlobalPollenProject.Lab.Server

open System
open Connections
open Microsoft.AspNetCore.Hosting
open Bolero.Remoting
open Bolero.Remoting.Server
open GlobalPollenProject.Lab
open Microsoft.AspNetCore.Authentication

type DigitiseService(ctx: IRemoteContext, env: IWebHostEnvironment, core:Connections.CoreMicroservice) =
    inherit RemoteHandler<Client.Types.DigitiseService>()
        
    let asDigitiseResult result =
        match result with
        | Ok r -> r
        | Error e -> failwith "TODO error"
        
    override this.Handler =
        {
            getCollections = ctx.Authorize <| fun () -> async {
                let! result = CoreActions.Digitise.myCollections() |> core.Apply
                return result |> asDigitiseResult
            }
            
            getCollection = ctx.Authorize <| fun id -> async {
                let! result = id |> CoreActions.Digitise.getCollection |> core.Apply
                return result |> asDigitiseResult
            }

            startCollection = ctx.Authorize <| fun start -> async {
                return! start |> CoreActions.Digitise.startCollection |> core.Apply
            }

            publishCollection = ctx.Authorize <| fun id -> async {
                return! id |> CoreActions.Digitise.publishCollection |> core.Apply
            }
            
            addSlideRecord = ctx.Authorize <| fun slide -> async {
                return! slide |> CoreActions.Digitise.recordSlide |> core.Apply
            }
            
            voidSlide = ctx.Authorize <| fun voidRequest -> async {
                return! voidRequest |> CoreActions.Digitise.voidSlide |> core.Apply
            }
            
            addImageToSlide = ctx.Authorize <| fun img -> async {
                return! img |> CoreActions.Digitise.uploadImage |> core.Apply
            }
                        
            getCalibrations = ctx.Authorize <| fun () -> async {
                return! CoreActions.User.myCalibrations () |> core.Apply
            }

            setupMicroscope = ctx.Authorize <| fun microscope -> async {
                return! microscope |> CoreActions.User.setupMicroscope |> core.Apply
            }
            
            setupMagnification = ctx.Authorize <| fun calibration -> async {
                return! calibration |> CoreActions.User.calibrateMicroscope |> core.Apply
            }
            
            signIn = fun (username, password) -> async {
                do! ctx.HttpContext.ChallengeAsync() |> Async.AwaitTask
                //do! ctx.HttpContext.AsyncSignIn(username, TimeSpan.FromDays(365.))
                return Some username
            }
            
            signOut = fun () -> async {
                return! ctx.HttpContext.AsyncSignOut()
            }

            getUsername = ctx.Authorize <| fun () -> async {
                return ctx.HttpContext.User.Identity.Name
            }
        }
