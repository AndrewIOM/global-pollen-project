namespace GlobalPollenProject.Identity

open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Http

[<AllowNullLiteral>]
type ApplicationUser () =
    inherit IdentityUser ()

type AspNetUser(accessor:IHttpContextAccessor) =

    member __.Name = 
        accessor.HttpContext.User.Identity.Name

    member __.IsAuthenticated =
        accessor.HttpContext.User.Identity.IsAuthenticated

    member __.GetClaimsIdentity =
        accessor.HttpContext.User.Claims
