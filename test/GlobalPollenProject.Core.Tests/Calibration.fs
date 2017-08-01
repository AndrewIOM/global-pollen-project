module CalibrationTests

open System
open Xunit
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Aggregate
open GlobalPollenProject.Core.Aggregates.Calibration

let a = {
    initial = State.Initial
    evolve = State.Evolve
    handle = handle
    getId = getId 
}
let Given = Given a domainDefaultDeps

module ``When creating a calibration set`` =

    let calId = CalibrationId (Guid.NewGuid())
    let currentUser = UserId (Guid.NewGuid())
    let fakeImage = Url.create "https://sometesturl"
    let microscope = Light <| Compound (10, [40], "Nikon D3100")

    [<Fact>]
    let ``The microscope is setup correctly`` () =
        Given []
        |> When (UseMicroscope { Id = calId; User = currentUser; Microscope = microscope; FriendlyName = "Lab microscope" })
        |> Expect [ SetupMicroscope { Id = calId; User = currentUser; Microscope = microscope; FriendlyName = "Lab microscope" } ]

    [<Fact>]
    let ``A compound light microscope must have at least one objective`` () =
        let badMicroscope = Light <| Compound (10, [ ], "Nikon D3100")
        Given []
        |> When (UseMicroscope { Id = calId; User = currentUser; Microscope = badMicroscope; FriendlyName = "Lab microscope" })
        |> ExpectInvalidOp

    [<Fact>]
    let ``A magnification level that the microscope does not have cannot be calibrated`` () =
        Given [ SetupMicroscope { Id = calId; User = currentUser; Microscope = microscope; FriendlyName = "Lab microscope" } ]
        |> When (Calibrate (calId,93<timesMagnified>,{Image = fakeImage; StartPoint = 2,3; EndPoint = 4,6; MeasureLength = 2.<um>}))
        |> ExpectInvalidOp

    [<Fact>]
    let ``A magnification level cannot be calibrated twice`` () =
        Given [ SetupMicroscope { Id = calId; User = currentUser; Microscope = microscope; FriendlyName = "Lab microscope" }
                CalibratedMag { Id = calId; Magnification = 400<timesMagnified>; PixelWidth = 0.12525<um>; Image = fakeImage } ]
        |> When (Calibrate (calId,400<timesMagnified>,{Image = fakeImage; StartPoint = 2,3; EndPoint = 4,6; MeasureLength = 2.<um>}))
        |> ExpectInvalidOp

    [<Fact>]
    let ``The measure line must be longer than zero`` () =
        Given [ SetupMicroscope { Id = calId; User = currentUser; Microscope = microscope; FriendlyName = "Lab microscope" } ]
        |> When (Calibrate (calId,93<timesMagnified>,{Image = fakeImage; StartPoint = 2,3; EndPoint = 2,3; MeasureLength = 23.2<um>}))
        |> ExpectInvalidOp

    [<Fact>]
    let ``The pixel size is calculated correctly`` () =
        Given [ SetupMicroscope { Id = calId; User = currentUser; Microscope = microscope; FriendlyName = "Lab microscope" } ]
        |> When (Calibrate (calId,400<timesMagnified>,{Image = fakeImage; StartPoint = 20,40; EndPoint = 20,440; MeasureLength = 50.1<um>}))
        |> Expect [ CalibratedMag { Id = calId
                                    Magnification = 400<timesMagnified>
                                    PixelWidth = 0.12525<um>
                                    Image = fakeImage }]
