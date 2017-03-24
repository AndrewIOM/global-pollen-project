module CalibrationTests

open System
open Xunit
open GlobalPollenProject.Core.Types    
open GlobalPollenProject.Core.Aggregates.Calibration

let a = {
    initial = State.Initial
    evolve = State.Evolve
    handle = handle
    getId = getId 
}
let Given = Given a defaultDependencies

module ``When creating a calibration set`` =

    let calId = CalibrationId (Guid.NewGuid())
    let currentUser = UserId (Guid.NewGuid())

    [<Fact>]
    let ``The measurement must be greater than zero`` () =
        Given [  ]
        |> When (Calibrate {Id = calId
                            User = currentUser
                            Device = "Nikon"
                            Ocular = 10
                            Objective = 40
                            Image = Url.create "someurl"
                            StartPoint = 20,43
                            EndPoint = 20,43
                            MeasureLength = 23.2<um> } )
        |> ExpectInvalidArg

    
    [<Fact>]
    let ``The pixel size is calculated correctly`` () =
        Given [  ]
        |> When (Calibrate {Id = calId
                            User = currentUser
                            Device = "Nikon"
                            Ocular = 10
                            Objective = 40
                            Image = Url.create "someurl"
                            StartPoint = 20,40;
                            EndPoint = 20,440;
                            MeasureLength = 50.1<um> } )
        |> Expect [ Calibrated {Id = calId; User = currentUser; Device = "Nikon"; Ocular = 10; Objective = 40; Image = Url.create "someurl"; PixelWidth = 0.12525<um>} ]
