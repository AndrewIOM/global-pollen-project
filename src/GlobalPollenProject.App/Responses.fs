[<AutoOpen>]
module Responses

type ValidationError = {
    Property: string
    Errors: string list
}

type ServiceError =
| Core
| Validation of ValidationError list
| Persistence
| NotFound

type PageRequest = { Page: int; PageSize: int }
type PagedResult<'TProjection> = {
    Items: 'TProjection list
    CurrentPage: int
    TotalPages: int
    ItemsPerPage: int
    ItemTotal: int
}