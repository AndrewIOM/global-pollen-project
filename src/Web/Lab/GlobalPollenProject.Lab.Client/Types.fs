module GlobalPollenProject.Lab.Client.Types

open System
open Bolero
open Bolero.Remoting
open ReadModels

/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/counter">] Counter
    | [<EndPoint "/collections">] Collections
    | [<EndPoint "/collections/start">] StartCollection
    | [<EndPoint "/collections/{collectionId}">] ViewCollection of collectionId: string
    | [<EndPoint "/collections/{collectionId}/add-slide">] AddSlideForm of collectionId: string
    | [<EndPoint "/collections/{collectionId}/{slideId}">] SlideDetailView of collectionId: string * slideId: string
    | [<EndPoint "/collections/{collectionId}/{slideId}/focus">] SlideFocusImage of collectionId: string * slideId: string
    | [<EndPoint "/collections/{collectionId}/{slideId}/static">] SlideStaticImage of collectionId: string * slideId: string
    | [<EndPoint "/calibrations">] Calibrations
    | [<EndPoint "/calibrations/add">] AddCalibration
    | [<EndPoint "/calibrations/{calId}">] CalibrationDetail of calId:string

type TaxonomicRank =
    | Family
    | Genus
    | Species

type Draft =
    | DraftCollection of StartCollectionRequest
    | DraftSlide of SlideRecordRequest * TaxonomicRank * BackboneTaxon list option
    | DraftSlideImage of SlideImageRequest * ImageDraftModel
    | DraftMicroscope of AddMicroscopeRequest
    | DraftCalibration of CalibrateRequest

// TODO Add progress bar and queue
and ActiveDraft = Draft //* ValidationError list

and ImageDraftModel = {
    SelectedCalibration: Calibration option
    SelectedMagnification: Magnification option
}

/// The Elmish application's model.
type Model =
    {
        page: Page
        counter: int
        collections: EditableRefCollection list option
        calibrations: Calibration list option
        draft: ActiveDraft option
        error: string option
        username: string
        password: string
        signedInAs: string option
        signInFailed: bool
    }

let initModel =
    {
        page = Home
        counter = 0
        collections = None
        calibrations = None
        draft = None
        error = None
        username = ""
        password = ""
        signedInAs = None
        signInFailed = false
    }

type result<'a> = Result<'a,ServiceError>

/// Remote service definition.
type DigitiseService =
    {
        /// Get the list of all reference collections for the user
        getCollections: unit -> Async<EditableRefCollection list>

        /// Get a specific collection in editing mode.
        getCollection: string -> Async<EditableRefCollection>
        
        /// Add a reference collection.
        startCollection: StartCollectionRequest -> Async<Guid result>

        /// Publish a reference collection by its ID.
        publishCollection: Guid -> Async<unit result>

        /// Record a slide within a collection.
        addSlideRecord: SlideRecordRequest -> Async<unit result>
        
        /// Voids a slide record as it stands.
        voidSlide: VoidSlideRequest -> Async<unit result>
        
        /// Add a single or focus image to a slide record.
        addImageToSlide: SlideImageRequest -> Async<unit result>

        /// List available calibrations on this user account.        
        getCalibrations: unit -> Async<Calibration list result>
        
        /// Set up a fixed camera system for a microscope.
        setupMicroscope: AddMicroscopeRequest -> Async<unit result>
        
        /// Set up a calibration on a microscope.
        setupMagnification: CalibrateRequest -> Async<unit result>
        
        /// Sign into the application.
        signIn : string * string -> Async<option<string>>

        /// Get the user's name, or None if they are not authenticated.
        getUsername : unit -> Async<string>

        /// Sign out from the application.
        signOut : unit -> Async<unit>
    }

    interface IRemoteService with
        member this.BasePath = "/digitise-service"


/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | Increment
    | Decrement
    | SetCounter of int
    | GetCollections
    | GotCollections of EditableRefCollection list
    | ChangeNewCollection of StartCollectionRequest
    | ChangeDraftSlide of SlideRecordRequest
    | ChangeDraftSlideImage of SlideImageRequest * ImageDraftModel
    | ChangeDraftMicroscope of AddMicroscopeRequest
    | ChangeDraftObjective of CalibrateRequest
    | ToggleRank of TaxonomicRank
    | RequestPublication of Guid
    | SendStartCollection
    | RecvStartCollection of Guid result
    | SendVoidSlide of Guid * string
    | SendUploadImage
    | SendAddMicroscope
    | SetUsername of string
    | SetPassword of string
    | GetSignedInAs
    | RecvSignedInAs of option<string>
    | SendSignIn
    | RecvSignIn of option<string>
    | SendSignOut
    | RecvSignOut
    | Error of exn
    | ClearError

and ViewerMessage =
    | StartLineDraw
    | ViewerReady
    | DrawnLine of float
