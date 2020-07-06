namespace GlobalPollenProject.Identity

open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Http

[<AllowNullLiteral>]
type ApplicationUser () =
    inherit IdentityUser ()
    member val Title = "" with get, set
    member val GivenNames = "" with get, set
    member val FamilyName = "" with get, set
    member val Organisation = "" with get, set

type AspNetUser(accessor:IHttpContextAccessor) =

    member __.Name = 
        accessor.HttpContext.User.Identity.Name

    member __.IsAuthenticated =
        accessor.HttpContext.User.Identity.IsAuthenticated

    member __.GetClaimsIdentity =
        accessor.HttpContext.User.Claims
