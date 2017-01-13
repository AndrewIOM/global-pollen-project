module GlobalPollenProject.Core.Services

open System

module TaxonomicBackbone =

    // Query CSV Plant List taxonomic backbone
    let isValidTaxon latinName rank parentTaxon =
        true