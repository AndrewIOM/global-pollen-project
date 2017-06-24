[<AutoOpen>]
module GlobalPollenProject.Core.Composition

let succeed x = 
    Ok x

let fail x = 
    Error x

let either successFunc failureFunc twoTrackInput =
    match twoTrackInput with
    | Ok s -> successFunc s
    | Error f -> failureFunc f

let bind f = 
    either f fail

let (>>=) x f = 
    bind f x

let switch f = 
    f >> succeed

let map f = 
    either (f >> succeed) fail

let tee f x = 
    f x; x 

let tryCatch f exnHandler x =
    try
        f x |> succeed
    with
    | ex -> exnHandler ex |> fail

let doubleMap successFunc failureFunc =
    either (successFunc >> succeed) (failureFunc >> fail)

let plus addSuccess addFailure switch1 switch2 x = 
    match (switch1 x),(switch2 x) with
    | Ok s1,Ok s2 -> Ok (addSuccess s1 s2)
    | Error f1,Ok _  -> Error f1
    | Ok _ ,Error f2 -> Error f2
    | Error f1,Error f2 -> Error (addFailure f1 f2)


let toErrorList (result:Result<'a,'b>) : Result<'a,'b list> =
    match result with
    | Error e -> Error [e]
    | Ok c -> Ok c

let apply f result =
    match f,result with
    | Ok f, Ok x -> 
        f x |> Ok 
    | Error e, Ok _ 
    | Ok _, Error e -> 
        e |> Error
    | Error e1, Error e2 -> 
        e1 |> Error 

let lift f result =
    let f' =  f |> succeed
    apply f' result

let (<*>) = apply
let (<!>) = lift