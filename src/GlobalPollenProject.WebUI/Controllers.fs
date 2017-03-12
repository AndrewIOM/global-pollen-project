namespace GlobalPollenProject.WebUI.Controllers

open System
open System.Collections.Generic
open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http

open GlobalPollenProject.App
open GlobalPollenProject.Shared.Identity.Models
open GlobalPollenProject.WebUI.ViewModels

type HomeController () =
    inherit Controller()

    member this.Index () =
        let model = GrainAppService.listUnknownGrains()
        this.View(model)

    member this.About () =
        this.ViewData.["Message"] <- "Your application description page."
        this.View()

    member this.Contact () =
        this.ViewData.["Message"] <- "Your contact page."
        this.View()

    member this.Error () =
        this.View()


type TaxonomyController () =
    inherit Controller ()

    member this.Index () = 
        let model = TaxonomyAppService.list()
        this.View(model)

    member this.Import () = this.View()

    [<HttpPost>]
    member this.Import (result:ImportTaxonViewModel) =
        if not this.ModelState.IsValid then this.View(result) :> IActionResult
        else
            TaxonomyAppService.import result.LatinName
            this.RedirectToAction "Index" :> IActionResult

type DigitiseController() =
    inherit Controller()

    [<HttpGet>]
    member this.Index () =
        this.View()

type CollectionController() =
    inherit Controller()

    [<HttpGet>]
    member this.Index () =
        this.View()

type AdminController() =
    inherit Controller()

    [<HttpGet>]
    member this.Events() =
        let model = GrainAppService.listEvents()
        this.View(model)


type GrainController () =
    inherit Controller()

    member this.Index () =
        let model = GrainAppService.listUnknownGrains()
        this.View(model)

    [<HttpGet>]
    member this.Identify (id) =
        let model = GrainAppService.listUnknownGrains().FirstOrDefault(fun m -> m.Id = id)
        //if model then this.BadRequest() :> IActionResult
        //else 
        this.View model :> IActionResult

    [<HttpPost>]
    member this.Identify (id,taxonId) =
        GrainAppService.identifyUnknownGrain id (Guid.NewGuid())
        this.RedirectToAction "Index"

    [<HttpGet>]
    [<Authorize>]
    member this.Add () =
        this.View ()

    [<HttpPost>]
    [<Authorize>]
    member this.Add (model:AddGrainViewModel) =
        if not this.ModelState.IsValid then this.View(model) :> IActionResult
        else
            let id = Guid.NewGuid()
            GrainAppService.submitUnknownGrain id (model.Images |> Array.toList) model.Age model.Latitude model.Longitude
            this.RedirectToAction "Index" :> IActionResult


[<Authorize>]
type AccountController(userManager: UserManager<ApplicationUser>, signInManager: SignInManager<ApplicationUser>, loggerFactory: ILoggerFactory) =
    inherit Controller()

    member private this.Logger = loggerFactory.CreateLogger<AccountController>()

    [<HttpGet>]
    [<AllowAnonymous>]
    member this.Login () =
        this.View()

    [<HttpPost>]
    [<AllowAnonymous>]
    member this.Login(model: LoginViewModel) =
        if this.ModelState.IsValid then
            let result = signInManager.PasswordSignInAsync(model.Email, model.Password, false, lockoutOnFailure = false) |> Async.AwaitTask |> Async.RunSynchronously
            if result.Succeeded then
                this.Logger.LogInformation "User logged in."
                this.RedirectToAction("Index","Home") :> IActionResult // Should be redirecttolocal
            else
                this.ModelState.AddModelError("","Invalid login attempt")
                this.View(model) :> IActionResult
        else
            this.View(model) :> IActionResult

    [<HttpGet>]
    [<AllowAnonymous>]
    member this.Register () =
        this.View()

    [<HttpPost>]
    [<AllowAnonymous>]
    member this.Register(model: RegisterViewModel) =
        if this.ModelState.IsValid then
            let user = ApplicationUser(UserName = model.Email, Email = model.Email)
            let result = userManager.CreateAsync(user, model.Password) |> Async.AwaitTask |> Async.RunSynchronously
            if result.Succeeded then
                let id = Guid.Parse user.Id
                UserAppService.register id model.Title model.FirstName model.LastName
                signInManager.SignInAsync(user, isPersistent = false) |> Async.AwaitTask |> Async.RunSynchronously
                this.Logger.LogInformation "User created a new account with password."
                this.RedirectToAction("Index","Home") :> IActionResult // Should be redirecttolocal
            else 
                //this.ModelState.AddErrors result
                this.View(model) :> IActionResult
        else this.View(model) :> IActionResult

    [<HttpPost>]
    member this.LogOff() =
        signInManager.SignOutAsync() |> Async.AwaitTask |> Async.RunSynchronously |> ignore
        ObjectResult(true)

    [<HttpGet>]
    [<AllowAnonymous>]
    member this.ExternalLoginLogOff(provider:string, returnUrl: string option) =
        signInManager.SignOutAsync() |> Async.AwaitTask |> Async.RunSynchronously |> ignore
        this.Logger.LogInformation "User logged out"
        this.RedirectToAction("Index", "Home")

    [<HttpPost>]
    [<AllowAnonymous>]
    [<ValidateAntiForgeryToken>]
    member this.ExternalLogin(provider:string,returnUrl:string option) =
        let redirectUrl = this.Url.Action("ExternalLoginCallback", "Account")
        let properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl)
        this.Challenge(properties, provider)

    // [<HttpGet>]
    // [<AllowAnonymous>]
    // member this.ExternalLoginCallback(returnUrl:string option, remoteError:string option) =
    //     match remoteError with
    //     | Some error ->
    //         this.ModelState.AddModelError("", "Error from external provider: {remoteError}")
    //         this.View("Login")
    //     | None ->
    //         let info = signInManager.GetExternalLoginInfoAsync() |> Async.AwaitTask |> Async.RunSynchronously
    //         if info = null then this.RedirectToAction "Login" :> IActionResult
    //         else
    //             let result = signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent = false) |> Async.AwaitTask |> Async.RunSynchronously
    //             if result.Succeeded then
    //                 this.Logger.LogInformation ("User logged in with {Name} provider.", info.LoginProvider)
    //                 this.RedirectToAction("Index","Home") :> IActionResult
    //             else
    //                 this.ViewData.Add("ReturnUrl", returnUrl)
    //                 this.ViewData.Add("LoginProvider", info.LoginProvider)
    //                 let email = info.Principal.FindFirstValue(ClaimTypes.Email)
    //                 this.View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email }) :> IActionResult

    [<HttpGet>]
    [<AllowAnonymous>]
    member this.ForgotPassword () =
        this.View()

    // [<HttpGet>]
    // [<AllowAnonymous>]
    // member this.ForgotPassword(model:ForgotPasswordViewModel) =
    //     if this.ModelState.IsValid then
    //         let user = userManager.FindByNameAsync(model.Email) |> Async.AwaitTask |> Async.RunSynchronously
    //         if user = null || userManager.IsEmailConfirmedAsync(user) |> Async.AwaitTask |> Async.RunSynchronously then
    //             this.View("ForgotPasswordConfirmation")
    //     else this.View(model)


type ManageController () =
    inherit Controller ()

    member this.Index () =
        this.View()