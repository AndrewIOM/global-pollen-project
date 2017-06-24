module ImportTaxonomy

open System.IO
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Aggregates.Taxonomy

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
    parsed 
    |> Seq.skip 1 
    |> Seq.filter (fun t -> t.TaxonRank = "species") 
    |> Seq.sortBy (fun t -> t.TaxonomicStatus )
    |> Seq.toList

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

let findTaxonIdInCommandsByName genus species currentCommands =

    let unwrapLn (LatinName ln) = ln
    let unwrapSe (SpecificEphitet se) = se

    let command = currentCommands |> Seq.tryFind (fun c ->
        match c with
        | ImportFromBackbone ci ->
            match ci.Identity with
            | Family f -> false
            | Genus g -> false
            | Species (ln,se,auth) -> 
                (unwrapLn ln) = genus && (unwrapSe se) = species
        | _ -> false )

    // TODO
    // 1. Make sure all lookups are using authorship
    // 2. When adding synonyms:
        // If the actual taxon lookup fails, return an Error / Failure
        // If this occurs, queue it with recursion to re-add all taxa, until they have been added.
        // This is because a synonym can point to another synonym.

    match command with
    | None -> None
    | Some c ->
        match c with
        | ImportFromBackbone i -> Some i.Id
        | _ -> None

type ImportError =
| Postpone
| SynonymOfSubspecies

let createImportCommands (taxon:ParsedTaxon) (allParsed:ParsedTaxon seq) (currentCommands: Command list) generateId =

    let group = 
        match taxon.Family with
        | t when gymnosperms |> List.tryFind (fun x -> x = t) = Some t -> Gymnosperm
        | t when pteridophytes |> List.tryFind (fun x -> x = t) = Some t -> Pteridophyte
        | t when bryophytes |> List.tryFind (fun x -> x = t) = Some t -> Bryophyte
        | _ -> Angiosperm

    let status =
        let relateToOtherTaxon otherPlantListId =
            let readTaxon = allParsed |> Seq.tryFind (fun t -> t.TaxonId = otherPlantListId)
            match readTaxon with
            | None -> Error SynonymOfSubspecies
            | Some tx ->
                let existingId = findTaxonIdInCommandsByName tx.Genus tx.SpecificEphitet currentCommands
                match existingId with
                | Some id -> Ok id
                | None -> Error Postpone
        match taxon.TaxonomicStatus with
        | "accepted" -> Ok Accepted
        | "doubtful" -> Ok Doubtful
        | "misapplied" -> relateToOtherTaxon taxon.AcceptedNameUsageId |> Result.bind (fun id -> Ok (Misapplied id))
        | "synonym" -> relateToOtherTaxon taxon.AcceptedNameUsageId |> Result.bind (fun id -> Ok (Synonym id))
        | _ -> invalidOp "Corrupt input data: invalid taxonomic status"

    match status with
    | Error e -> Error e
    | Ok s ->

        let higherRankStatus =
            match s with
            | Accepted -> Accepted
            | Doubtful -> Doubtful
            | Misapplied id -> Doubtful // TODO handle alternative cases
            | Synonym id -> Doubtful // TODO handle alternative cases

        let family : Command option * TaxonId = 
            let exsitingFamily = currentCommands |> List.tryFind (fun c -> 
                                                match c with
                                                | ImportFromBackbone t -> t.Identity = Family (LatinName taxon.Family)
                                                | _ -> false )
            match exsitingFamily with
            | Some c -> None, (unwrap c)
            | None -> 
                let id = TaxonId (generateId())
                Some (ImportFromBackbone {  Id          = id
                                            Group       = group
                                            Identity    = Family (LatinName taxon.Family)
                                            Parent      = None
                                            Status      = higherRankStatus
                                            Reference   = None }) , id

        let genus : Command option * TaxonId = 
            let existingGenus = currentCommands |> List.tryFind (fun c -> 
                                                match c with
                                                | ImportFromBackbone t -> t.Identity = Genus (LatinName taxon.Genus) && t.Parent = Some (snd family)
                                                | _ -> false )
            match existingGenus with
            | Some c -> None, (unwrap c)
            | None -> 
                let id = TaxonId (generateId())
                Some (ImportFromBackbone {  Id          = id
                                            Group       = group
                                            Identity    = Genus (LatinName taxon.Genus)
                                            Parent      = Some (snd family)
                                            Status      = higherRankStatus
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
                let id = TaxonId (generateId())
                Some (ImportFromBackbone {      Id          = TaxonId (generateId())
                                                Group       = group
                                                Identity    = Species (LatinName taxon.Genus, SpecificEphitet taxon.SpecificEphitet, Scientific taxon.ScientificNameAuthorship )
                                                Parent      = Some (snd genus)
                                                Status      = s
                                                Reference   = reference })


        [ fst family; fst genus; species ] |> List.choose id |> Ok
