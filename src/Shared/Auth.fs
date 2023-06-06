namespace GlobalPollenProject.Shared

open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open System.Threading
open System.Net.Http
open System.Net.Http.Headers
open System.Threading.Tasks

module Auth =

    type HttpClientAuthorizationDelegatingHandler(httpContextAccessor:IHttpContextAccessor) =
        inherit DelegatingHandler()

        member __.GetToken () : Task<string> =
            let accessToken = "access_token"
            httpContextAccessor.HttpContext.GetTokenAsync(accessToken)

        override this.SendAsync (request:HttpRequestMessage, cancellationToken:CancellationToken) = 
            let authorisationHeader = httpContextAccessor.HttpContext.Request.Headers.["Authorization"]
            if authorisationHeader.Count > 0 then request.Headers.Add("Authorization", authorisationHeader)
            let token = this.GetToken().Result // TODO Don't call 'Result' here
            if not (isNull token) then request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
            base.SendAsync(request, cancellationToken)

    type HttpClientRequestIdDelegatingHandler() =
        inherit DelegatingHandler()

        override __.SendAsync (request:HttpRequestMessage, cancellationToken:CancellationToken) = 
            if request.Method = HttpMethod.Post || request.Method = HttpMethod.Put
            then 
                if request.Headers.Contains "x-requestid" |> not
                then request.Headers.Add("x-requestid", System.Guid.NewGuid().ToString())
            base.SendAsync(request, cancellationToken)
