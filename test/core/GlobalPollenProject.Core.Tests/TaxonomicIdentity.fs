module TaxonomicIdentityTests

open System
open Xunit
open GlobalPollenProject.Core.DomainTypes    
open GlobalPollenProject.Core.Dependencies

let unwrap (TaxonId t) = t

let commonFamily = Guid.NewGuid()
let commonGenus = Guid.NewGuid()
let testBackboneData = [
    [ Guid.NewGuid(); Guid.NewGuid(); Guid.NewGuid() ]
    [ Guid.NewGuid(); Guid.NewGuid(); Guid.NewGuid() ]
    [ commonFamily; Guid.NewGuid(); Guid.NewGuid() ]
    [ commonFamily; Guid.NewGuid(); Guid.NewGuid() ]
    [ commonFamily; Guid.NewGuid(); Guid.NewGuid() ]
    [ commonFamily; commonGenus; Guid.NewGuid() ]
    [ commonFamily; commonGenus; Guid.NewGuid() ]
    [ commonFamily; commonGenus; Guid.NewGuid() ] ]

let testBackboneUpper taxonId : Result<TaxonId option,string> = 
    let taxonId = unwrap taxonId
    match testBackboneData |> Seq.tryFind (fun t -> t |> Seq.contains taxonId) with
    | Some t ->
        match t |> Seq.findIndex (fun i -> i = taxonId) with
        | 0 -> Ok None
        | 1 -> Ok (Some (TaxonId t.[0]))
        | 2 -> Ok (Some (TaxonId t.[1]))
        | _ -> Error "error"
    | None -> Error "not found"

let testPerson = Person (["Test"],"McTest")

module ``When calculating a taxonomic identity`` =

    [<Fact>]
    let ``no confirmed identity when no identifications`` () =
        Assert.Equal(Ok None, calculateTaxonomicIdentity testBackboneUpper [])

    [<Fact>]
    let ``a single botanical identification leads to a confirmed identity`` () =
        let testId = TaxonId testBackboneData.[1].[2]
        let result = calculateTaxonomicIdentity testBackboneUpper [
            Botanical (testId, Unknown, testPerson) ]
        Assert.Equal(Ok (Some testId), result)

    [<Fact>]
    let ``three of the same morphological ids leads to a confirmed identity`` () =
        let testId = TaxonId testBackboneData.[1].[2]
        let result = calculateTaxonomicIdentity testBackboneUpper [
            Morphological (testId, ExternalPerson testPerson)
            Morphological (testId, ExternalPerson testPerson)
            Morphological (testId, ExternalPerson testPerson) ]
        Assert.Equal(Ok (Some testId), result)

    [<Fact>]
    let ``reduction below 70% agreement (morphological ids) leads to no confirmed identity`` () =
        let testId = TaxonId testBackboneData.[1].[2]
        let opposingId = TaxonId testBackboneData.[0].[2]
        let result = calculateTaxonomicIdentity testBackboneUpper [
            Morphological (testId, ExternalPerson testPerson)
            Morphological (testId, ExternalPerson testPerson)
            Morphological (testId, ExternalPerson testPerson)
            Morphological (opposingId, ExternalPerson testPerson)
            Morphological (opposingId, ExternalPerson testPerson) ]
        Assert.Equal(Ok None, result)

    [<Fact>]
    let ``environmental identifications work the same as morphological identifications`` () =
        let testId = TaxonId testBackboneData.[1].[2]
        let result = calculateTaxonomicIdentity testBackboneUpper [
            Environmental (testId, ExternalPerson testPerson)
            Environmental (testId, ExternalPerson testPerson)
            Environmental (testId, ExternalPerson testPerson) ]
        Assert.Equal(Ok (Some testId), result)

    [<Fact>]
    let ``species identifications that match at genus level lead to confirmed genus identification`` () =
        let result = calculateTaxonomicIdentity testBackboneUpper [
            Morphological (TaxonId testBackboneData.[5].[2], ExternalPerson testPerson)
            Morphological (TaxonId testBackboneData.[6].[2], ExternalPerson testPerson)
            Morphological (TaxonId testBackboneData.[7].[2], ExternalPerson testPerson) ]
        Assert.Equal(Ok (Some (TaxonId commonGenus)), result)

    [<Fact>]
    let ``species identifications that match at family level lead to confirmed family identification`` () =
        let result = calculateTaxonomicIdentity testBackboneUpper [
            Morphological (TaxonId testBackboneData.[2].[2], ExternalPerson testPerson)
            Morphological (TaxonId testBackboneData.[3].[2], ExternalPerson testPerson)
            Morphological (TaxonId testBackboneData.[4].[2], ExternalPerson testPerson) ]
        Assert.Equal(Ok (Some (TaxonId commonFamily)), result)

    [<Fact>]
    let ``genus identifications that match at family level lead to confirmed family identification`` () =
        let result = calculateTaxonomicIdentity testBackboneUpper [
            Morphological (TaxonId testBackboneData.[2].[1], ExternalPerson testPerson)
            Morphological (TaxonId testBackboneData.[3].[1], ExternalPerson testPerson)
            Morphological (TaxonId testBackboneData.[4].[1], ExternalPerson testPerson) ]
        Assert.Equal(Ok (Some (TaxonId commonFamily)), result)


module ``When calculating points scores`` =

    let ``write some tests`` () =
        failwith "not implemented"
        //calculatePointsScore