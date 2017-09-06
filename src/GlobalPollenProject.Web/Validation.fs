
module ModelValidation

open Giraffe.Tasks
open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Giraffe.Middleware
open Giraffe.Razor.HttpHandlers
open Giraffe.Razor.Middleware

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Abstractions
open Microsoft.AspNetCore.Mvc.ModelBinding
open Microsoft.AspNetCore.Mvc.Razor
open Microsoft.AspNetCore.Mvc.Rendering
open Microsoft.AspNetCore.Mvc.ViewFeatures
open Microsoft.AspNetCore.Routing

open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Options

open System.Text
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives


let renderRazorViewWithState (razorViewEngine   : IRazorViewEngine)
                             (tempDataProvider  : ITempDataProvider)
                             (httpContext       : HttpContext)
                             (viewName          : string)
                             (modelState        : ModelStateDictionary)
                             (model             : 'T) =
    task {
        let actionContext    = ActionContext(httpContext, RouteData(), ActionDescriptor())
        let viewEngineResult = razorViewEngine.FindView(actionContext, viewName, true)

        match viewEngineResult.Success with
        | false ->
            let locations = String.Join(" ", viewEngineResult.SearchedLocations)
            return Error (sprintf "Could not find view with the name '%s'. Looked in %s." viewName locations)
        | true  ->
            let view = viewEngineResult.View
            let viewDataDict       = ViewDataDictionary<'T>(EmptyModelMetadataProvider(), modelState, Model = model)
            let tempDataDict       = TempDataDictionary(actionContext.HttpContext, tempDataProvider)
            let htmlHelperOptions  = HtmlHelperOptions()
            use output = new StringWriter()
            let viewContext = ViewContext(actionContext, view, viewDataDict, tempDataDict, output, htmlHelperOptions)
            do! view.RenderAsync(viewContext)
            return Ok (output.ToString())
    }

let razorViewWithModelState (contentType : string) (viewName : string) (modelState: ModelStateDictionary) (model : 'T) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let engine = ctx.RequestServices.GetService<IRazorViewEngine>()
            let tempDataProvider = ctx.RequestServices.GetService<ITempDataProvider>()
            let! result = renderRazorViewWithState engine tempDataProvider ctx viewName modelState model
            match result with
            | Error msg -> return (failwith msg)
            | Ok output ->
                let bytes = Encoding.UTF8.GetBytes output
                return! (setHttpHeader "Content-Type" contentType >=> setBody bytes) next ctx
        }

let razorHtmlViewWithModelState (viewName : string) modelState (model : 'T) =
    razorViewWithModelState "text/html" viewName modelState model

let validateModel' model =
    let context = ValidationContext(model)
    let validationResults = new List<ValidationResult>() 
    let isValid = Validator.TryValidateObject(model,context,validationResults,true)
    let dict = ModelStateDictionary()
    for error in validationResults do dict.AddModelError(error.MemberNames |> Seq.head, error.ErrorMessage)
    isValid, dict


////////////////////
/// HTTP Handlers
////////////////////

// let validateModel model =
//     let isValid,errors = validateModel' model
//     match isValid with
//     | false -> Error errors
//     | true -> Ok model

let validateModel model : Result<'a,ServiceError> =
    let isValid,errors = validateModel' model
    match isValid with
    | true -> Ok model
    | false ->
        let keys : string list = 
            errors.Keys 
            |> Seq.map (fun (k:string) -> k) 
            |> Seq.toList
        let values : (string list) list = 
            errors.Values 
            |> Seq.map (fun (v:ModelStateEntry) -> v.Errors |> Seq.map (fun y -> y.ErrorMessage ) |> Seq.toList) 
            |> Seq.toList
        keys
        |> List.zip values
        |> List.map (fun (v,k) -> { Property = k; Errors = v })
        |> Validation
        |> Error

let bindModel<'a> (ctx:HttpContext) =
    try
        let model = ctx.BindModel<'a>() |> Async.AwaitTask |> Async.RunSynchronously
        Some model
    with
        | _ -> None

// let bindAndValidate<'a> (failedHandler : HttpHandler) : HttpHandler =
//     fun next ctx ->
//         let model = bindModel<'a> ctx
//         match model with
//         | Some m ->
//             let isValid,errors = validateModel' model
//             match isValid with
//             | true -> async.Return (Some ctx)
//             | false -> failedHandler ctx
//         | None -> failedHandler ctx
