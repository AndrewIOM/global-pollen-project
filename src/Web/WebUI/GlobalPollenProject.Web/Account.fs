namespace GlobalPollenProject.Web

open Giraffe

module PublicProfile =

    open System.ComponentModel.DataAnnotations

    [<CLIMutable>]
    type ChangePublicProfileViewModel = {
        [<Required>] [<Display(Name="Title")>] Title: string
        [<Required>] [<Display(Name="Forename(s)")>] FirstName: string
        [<Required>] [<Display(Name="Surname")>] LastName: string
        [<Required>] [<Display(Name="Organisation")>] Organisation: string
    }

    let profile : HttpHandler =
        fun _ ctx ->
            task {
                let! model = ctx.BindFormAsync<ChangePublicProfileViewModel>()
                return invalidOp "Not implemented"
            }