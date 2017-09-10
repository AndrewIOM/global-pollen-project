module SerialisationTests

open System
open Serialisation
open Xunit
open GlobalPollenProject.Core.Composition

module ``When serialising a domain event`` =

    open GlobalPollenProject.Core.DomainTypes
    open GlobalPollenProject.Core.Aggregates.ReferenceCollection

    [<Fact>]
    let ``A complex event is serialised and deserialised correctly`` () =
        let testEvent = 
            SlideRecorded {
                Id                  = SlideId (CollectionId (Guid.NewGuid()), "Cool")
                OriginalFamily      = "Oleaceae"
                OriginalGenus       = "Fraxinus"
                OriginalSpecies     = "Fraxinus excelsior"
                OriginalAuthor      = "L."
                Place               = Site (Latitude 27.<DD>, Longitude 23.<DD>) |> Some
                Time                = Age.CollectionDate 1987<CalYr> |> Some
                PrepMethod          = Acetolysis |> Some
                PrepDate            = 2002<CalYr> |> Some
                Mounting            = SiliconeOil |> Some
                Taxon               = Botanical (TaxonId (Guid.NewGuid()), IdentificationSource.Unknown, Person (["A"], "Bill")) }
        
        let eventName,json = testEvent |> Serialisation.serialiseEventToBytes
        let deserialised = Serialisation.deserialiseEventFromBytes<Event> eventName json
        Assert.Equal(testEvent, deserialised)


module ``When serialising read models`` =

    open ReadModels

    [<Fact>]
    let ``A single string is serialised and deserialised correctly`` () =
        let sut = "I'm a test string read model"
        let result =
            sut
            |> Serialisation.serialise
            |> bind Serialisation.deserialise
        match result with
        | Error e -> Assert.True(false, "Deserialisation failed")
        | Ok r -> Assert.Equal(sut,r)

    [<Fact>]
    let ``An int is serialised and deserialised correctly`` () =
        let sut = 23
        let result =
            sut
            |> Serialisation.serialise
            |> bind Serialisation.deserialise
        match result with
        | Error e -> Assert.True(false, "Deserialisation failed")
        | Ok r -> Assert.Equal(sut,r)

    [<Fact>]
     let ``A complex read model is serialised and deserialised correctly`` () =
        let sut = {
            Id          = Guid.NewGuid()
            Family      = "Some family"
            Genus       = "Some genus"
            Species     = "Some species"
            LatinName   = "Some latin name"
            Authorship  = "L."
            Rank        = "Species"
            Parent      = Some { Id = Guid.NewGuid(); Name = "Fraxinus"; Rank = "Genus"}
            Children    = [ { Id = Guid.NewGuid(); Name = "Fraxinus excelsior b."; Rank = "Subspecies"} ]
            Slides      = [ { ColId = Guid.NewGuid(); SlideId = "GPP2"; LatinName = "Test"; Rank = "Genus"; Thumbnail = "http://test.test/test.test"} ]
            Grains      = []
            ReferenceName = "Test et al., Some test manuscript. In Testy test: Trees of Europe"
            ReferenceUrl = "https://sometest.test/test?r89y89cbq"
            NeotomaId   = 0
            GbifId      = 2841145
            BackboneChildren = 2 }
        let result =
            sut
            |> Serialisation.serialise
            |> bind Serialisation.deserialise
        match result with
        | Error e -> Assert.True(false, "Deserialisation failed")
        | Ok r -> Assert.Equal(sut,r)
