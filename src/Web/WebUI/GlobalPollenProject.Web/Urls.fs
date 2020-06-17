module GlobalPollenProject.Web.Urls

open Microsoft.AspNetCore.Http

[<CLIMutable>]
type ApplicationUser = {
    Firstname: string option
    Lastname: string option
}

let getBaseUrl (ctx:HttpContext) = 
    let port = 
        if ctx.Request.Host.Port.HasValue 
        then sprintf ":%i" ctx.Request.Host.Port.Value
        else ""
    sprintf "%s://%s%s" ctx.Request.Scheme ctx.Request.Host.Host port

let home = "/"
let guide = "/Guide"
let referenceCollection = "/Taxon"
let identify = "/Identify"
let individualCollections = "/Reference"
let statistics = "/Statistics"
let digitise = "/Digitise"
let api = "/Guide/API"
let tools = "/Tools"
let cite = "/Cite"
let terms = "/Terms"

module MasterReference =
    let root = "/Taxon"


module Account =
    let root = "/Account"
    let login = root + "/Login"
    let register = root + "/Register"
    let externalLogin = root + "/ExternalLogin"
    let externalLoginFailure = root + "/ExternalLoginFailure"
    let externalLoginConf = root + "/ExternalLoginConfirmation"
    let logout = root + "/Logout"
    let forgotPassword = root + "/ForgotPassword"
    let resetPassword = root + "/ResetPassword"
    let resetPasswordConf = root + "/ResetPasswordConfirmation"
    let confirmEmail = root + "/ConfirmEmail"
    let externalLoginCallbk = root + "/ExternalLoginCallback"

module Collections =

    let root = "/Reference"

module Identify =

    let root = "/Identify"
    
module Admin =
    
    let root = "/Admin"
    let rebuildReadModel = "/RebuildReadModel"
    let users = "/Users"