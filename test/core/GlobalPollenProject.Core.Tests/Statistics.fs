module StatisticTests

open System
open Xunit
open GlobalPollenProject.Core.Statistics
open MathNet.Numerics

module ``when computing the dip statistic`` =

    [<Fact>]
    let ``a unimodal distribution has a non-significant p value`` () =
        let unimodal = Distributions.Normal.Samples(100., 2.) |> Seq.take 10 |> Seq.toArray
        match DipTest.dipTestSigSimulated unimodal 2000 (System.Random()) with
        | Ok (result, pValue) -> 
            printfn "Dip test p value was %f (dip = %A)" pValue result
            Assert.True(pValue >= 0.05, sprintf "Expected p-value under 0.05, but was %f" pValue)
        | Error e -> Assert.True(false, e)

    [<Fact>]
    let ``a bimodal distribution has a significant p value`` () =
        let unimodal1 = Distributions.Normal.Samples(100., 2.) |> Seq.take 5 |> Seq.toArray
        let unimodal2 = Distributions.Normal.Samples(200., 2.) |> Seq.take 5 |> Seq.toArray
        match DipTest.dipTestSigSimulated (Array.append unimodal1 unimodal2) 2000 (System.Random()) with
        | Ok (result, pValue) -> 
            printfn "Dip test p value was %f (dip = %A)" pValue result
            Assert.True(pValue <= 0.05, sprintf "Expected p-value over 0.05, but was %f" pValue)
        | Error e -> Assert.True(false, e)