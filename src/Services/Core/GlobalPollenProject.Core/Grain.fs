module GlobalPollenProject.Core.Aggregates.Grain

open GlobalPollenProject.Core
open GlobalPollenProject.Core.DomainTypes

// Commands
type Command =
    | SubmitUnknownGrain of SubmitUnknownGrain
    | IdentifyUnknownGrain of IdentifyUnknownGrain
    | IdentifyTrait of IdentifyTrait
    | DeriveGrainFromSlide of DeriveGrainFromSlide
    | ReportProblem of GrainId * GrainProblem

and SubmitUnknownGrain = {
    Id: GrainId
    SubmittedBy: UserId
    Images: Image list
    Temporal: Age option
    Spatial: Site
}

and IdentifyUnknownGrain = {
    Id: GrainId
    IdentifiedBy: UserId
    Taxon: TaxonId
}

and IdentifyTrait = {
    Id: GrainId
    IdentifiedBy: UserId
    Trait: CitizenScienceTrait
}

and DeriveGrainFromSlide = {
    Id: GrainId
    Origin: SlideId * ColVersion
    Taxon: IdentificationStatus
    Image: Image
    ImageCroppedArea: CartesianBox option
}

and GrainProblem =
    | IsMultipleSpecimen
    | IsNotSpecimen

// Events
type Event =
    | GrainSubmitted of GrainSubmitted
    | GrainDerived of GrainDerived
    | GrainIdentified of GrainIdentified
    | GrainIdentityConfirmed of GrainIdentityConfirmed
    | GrainIdentityChanged of GrainIdentityChanged
    | GrainIdentityUnconfirmed of GrainIdentityUnconfirmed
    | GrainIdentifiedExternally of GrainIdentifiedExternally
    | GrainTraitIdentified of GrainTraitIdentified
    | GrainTraitConfirmed of GrainTraitConfirmed
    | ProblemFlagged of GrainFlagged

and GrainSubmitted = {
    Id: GrainId
    Images: Image list
    Owner: UserId
    Temporal: Age option
    Spatial: SamplingLocation
}

and GrainDerived = {
    Id: GrainId
    Origin: SlideId * ColVersion
    Image: Image
    ImageCroppedArea: CartesianBox option
}

and GrainIdentified = {
    Id: GrainId
    Taxon: TaxonId
    IdentifiedBy: UserId
}

and GrainIdentifiedExternally = {
    Id: GrainId
    Identification: TaxonIdentification
}

and GrainIdentityConfirmed = {
    Id: GrainId
    Taxon: TaxonId
}

and GrainIdentityChanged = {
    Id: GrainId
    Taxon: TaxonId
}

and GrainIdentityUnconfirmed = {
    Id: GrainId
}

and GrainTraitIdentified = {
    Id: GrainId
    Trait: CitizenScienceTrait
    IdentifiedBy: UserId
}

and GrainTraitConfirmed = {
    Id: GrainId
    Trait: ConfirmedTrait
}

and GrainFlagged = {
    Id: GrainId
    Flag: GrainProblem
}

// State tracking
type State =
    | InitialState
    | Submitted of GrainState

// Traits:
// * Should build up a distribution of each trait as it goes along.
// * When some statistical threshold is reached in distribution, mark as definitive.
// * When definitive, no more trait identifications are required (as marked in read model), but are accepted.

// e.g. shape. 5x the same?
// e.g. diameter. When within a certain standard deviation of a given min n (like 5)?

// can we detect the distribution i.e. is it a normal distribution, or bimodal?
// bimodal would indicate problems, but normal would indicate error on measurement.
// - gives mean value, plus measurement error (based on citizen science approach).


and GrainState = {
    Owner: UserId option
    Images: (Image * CartesianBox option) list
    IdentificationStatus: IdentificationStatus
    FromReferenceMaterial: bool
    TraitMeasurements: Map<UserId,CitizenScienceTrait list>
    Flags: GrainProblem list
}

// Decisions
let submitGrain (command: SubmitUnknownGrain) state =
    if command.Images.Length = 0 then invalidArg "images" "At least one image is required"
    match state with
    | InitialState ->
        [ GrainSubmitted { Id = command.Id
                           Owner = command.SubmittedBy
                           Spatial = Site command.Spatial
                           Temporal = command.Temporal
                           Images = command.Images }]
    | _ -> 
        invalidOp "This grain has already been submitted"

