[<AutoOpen>]
module Responses

open ReadModels

type ValidationError = {
    Property: string
    Errors: string list
}

type ServiceError =
| Core
| InvalidRequestFormat
| Validation of ValidationError list
| Persistence
| InMaintenanceMode
| NotFound

type PagedResult<'TProjection> = {
    Items: 'TProjection list
    CurrentPage: int
    TotalPages: int
    ItemsPerPage: int
    ItemTotal: int
}

type SuccessResult<'a> = {
    Message: string
    Data: 'a
}

type FailResult = {
    Message: string
    Errors: ValidationError list
}

type HomeStatsViewModel = {
    DigitisedSlides: int
    Species: int
    IndividualGrains: int
    UnidentifiedGrains: int
}

type GrainPositionViewModel = {
    Latitude: float
    Longitude: float
    Id: System.Guid
}

type Percent = {
    Count: int
    Total: int }

type LeaderboardItem = {
    Name: string
    Score: string }

type AllStatsViewModel = {
    Family: Percent
    Genus: Percent
    Species: Percent
    TopIndividuals: LeaderboardItem list
    TopOrganisations: LeaderboardItem list }

type SlidePageViewModel = {
    Slide: SlideDetail
    Collection: ReferenceCollectionSummary
}