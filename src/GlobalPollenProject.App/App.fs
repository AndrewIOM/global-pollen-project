namespace GlobalPollenProject.App

open System
open System.IO
open Microsoft.Extensions.Configuration

open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.CommandHandlers
open GlobalPollenProject.Shared.Identity.Models
open System.Threading

open ReadStore
open EventStore

open AzureImageService
open GlobalPollenProject.Core.Dependencies

module Config =

    let appSettings = ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build()
    let eventStore = EventStore.SqlEventStore()
    eventStore.SaveEvent 
    :> IObservable<string*obj>
    |> EventHandlers.grainProjections 
    |> ignore

    eventStore.SaveEvent 
    :> IObservable<string*obj>
    |> EventHandlers.taxonomyProjections 
    |> ignore

    let projections = new ReadContext()

    let dependencies = 
        let generateId = Guid.NewGuid
        let log = ignore
        let uploadImage = AzureImageService.uploadToAzure "Development" appSettings.["imagestore:azureconnectionstring"] (fun x -> Guid.NewGuid().ToString())
        let gbifLink = ExternalLink.getGbifId
        let neotomaLink = ExternalLink.getNeotomaId

        let taxonomicBackbone a = None
        let calculateIdentity = calculateTaxonomicIdentity taxonomicBackbone
    
        { GenerateId        = generateId
          Log               = log
          UploadImage       = uploadImage
          GetGbifId         = gbifLink
          GetNeotomaId      = neotomaLink
          ValidateTaxon     = taxonomicBackbone
          CalculateIdentity = calculateIdentity }

module GrainAppService =

    open GlobalPollenProject.Core.Aggregates.Grain

    let aggregate = {
        initial = State.InitialState
        evolve = State.Evolve
        handle = handle
        getId = getId 
    }

    let handle = create aggregate "Grain" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save

    let submitUnknownGrain grainId (images:string list) age (lat:float) lon =
        let id = GrainId grainId
        let uploadedImages = images |> List.map (fun x -> SingleImage (Url.create x))
        let spatial = Latitude (lat * 1.0<DD>), Longitude (lon * 1.0<DD>)
        let temporal = CollectionDate (age * 1<CalYr>)
        let userId = UserId (Guid.NewGuid())
        handle (SubmitUnknownGrain {Id = id; Images = uploadedImages; SubmittedBy = userId; Temporal = Some temporal; Spatial = spatial })

    let identifyUnknownGrain grainId taxonId =
        handle (IdentifyUnknownGrain { Id = GrainId grainId; Taxon = TaxonId taxonId; IdentifiedBy = UserId (Guid.NewGuid()) })

    let listUnknownGrains() =
        Config.projections.GrainSummaries |> Seq.toList

    let listEvents() =
        Config.eventStore.Events |> Seq.toList


module UserAppService =

    open GlobalPollenProject.Core.Aggregates.User
    open GlobalPollenProject.Shared.Identity

    let handle =
        let aggregate = {
            initial = State.InitialState
            evolve = State.Evolve
            handle = handle
            getId = getId
        }
        create aggregate "User" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save

    let register userId title firstName lastName =
        handle ( Register { Id = UserId userId; Title = title; FirstName = firstName; LastName = lastName })


module TaxonomyAppService =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let handle =
        let aggregate = {
            initial = State.InitialState
            evolve = State.Evolve
            handle = handle
            getId = getId
        }
        create aggregate "Taxon" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save

    // let import name =
    //     let domainName = LatinName name
    //     let id = Guid.NewGuid()
    //     handle ( Import { Id = TaxonId id; Name = domainName; Rank = Family; Parent = None })

    let list() =
        Config.projections.TaxonSummaries |> Seq.toList


module DigitiseAppService =

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection

    let handle =
        let aggregate = {
            initial = State.Initial
            evolve = State.Evolve
            handle = handle
            getId = getId
        }
        GlobalPollenProject.Core.CommandHandlers.create aggregate "ReferenceCollection" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save