let deriveGrainFromSlide getImageDimension (command:DeriveGrainFromSlide) state =
    match state with
    | InitialState ->
        let successEvents =
            match command.Taxon with
            | Unidentified
            | Partial _ -> invalidOp "Cannot derive grains from partially identified material"
            | Confirmed (ids,_) -> 
                if ids.Length = 0 then invalidOp "Cannot derive grains from partially identified material" else
                    GrainDerived {  Id = command.Id
                                    Origin = command.Origin
                                    Image = command.Image
                                    ImageCroppedArea = command.ImageCroppedArea } :: (ids |> List.map(fun i -> GrainIdentifiedExternally { Id = command.Id; Identification = i }))
        match command.ImageCroppedArea with
        | None -> successEvents
        | Some crop ->
            match getImageDimension command.Image with
            | Error _ -> invalidOp "Could not get image dimensions."
            | Ok (dims:Dimensions) ->
                if crop.BottomRight.X >= 0<pixels> && crop.BottomRight.X <= dims.Width
                    && crop.TopLeft.X >= 0<pixels> && crop.TopLeft.X <= dims.Width
                    && crop.BottomRight.Y >= 0<pixels> && crop.BottomRight.Y <= dims.Height
                    && crop.TopLeft.Y >= 0<pixels> && crop.TopLeft.Y <= dims.Height
                    && crop.BottomRight.X - crop.TopLeft.X > 0<pixels>
                    && crop.BottomRight.Y - crop.TopLeft.Y > 0<pixels>
                then successEvents
                else invalidOp "Crop cannot be outside the image bounds"
    | _ -> invalidOp "This grain has already been submitted"

let tryFindIdentificationForUser ids userId =
    ids |> List.tryFind(fun i ->
        match i with
        | Environmental (_,u)
        | Morphological (_,u) ->
            match u with
            | PollenProjectUser u -> u = userId
            | ExternalPerson _ -> false
        | Botanical _ -> false
    )

let identifyGrain calculateIdentity (command: IdentifyUnknownGrain) state =
    match state with
    | InitialState -> invalidOp "This grain does not exist"
    | Submitted s -> 
        let newId = Morphological (command.Taxon, PollenProjectUser command.IdentifiedBy)
        
        let evaluate oldIds newId taxon =
            printfn "Old IDs: %i." (oldIds |> List.length)
            let result = GrainIdentified { Id = command.Id; Taxon = command.Taxon; IdentifiedBy = command.IdentifiedBy }
            if tryFindIdentificationForUser oldIds command.IdentifiedBy |> Option.isSome then invalidOp "Cannot submit a second ID"
            let ids = newId :: oldIds
            let confirmedId = 
                printfn "Evaluating grain identity. It has %i IDs" (ids |> List.length)
                if ids |> List.length < 3 then None
                else 
                    match calculateIdentity ids with
                    | Ok i -> i
                    | Error _ -> invalidOp "Failure when calculating taxonomic identity"
            match confirmedId with
            | Some identity ->
                let confirmed = GrainIdentityConfirmed { Id = command.Id; Taxon = identity }
                match taxon with
                | Some existing ->
                    if existing = identity then [result]
                    else [result; GrainIdentityChanged { Id = command.Id; Taxon = identity } ]
                | None -> [result;confirmed]
            | None ->
                match taxon with
                | Some _ -> [result; GrainIdentityUnconfirmed { Id = command.Id } ]
                | None -> [result]
        
        match s.IdentificationStatus with
        | Unidentified -> evaluate [] newId None
        | Partial ids -> evaluate ids newId None
        | Confirmed (ids,t) -> evaluate ids newId (Some t)


