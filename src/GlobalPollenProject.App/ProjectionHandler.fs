module GlobalPollenProject.App.ProjectionHandler

open System
open GlobalPollenProject.Core.Composition
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.App.Projections
open ReadStore

let validateCheckpoint get set getEventCount e =
    // let validate readCheckpoint writeCheckpoint =
    //     match readCheckpoint = writeCheckpoint with
    //     | true -> Ok writeCheckpoint
    //     | false -> Error "Checkpoint mismatch - rebuild read model"
    // validate
    // <*> Checkpoint.getCurrentVersion get
    // <*> getEventCount()
    Ok e

let init set () =
    Checkpoint.init set |> ignore
    Statistics.init set |> ignore
    TaxonomicBackbone.init set

let route 
    (get:GetFromKeyValueStore) 
    (getList:GetListFromKeyValueStore) 
    (getSortedList:GetSortedListFromKeyValueStore) 
    (set:SetStoreValue) 
    (setList:SetEntryInList) 
    (setSortedList:SetEntryInSortedList) 
    (generateThumb:RelativeUrl->Result<Url,string>)
    (toAbsoluteUrl:RelativeUrl->Url)
    (e:string*obj*DateTime) =

    let feed (f:(string*obj*DateTime)->Result<unit,string>) (e:string*obj*DateTime) =
        let r = f e
        match r with
        | Ok o -> Ok e
        | Error e -> Error e

    e
    |> feed (TaxonomicBackbone.handle get getSortedList set setSortedList)
    >>= feed (Digitisation.handle get getList set setList generateThumb toAbsoluteUrl)
    >>= feed (Calibration.handle get getList set setList toAbsoluteUrl)
    >>= feed (UserProfile.handle get set setList)
    >>= feed (ReferenceCollectionReadOnly.handle get set setList)
    >>= feed (Grain.handle get set setList generateThumb toAbsoluteUrl)
    >>= feed (Statistics.handle get getSortedList set setSortedList)
    >>= MasterReferenceCollection.handle get getSortedList set setSortedList


type Message = 
    (string*obj*DateTime) * AsyncReplyChannel<Result<int,string>>

let readModelAgent handleEvent get set getEventCount =
  MailboxProcessor<Message>.Start(fun inbox ->
  let rec loop () =
    async {
      let! (message, replyChannel) =
        inbox.Receive()
      let result = 
            message
            |> validateCheckpoint get set getEventCount
            |> Result.bind handleEvent
            |> Result.bind (Checkpoint.increment get set)      
      replyChannel.Reply result
      do! loop ()
    }
  loop ())
