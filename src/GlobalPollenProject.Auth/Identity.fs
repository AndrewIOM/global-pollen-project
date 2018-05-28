namespace GlobalPollenProject.Auth

open Microsoft.AspNetCore.Authorization
open System.Threading.Tasks

type ClaimsRequirement(claimName:string, claimValue:string)  =
    interface IAuthorizationRequirement
    with
        let mutable name = claimName
        let mutable value = claimValue

        member __.ClaimName with get() = name and set(v) = name <- v
        member __.ClaimValue with get() = value and set(v) = value <- v


type ClaimsRequirementHandler() =
    inherit AuthorizationHandler<ClaimsRequirement>()

    override __.HandleRequirementAsync(context:AuthorizationHandlerContext, requirement:ClaimsRequirement) =
        let claim = context.User.Claims |> Seq.tryFind(fun c -> c.Type = requirement.ClaimName)
        match claim with
        | Some c ->
            if c.Value.Contains(requirement.ClaimValue)
            then context.Succeed(requirement)        
        | None -> ()
        Task.CompletedTask