module GlobalPollenProject.Web.Urls

open Microsoft.AspNetCore.Http

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
    let family family = root + "/" + family
    let genus family genus = root + "/" + family + "/" + genus

module Account =
    let root = "/Account"
    let login = root + "/Login"
    let register = root + "/Register"
    let logout = root + "/Logout"
    let profile = root + "/Profile"

module Collections =

    let root = "/Reference"

module Identify =

    let root = "/Identify"
    let identify = root + "/Identify"
    
module Admin =
    
    let root = "/Admin"
    let rebuildReadModel = "/RebuildReadModel"
    let users = "/Users"