module BackboneAppService =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    module C = Config

    module CsvImport =

        type ParsedTaxon = {
            TaxonId : string 
            AcceptedNameUsageId : string
            TaxonomicStatus : string
            Family : string
            Genus : string
            SpecificEphitet : string
            InfraspecificEphitet : string
            ScientificName : string
            TaxonRank : string
            ScientificNameAuthorship : string
            NameAccordingTo : string
            NameAccordingToId : string
            ScientificNameId : string
            NamePublishedIn : string
            References : string
        }

        let readPlantListTextFile filePath : ParsedTaxon list =
        
            let reader = new StreamReader(File.OpenRead(filePath))

            let mutable parsed : ParsedTaxon list = []
            while not reader.EndOfStream do
                let line = reader.ReadLine()
                let values = line.Split('\t')
                let taxon = {   TaxonId = values.[0]
                                AcceptedNameUsageId = values.[1]
                                TaxonomicStatus = values.[2] 
                                Family = values.[3]
                                Genus = values.[4]
                                SpecificEphitet = values.[5]
                                InfraspecificEphitet = values.[6]
                                ScientificName = values.[7]
                                TaxonRank = values.[8]
                                ScientificNameAuthorship = values.[9]
                                NameAccordingTo = values.[10]
                                NameAccordingToId = values.[11]
                                ScientificNameId = values.[12]
                                NamePublishedIn = values.[13]
                                References = values.[14] }
                parsed <- parsed |> List.append [taxon]
            parsed |> Seq.skip 1 |> Seq.filter (fun t -> t.TaxonRank = "species") |> Seq.toList

        let bryophytes = ["Acrobolbaceae"; "Adelanthaceae"; "Allisoniaceae"; "Amblystegiaceae"; "Anastrophyllaceae"; "Andreaeaceae"; "Andreaeobryaceae"; "Aneuraceae"; "Antheliaceae"; "Anthocerotaceae"; "Archidiaceae"; "Arnelliaceae"; "Aulacomniaceae"; "Aytoniaceae"; "Balantiopsaceae"; "Bartramiaceae"; "Blasiaceae"; "Brachytheciaceae"; "Brevianthaceae"; "Bruchiaceae"; "Bryaceae"; "Bryobartramiaceae"; "Bryoxiphiaceae"; "Buxbaumiaceae"; "Calomniaceae"; "Calymperaceae"; "Calypogeiaceae"; "Catagoniaceae"; "Catoscopiaceae"; "Cephaloziaceae"; "Cephaloziellaceae"; "Chaetophyllopsaceae"; "Chonecoleaceae"; "Cinclidotaceae"; "Cleveaceae"; "Climaciaceae"; "Conocephalaceae"; "Corsiniaceae"; "Cryphaeaceae"; "Cyrtopodaceae"; "Daltoniaceae"; "Dendrocerotaceae"; "Dicnemonaceae"; "Dicranaceae"; "Diphysciaceae"; "Disceliaceae"; "Ditrichaceae"; "Echinodiaceae"; "Encalyptaceae"; "Entodontaceae"; "Ephemeraceae"; "Erpodiaceae"; "Eustichiaceae"; "Exormothecaceae"; "Fabroniaceae"; "Fissidentaceae"; "Fontinalaceae"; "Fossombroniaceae"; "Funariaceae"; "Geocalycaceae"; "Gigaspermaceae"; "Goebeliellaceae"; "Grimmiaceae"; "Gymnomitriaceae"; "Gyrothyraceae"; "Haplomitriaceae"; "Hedwigiaceae"; "Helicophyllaceae"; "Herbertaceae"; "Hookeriaceae"; "Hylocomiaceae"; "Hymenophytaceae"; "Hypnaceae"; "Hypnodendraceae"; "Hypopterygiaceae"; "Jackiellaceae"; "Jubulaceae"; "Jubulopsaceae"; "Jungermanniaceae"; "Lejeuneaceae"; "Lembophyllaceae"; "Lepicoleaceae"; "Lepidolaenaceae"; "Lepidoziaceae"; "Leptodontaceae"; "Lepyrodontaceae"; "Leskeaceae"; "Leucodontaceae"; "Leucomiaceae"; "Lophocoleaceae"; "Lophoziaceae"; "Lunulariaceae"; "Makinoaceae"; "Marchantiaceae"; "Mastigophoraceae"; "Meesiaceae"; "Mesoptychiaceae"; "Meteoriaceae"; "Metzgeriaceae"; "Microtheciellaceae"; "Mitteniaceae"; "Mizutaniaceae"; "Mniaceae"; "Monocarpaceae"; "Monocleaceae"; "Monosoleniaceae"; "Myriniaceae"; "Myuriaceae"; "Neckeraceae"; "Neotrichocoleaceae"; "Notothyladaceae"; "Octoblepharaceae"; "Oedipodiaceae"; "Orthorrhynchiaceae"; "Orthotrichaceae"; "Oxymitraceae"; "Pallaviciniaceae"; "Pelliaceae"; "Phyllodrepaniaceae"; "Phyllogoniaceae"; "Pilotrichaceae"; "Plagiochilaceae"; "Plagiotheciaceae"; "Pleurophascaceae"; "Pleuroziaceae"; "Pleuroziopsaceae"; "Polytrichaceae"; "Porellaceae"; "Pottiaceae"; "Prionodontaceae"; "Pseudoditrichaceae"; "Pseudolepicoleaceae"; "Pterigynandraceae"; "Pterobryaceae"; "Ptilidiaceae"; "Ptychomitriaceae"; "Ptychomniaceae"; "Racopilaceae"; "Radulaceae"; "Regmatodontaceae"; "Rhabdoweisiaceae"; "Rhachitheciaceae"; "Rhacocarpaceae"; "Rhizogoniaceae"; "Ricciaceae"; "Riellaceae"; "Rigodiaceae"; "Rutenbergiaceae"; "Scapaniaceae"; "Schistochilaceae"; "Schistostegaceae"; "Scorpidiaceae"; "Seligeriaceae"; "Sematophyllaceae"; "Serpotortellaceae"; "Sorapillaceae"; "Sphaerocarpaceae"; "Sphagnaceae"; "Spiridentaceae"; "Splachnaceae"; "Splachnobryaceae"; "Stereophyllaceae"; "Takakiaceae"; "Targioniaceae"; "Tetraphidaceae"; "Thamnobryaceae"; "Theliaceae"; "Thuidiaceae"; "Timmiaceae"; "Trachypodaceae"; "Treubiaceae"; "Trichocoleaceae"; "Trichotemnomataceae"; "Vandiemeniaceae"; "Vetaformaceae"; "Viridivelleraceae"; "Wardiaceae"; "Wiesnerellaceae" ]
        let pteridophytes = ["Anemiaceae"; "Apleniaceae"; "Aspleniaceae"; "Athyriaceae"; "Blechnaceae"; "Cibotiaceae"; "Culcitaceae"; "Cyatheaceae"; "Cystodiaceae"; "Cystopteridaceae"; "Davalliaceae"; "Dennstaedtiaceae"; "Dicksoniaceae"; "Diplaziopsidaceae"; "Dipteridaceae"; "Dryopteridacae"; "Dryopteridaceae"; "Equisetaceae"; "Gleicheniaceae"; "Hymenophyllaceae"; "Hypodematiaceae"; "Isoëtaceae"; "Lindsaeaceae"; "Lomariopsidaceae"; "Lonchitidaceae"; "Loxsomataceae"; "Lycopodiaceae"; "Lygodiaceae"; "Marattiaceae"; "Marsileaceae"; "Matoniaceae"; "Metaxyaceae"; "Nephrolepidaceae"; "Oleandraceae"; "Onocleaceae"; "Ophioglossaceae"; "Osmundaceae"; "Plagiogyriaceae"; "Polypodiaceae"; "Psilotaceae"; "Pteridaceae"; "Rhachidosoraceae"; "Saccolomataceae"; "Salviniaceae"; "Schizaeaceae"; "Selaginellaceae"; "Tectariaceae"; "Thelypteridaceae"; "Thyrsopteridaceae"; "Woodsiaceae" ]
        let gymnosperms = ["Araucariacea"; "Cupressacea"; "Cycadacea"; "Ephedracea"; "Ginkgoacea"; "Gnetacea"; "Pinacea"; "Podocarpacea"; "Sciadopityacea"; "Taxacea"; "Welwitschiacea"; "Zamiaceae" ]

        let unwrap command =
            match command with
            | ImportFromBackbone c -> c.Id
            | ConnectToExternalDatabase (c,_) -> c


        open System.Text.RegularExpressions

        let (|FirstRegexGroup|_|) pattern input =
            let m = Regex.Match(input,pattern) 
            if (m.Success) then Some m.Groups.[1].Value else None  

        let toUrl str = 
            match str with
            | FirstRegexGroup "http://(.*?)/(.*)" host -> Some (Url.create (str))
            | _ -> None

        let H = 
            let aggregate = {
                initial = State.InitialState
                evolve = State.Evolve
                handle = handle
                getId = getId }
            GlobalPollenProject.Core.CommandHandlers.create aggregate "Taxonomy" C.dependencies C.eventStore.ReadStream<Event> C.eventStore.Save

        let createImportCommands (taxon:ParsedTaxon) (currentCommands: Command list) : Command list =

            let group = match taxon.Family with
                        | t when gymnosperms |> List.tryFind (fun x -> x = t) = Some t -> Gymnosperm
                        | t when pteridophytes |> List.tryFind (fun x -> x = t) = Some t -> Pteridophyte
                        | t when bryophytes |> List.tryFind (fun x -> x = t) = Some t -> Bryophyte
                        | _ -> Angiosperm

            let family : Command option * TaxonId = 
                let exsitingFamily = currentCommands |> List.tryFind (fun c -> 
                                                    match c with
                                                    | ImportFromBackbone t -> t.Identity = Family (LatinName taxon.Family)
                                                    | _ -> false )
                match exsitingFamily with
                | Some c -> None, (unwrap c)
                | None -> 
                    let id = TaxonId (C.dependencies.GenerateId())
                    Some (ImportFromBackbone {  Id          = id
                                                Group       = group
                                                Identity    = Family (LatinName taxon.Family)
                                                Parent      = None
                                                Status      = Accepted
                                                Reference   = None }) , id

            let genus : Command option * TaxonId = 
                let existingGenus = currentCommands |> List.tryFind (fun c -> 
                                                    match c with
                                                    | ImportFromBackbone t -> t.Identity = Genus (LatinName taxon.Genus) && t.Parent = Some (snd family)
                                                    | _ -> false )
                match existingGenus with
                | Some c -> None, (unwrap c)
                | None -> 
                    let id = TaxonId (C.dependencies.GenerateId())
                    Some (ImportFromBackbone {  Id          = id
                                                Group       = group
                                                Identity    = Genus (LatinName taxon.Family)
                                                Parent      = Some (snd family)
                                                Status      = Accepted
                                                Reference   = None }) , id

            let reference =
                match taxon.NamePublishedIn.Length with
                | 0 -> None
                | _ -> match taxon.References.Length with
                    | 0 -> Some (taxon.NamePublishedIn, None)
                    | _ -> Some (taxon.NamePublishedIn, toUrl taxon.References)

            let species : Command option = 
                let existingSpecies = currentCommands |> List.tryFind (fun c -> 
                                                    match c with
                                                    | ImportFromBackbone t -> t.Identity = Family (LatinName taxon.Family) && t.Parent = Some (snd genus)
                                                    | _ -> false )

                match existingSpecies with
                | Some c -> None
                | None -> 
                    let id = TaxonId (C.dependencies.GenerateId())
                    Some (ImportFromBackbone {      Id          = TaxonId (C.dependencies.GenerateId())
                                                    Group       = group
                                                    Identity    = Species (LatinName taxon.Genus, SpecificEphitet taxon.SpecificEphitet, Scientific taxon.ScientificNameAuthorship )
                                                    Parent      = Some (snd genus)
                                                    Status      = Accepted
                                                    Reference   = reference })


            [ fst family; fst genus; species ] |> List.choose id

        let importAll filePath =

            let taxa = (readPlantListTextFile filePath) |> List.filter (fun x -> x.TaxonomicStatus = "accepted")
            let mutable commands : Command list = []
            for row in taxa do
                let additionalCommands = createImportCommands row commands
                additionalCommands |> List.map H |> ignore
                commands <- List.append commands additionalCommands
            ()
            //commands |> List.map H
