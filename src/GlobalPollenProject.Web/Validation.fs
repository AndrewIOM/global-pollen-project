
module ModelValidation

open Giraffe
open Microsoft.AspNetCore.Http
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Threading.Tasks

//////////////////
/// HTTP Handlers
//////////////////

let finish : HttpFunc = Some >> Task.FromResult

let isValid model =
    let context = ValidationContext(model)
    let validationResults = new List<ValidationResult>() 
    Validator.TryValidateObject(model,context,validationResults,true)

let requiresValidModel (error:'a->HttpHandler) model : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        match isValid model with
        | false -> (error model) finish ctx
        | true  -> next ctx


//////////////////
/// Error Retrival
//////////////////

let modelErrors model =
    let context = ValidationContext(model)
    let validationResults = new List<ValidationResult>() 
    let isValid = Validator.TryValidateObject(model,context,validationResults,true)
    let errors = 
        validationResults 
        |> Seq.map(fun e -> { Property = e.MemberNames |> Seq.head; Errors = [ e.ErrorMessage ] } )
        |> Seq.toList
    isValid, errors
