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
let notFound = "/NotFound"

module MasterReference =
    let root = "/Taxon"
    let rootBy rank letter = root + "?rank=" + rank + "&lex=" + letter
    let family family = root + "/" + family
    let genus family genus = root + "/" + family + "/" + genus
    let species family genus species = root + "/" + family + "/" + genus + "/" + species
    let taxonById (id:System.Guid) = sprintf "%s/ID/%s" root (id.ToString())

module Account =
    let root = "/Account"
    let login = root + "/Login"
    let register = root + "/Register"
    let logout = root + "/Logout"
    let profile = root + "/Profile"

module Collections =

    let root = "/Reference"
    let byId (id:System.Guid) = root + "/" + (id.ToString())

module Identify =

    let root = "/Identify"
    let identify = root + "/Identify"
    
module Admin =
    
    let root = "/Admin"
    let rebuildReadModel = "/RebuildReadModel"
    let users = "/Users"