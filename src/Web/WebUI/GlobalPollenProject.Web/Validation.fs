
module Validation

open Giraffe
open Microsoft.AspNetCore.Http
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.ModelBinding

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

let validateModel' model =
    let context = ValidationContext(model)
    let validationResults = new List<ValidationResult>() 
    let isValid = Validator.TryValidateObject(model,context,validationResults,true)
    let dict = ModelStateDictionary()
    for error in validationResults do dict.AddModelError(error.MemberNames |> Seq.head, error.ErrorMessage)
    isValid, dict

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