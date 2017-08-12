module GlobalPollenProject.Core.Aggregates.Calibration

open GlobalPollenProject.Core.DomainTypes
open System

type Command =
| UseMicroscope of UseMicroscope
| Calibrate of CalibrationId * Magnification * ImageCalibration

and UseMicroscope = {
    Id:             CalibrationId
    User:           UserId
    FriendlyName:   Label
    Microscope:     Microscope
}

and ImageCalibration = {
    Image:          RelativeUrl
    StartPoint:     int * int
    EndPoint:       int * int
    MeasureLength:  float<um>
}

type Event =
| SetupMicroscope of SetupMicroscope
| CalibratedMag of CalibratedMag
and SetupMicroscope = { Id: CalibrationId; User: UserId; Microscope: Microscope; FriendlyName: Label }
and CalibratedMag = { Id: CalibrationId; Magnification: Magnification; Image: RelativeUrl; PixelWidth: float<um> }

type State =
| Initial
| Uncalibrated of CalState
| Partial of CalState
| Complete of CalState

and CalState = {
    Owner: UserId
    Microscope: Microscope
    CalibratedMagnifications: (int<timesMagnified> * ImageState) list
}

and ImageState = {
    Image: RelativeUrl
    ImageScaleFactor: float<um>
}

let checkObjectives microscope =
    match microscope with
    | Light l ->
        match l with
        | Single _
        | Digital _ -> ()
        | Compound (_,objectives,_) ->
            if objectives |> List.isEmpty
            then invalidOp "Composite microscopes must have at least one objective"
            else ()

let getMagnifications microscope =
    match microscope with
    | Light l ->
        match l with
        | Single (mag,model) -> [ mag ]
        | Digital (mag,model) -> [ mag ]
        | Compound (oc,ob,model) ->
            ob |> List.map (fun x -> x * oc * 1<timesMagnified>)

let calculatePixelDistance x1 y1 x2 y2 measuredLength = 
        match (x2 - x1) + (y2 - y1) with
        | 0 -> invalidOp "You must specify a line of length greater than zero"
        | _ ->
            let pixelDistance = sqrt ((((float x2)-(float x1))**2.) + (((float y2)-(float y1))**2.))
            let scale (actual:float<_>) image = actual / image
            scale measuredLength pixelDistance

let setup (command:UseMicroscope) state =
    match state with
    | Uncalibrated _
    | Partial _
    | Complete _ -> invalidOp "Calibration is already partially set up"
    | Initial ->
        checkObjectives command.Microscope
        [ SetupMicroscope { Id = command.Id
                            User = command.User
                            FriendlyName = command.FriendlyName
                            Microscope = command.Microscope } ]

let calibrate id mag imageCal state =
    match state with
    | Initial -> invalidOp "Calibration does not exist"
    | Complete _ -> invalidOp "Already calibrated"
    | Uncalibrated s
    | Partial s ->
        let mags = getMagnifications s.Microscope
        if mags |> List.contains mag then () else invalidOp "Magnification not supported by this microscope"
        match s.CalibratedMagnifications |> List.tryFind (fun x -> fst x = mag) with
        | Some m -> invalidOp "Magnification already calibrated"
        | None ->
             let x1,y1 = imageCal.StartPoint
             let x2,y2 = imageCal.EndPoint
             let pixDist = calculatePixelDistance x1 y1 x2 y2 imageCal.MeasureLength
             [ CalibratedMag { Id = id; Magnification = mag; PixelWidth = pixDist; Image = imageCal.Image } ]

let handle deps = 
    function
    | UseMicroscope c -> setup c
    | Calibrate (id,mag,img) -> calibrate id mag img

type State with
    static member Evolve state = function
        | SetupMicroscope e ->
            match state with
            | Initial -> Uncalibrated { Owner = e.User
                                        Microscope = e.Microscope
                                        CalibratedMagnifications = [] }
            | _ -> invalidOp "Invalid state change"
        | CalibratedMag e ->
            match state with
            | Complete _
            | Initial -> invalidOp "Invalid state change"
            | Uncalibrated s
            | Partial s ->
                let newMag = e.Magnification, { Image = e.Image; ImageScaleFactor = e.PixelWidth }
                let allMags = getMagnifications s.Microscope
                if allMags.Length = s.CalibratedMagnifications.Length + 1
                then Complete { s with CalibratedMagnifications = newMag :: s.CalibratedMagnifications }
                else Partial { s with CalibratedMagnifications = newMag :: s.CalibratedMagnifications }


let getId = 
    let unwrap (CalibrationId e) = e
    function
    | UseMicroscope c -> unwrap c.Id
    | Calibrate (id,_,_) -> unwrap id
