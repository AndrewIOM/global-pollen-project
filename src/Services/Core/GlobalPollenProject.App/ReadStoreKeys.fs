module GlobalPollenProject.App.ReadStoreKeys

let checkpoint = "Checkpoint"

module Statistic =
    let unknownTotal = "Statistic:UnknownSpecimenTotal"
    let unknownRemaining = "Statistic:UnknownSpecimenRemaining"
    let totalIdentifications = "Statistic:UnknownSpecimenIdentificationsTotal"
    let totalGrains = "Statistic:Grain:Total"
    let totalSlides = "Statistic:SlideTotal"
    let totalDigitisedSlides = "Statistic:SlideDigitisedTotal"
    let totalSpecies = "Statistic:Taxon:SpeciesTotal"
    let familyCount = "Statistic:Taxon:FamilyTotal"
    let genusCount = "Statistic:Taxon:GenusTotal"
    let speciesCount = "Statistic:Taxon:SpeciesTotal"
    let totalSlidesDigitised = "Statistic:SlideDigitisedTotal"
    let backboneFamilies = "Statistic:BackboneTaxa:Families"
    let backboneGenera = "Statistic:BackboneTaxa:Genera"
    let backboneSpecies = "Statistic:BackboneTaxa:Species"
    let backboneTotal = "Statistic:BackboneTaxa:Total"
    let familySubTaxaCount family = sprintf "Statistic:BackboneTaxa:%s" family
    let genusSubTaxaCount family genus = sprintf "Statistic:BackboneTaxa:%s:%s" family genus

module Profiles =
    let index = "PublicProfile:index"

module IndividualCollections =
    let collection (colId:System.Guid) ver = sprintf "ReferenceCollectionDetail:%s:V%i" (colId.ToString()) ver
    
module MRC =
    let autocompleteAll = "Autocomplete:Taxon"
    let autocomplete rank = sprintf "Autocomplete:Taxon:%s" rank
    let taxonSummary rank = sprintf "TaxonSummary:%s" rank

module Representation =

    let family = "Statistic:Representation:Families:GPP"
