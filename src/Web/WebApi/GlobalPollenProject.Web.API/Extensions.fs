module GlobalPollenProject.Web.API.Extensions

open Microsoft.AspNetCore.Mvc

module ActionResult =
    let ofAsync (res: Async<IActionResult>) =
        res |> Async.StartAsTask    


type ControllerBase with

    member this.apiAction(request,action,core:Connections.CoreMicroservice) =
        ActionResult.ofAsync <| async {
        if not this.ModelState.IsValid
        then return BadRequestObjectResult(this.ModelState) :> IActionResult
        else
            let! result =
                request
                |> action
                |> core.Apply
            match result with
            | Ok r -> return JsonResult(r) :> IActionResult
            | Error e ->
                match e with
                | Core
                | InMaintenanceMode
                | Persistence -> return StatusCodeResult(500) :> IActionResult
                | NotFound -> return NotFoundResult() :> IActionResult
                | Validation _ -> return BadRequestResult() :> IActionResult
                | InvalidRequestFormat -> return BadRequestResult() :> IActionResult
        }