module Traits =

    /// Simple test for normal vs multimodal distribution.
    /// Here we assume that the traits measured on continuous
    /// range have a true value with measurement error. In addition,
    /// it is possible that there may be mistakes being made in the
    /// measurement (e.g. for pinus measuring with or without air sacs).
    /// The idea is to pick up where there is disagreement in the measurements
    /// and stop these from becoming confirmed. Where there is unimodal, we can
    /// confirm, even if the associated error in measurement is high.
    /// See: https://skeptric.com/dip-statistic/
    let continuousTraitTest rnd (v:float<um> list) : MeasureUncertainty option =
        let removeUnit (x:float<_>) = float x
        let dipTest = Statistics.DipTest.dipTestSigSimulated (v |> List.map removeUnit |> List.toArray) 2000 rnd
        match dipTest with
        | Error _ -> None
        | Ok (dip, pValue) ->
            if pValue >= 0.05
            then Some { Value = v |> Seq.average; MeasurementUncertainty = 0.<um> } // TODO Measurement uncertainty calculation
            else None

    /// For categorical traits, we use the same algorithm for taxonomic identification,
    /// i.e. there need to be at least n observations, and these need to be the same.
    /// We also add in a disagreement score, such that the more disagreement there
    /// is, the higher the threshold in terms of the n required.
    let categoricalTraitTest v =
        Dependencies.acceptedCategoricalValue (fun a -> a, 1.) v

    let identify confirmationTest userTraitIds traitMeasurements f conf (command:IdentifyTrait) =
        match userTraitIds |> List.choose f |> List.isEmpty with
        | false -> invalidOp "Cannot submit another trait ID"
        | true ->
            let event = GrainTraitIdentified {
                    Id = command.Id
                    IdentifiedBy = command.IdentifiedBy
                    Trait = command.Trait
                }
            let accepted = 
                traitMeasurements
                |> Map.toList 
                |> List.map snd |> List.collect id
                |> List.append [ command.Trait ]
                |> List.choose f
                |> confirmationTest
            match accepted with
            | Some t -> [ event; GrainTraitConfirmed { Id = command.Id; Trait = conf t } ]
            | None -> [ event ]

    /// Identify a categorical trait. Only allow if specific trait not already identified
    /// by this person. Trigger confirmation event when trait meets confirmation criteria.
    let identifyCategorical userTraitIds traitMeasurements (f:CitizenScienceTrait -> Option<'b>) (conf:'b->ConfirmedTrait) (command:IdentifyTrait) =
        identify categoricalTraitTest userTraitIds traitMeasurements f conf command

    /// Identify a continuous trait. Only allow if specific trait not already identified
    /// by this person. Trigger confirmation event when trait meets confirmation criteria.
    let identifyContinuous rnd userTraitIds traitMeasurements f conf command =
        identify (continuousTraitTest rnd) userTraitIds traitMeasurements f conf command


let identifyTrait rnd (command:IdentifyTrait) state =
    match state with
    | InitialState -> invalidOp "This grain does not exist"
    | Submitted s -> 
        let userTraitIds = 
            s.TraitMeasurements 
            |> Map.tryFind command.IdentifiedBy 
            |> Option.toList 
            |> List.collect id
        match command.Trait with
        | Shape _ -> 
            Traits.identifyCategorical userTraitIds s.TraitMeasurements 
                (fun t -> match t with | Shape sh -> (match sh with | GrainShape.Unsure -> None | _ -> Some sh) | _ -> None) ConfirmedShape command
        | Pattern _ -> 
            Traits.identifyCategorical userTraitIds s.TraitMeasurements 
                (fun t -> match t with | Pattern sh -> (match sh with | Patterning.Unsure -> None | _ -> Some sh) | _ -> None) ConfirmedPattern command
        | Pores _ -> 
            Traits.identifyCategorical userTraitIds s.TraitMeasurements 
                (fun t -> match t with | Pores sh -> (match sh with | Unsure -> None | _ -> Some sh) | _ -> None) ConfirmedPores command
        | WallThickness _ ->
            Traits.identifyContinuous rnd userTraitIds s.TraitMeasurements 
                (fun t -> match t with | WallThickness sh -> Some sh | _ -> None) ConfirmedWall command
        | Size _ -> failwith "not finished"
            // Traits.identifyContinuous rnd userTraitIds s.TraitMeasurements 
            //     (fun t -> match t with | Size (s1,s2) -> Some (s1,s2) | _ -> None) ConfirmedSize command

let report grainId problem state =
    match state with
    | InitialState -> invalidOp "This grain does not exist"
    | Submitted s -> 
        if s.Flags |> Seq.contains problem
        then []
        else [ ProblemFlagged { Id = grainId; Flag = problem } ]

