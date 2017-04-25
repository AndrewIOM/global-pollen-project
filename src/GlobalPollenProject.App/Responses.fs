[<AutoOpen>]
module Responses

type PageRequest = { Page: int; PageSize: int }
type PagedResult<'TProjection> = {
    Items: 'TProjection list
    CurrentPage: int
    TotalPages: int
    ItemsPerPage: int
    ItemTotal: int
}