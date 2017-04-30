module GlobalPollenProject.Core.Aggregates.Calibration

open GlobalPollenProject.Core.DomainTypes
open System

type Command =
| Calibrate of Calibrate
and Calibrate = 
    {Id:CalibrationId
     User: UserId
     Device: string
     Ocular:int
     Objective:int
     Image: Url
     StartPoint: int * int
     EndPoint: int * int
     MeasureLength: float<um>}

type Event =
| Calibrated of Calibrated
and Calibrated = {Id: CalibrationId; User: UserId; Device: string; 
                  Ocular:int; Objective:int; Image: Url; PixelWidth: float<um>}

type State =
| Initial
| Complete of CalState
and CalState = {
    User: UserId
    Device: string
    Ocular: int
    Objective: int
    Image: Url
    ImageScaleFactor: float<um>
}

let calibrate (command:Calibrate) state =
    match state with
    | Initial ->
        let x1,y1 = command.StartPoint
        let x2,y2 = command.EndPoint
        match (x2 - x1) + (y2 - y1) with
        | 0 -> invalidArg "You must specify a line" "coordinates"
        | _ ->
            let pixelDistance = sqrt ((((float x2)-(float x1))**2.) + (((float y2)-(float y1))**2.))
            let scale (actual:float<_>) image = actual / image
            let mag = scale command.MeasureLength pixelDistance
            [ Calibrated { Id = command.Id; User = command.User; Device = command.Device; 
                           Ocular = command.Ocular; Objective = command.Objective; Image = command.Image; PixelWidth = mag} ]
    | Complete c -> invalidOp "The calibration already exists"
 
let handle deps = 
    function
    | Calibrate c -> calibrate c

type State with
    static member Evolve state = function

        | Calibrated event ->
            match state with
            | Complete c -> invalidOp "Cannot calibrate complete collections"
            | Initial ->
                Complete {User = event.User; 
                            Device = event.Device; 
                            Ocular = event.Ocular; 
                            Objective = event.Objective; 
                            Image = event.Image; 
                            ImageScaleFactor = event.PixelWidth}

let getId = 
    let unwrap (CalibrationId e) = e
    function
    | Calibrate c -> unwrap c.Id