// Handle Commands to make Decisions.
let handle (deps:Aggregate.Dependencies) = 
    function
    | SubmitUnknownGrain command -> submitGrain command
    | IdentifyUnknownGrain command -> identifyGrain deps.CalculateIdentity command
    | DeriveGrainFromSlide command -> deriveGrainFromSlide deps.GetImageDimension command
    | ReportProblem(grainId, problem) -> report grainId problem
    | IdentifyTrait command -> identifyTrait deps.Random command

let private unwrap (GrainId e) = e
let getId = function
    | SubmitUnknownGrain c -> unwrap c.Id
    | IdentifyUnknownGrain c -> unwrap c.Id
    | DeriveGrainFromSlide c -> unwrap c.Id
    | ReportProblem(c, _) -> unwrap c
    | IdentifyTrait c -> unwrap c.Id

// Apply decisions already taken (rebuild)
let identified state newId =
    match state with
    | InitialState -> invalidOp "Grain is not submitted"
    | Submitted grainState ->
        match grainState.IdentificationStatus with
        | Unidentified ->
            printfn "Grain is now partially identified"
            Submitted {
                grainState with
                    IdentificationStatus = Partial ([newId]) }
        | Partial ids ->
            printfn "Partially ID'd grain gained new id. Current IDs: %i" ids.Length
            Submitted {
                grainState with
                    IdentificationStatus = Partial (newId :: ids) }
        | Confirmed (ids,t) ->
            Submitted {
                grainState with
                    IdentificationStatus = Confirmed (newId :: ids,t) }

type State with
    static member Evolve state = function

        | GrainSubmitted event -> 
            Submitted { 
              IdentificationStatus = Unidentified
              Flags = []
              TraitMeasurements = Map.empty
              FromReferenceMaterial = false
              Owner = Some event.Owner
              Images = event.Images |> List.map(fun i -> i, None) }

        | GrainDerived event ->
            Submitted {
                IdentificationStatus = Unidentified
                Flags = []
                TraitMeasurements = Map.empty
                FromReferenceMaterial = true
                Owner = None
                Images = [ event.Image, event.ImageCroppedArea ]
            }

        | GrainIdentified event -> identified state (Morphological (event.Taxon, PollenProjectUser event.IdentifiedBy))
        | GrainIdentifiedExternally event -> identified state event.Identification

        | GrainIdentityConfirmed event ->
            match state with
            | InitialState -> invalidOp "Grain is not submitted"
            | Submitted grainState ->
                match grainState.IdentificationStatus with
                | Partial ids ->
                    Submitted {
                        grainState with
                            IdentificationStatus = Confirmed (ids, event.Taxon) }
                | _ -> invalidOp "Cannot transition from unsubmitted or confirmed to confirmed"

        | GrainIdentityChanged event ->
            match state with
            | InitialState -> invalidOp "Grain is not submitted"
            | Submitted grainState ->
                match grainState.IdentificationStatus with
                | Confirmed (ids,_) ->
                    Submitted {
                        grainState with
                            IdentificationStatus = Confirmed (ids, event.Taxon) }
                | _ -> invalidOp "Can only 'change' taxon ID for a confirmed ID grain"

        | GrainIdentityUnconfirmed event ->
            match state with
            | InitialState -> invalidOp "Grain is not submitted"
            | Submitted grainState ->
                match grainState.IdentificationStatus with
                | Confirmed (ids,_) ->
                    Submitted {
                        grainState with
                            IdentificationStatus = Partial ids }
                | _ -> invalidOp "Cannot transition from unsubmitted or unconfirmed to unconfirmed"
        
        | GrainTraitIdentified event ->
            match state with
            | InitialState -> invalidOp "Grain is not submitted"
            | Submitted grainState ->
                match grainState.TraitMeasurements |> Map.tryFind event.IdentifiedBy with
                | Some traits -> Submitted { grainState with TraitMeasurements = grainState.TraitMeasurements |> Map.add event.IdentifiedBy (event.Trait::traits) }
                | None -> Submitted { grainState with TraitMeasurements = grainState.TraitMeasurements |> Map.add event.IdentifiedBy [ event.Trait ] }

        | GrainTraitConfirmed event ->
            // TODO implement this
            state

        | ProblemFlagged event -> 
            match state with
            | InitialState -> invalidOp "Grain is not submitted"
            | Submitted grainState ->
                Submitted { grainState with Flags = event.Flag::grainState.Flags }