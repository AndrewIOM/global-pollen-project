module GlobalPollenProject.App.ProjectionHandler

open System
open GlobalPollenProject.Core.Composition
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

let route 
    (get:GetFromKeyValueStore) 
    (getList:GetListFromKeyValueStore) 
    (getSortedList:GetListFromKeyValueStore) 
    (set:SetStoreValue) 
    (setList:SetEntryInList) 
    (setSortedList:SetEntryInSortedList) 
    (e:string*obj) =

    let feed (f:(string*obj)->Result<unit,string>) (e:string*obj) =
        let r = f e
        match r with
        | Ok o -> Ok e
        | Error e -> Error e

    e
    |> feed (TaxonomicBackbone.handle get getSortedList set setSortedList)
    >>= feed (Digitisation.handle get getList set setList)
    >>= feed UserProfile.handle
    >>= feed ReferenceCollectionReadOnly.handle
    >>= feed Slide.handle
    >>= feed (Grain.handle set)
    >>= MasterReferenceCollection.handle


type Message = 
    (string*obj) * AsyncReplyChannel<Result<int,string>>

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
