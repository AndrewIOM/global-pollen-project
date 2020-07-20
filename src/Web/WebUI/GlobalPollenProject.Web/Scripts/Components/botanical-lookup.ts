import * as ko from 'knockout';

const traceEndpoint = "/api/v1/backbone/trace?"
const searchEndpoint = "/api/v1/backbone/search?"

export function activate(_: HTMLElement) {
    $(() => {
        ko.applyBindings(new BotanicalLookupToolViewModel());
    })
}

export class BotanicalLookupToolViewModel {

    rank: KnockoutObservable<string>
    family: KnockoutObservable<string>
    genus: KnockoutObservable<string>
    species: KnockoutObservable<string>
    author: KnockoutObservable<string>
    isValidSearch: KnockoutComputed<boolean>
    currentTaxon: KnockoutObservable<string> // Valid taxon ID (GUID)
    newSlideTaxonStatus: KnockoutObservable<any> // Contains array of API results
    
    doneTypingInterval: number
    typingTimer: number

    constructor() {
        this.rank = ko.observable("");
        this.family = ko.observable("");
        this.genus = ko.observable("");
        this.species = ko.observable("");
        this.author = ko.observable("");
        this.newSlideTaxonStatus = ko.observable(null);
        this.currentTaxon = ko.observable("");
        this.rank.subscribe((rank) => this.switchRank(rank));
        this.isValidSearch = ko.computed(this.searchIsValid, this);
        this.doneTypingInterval = 100;
    }
    
    switchRank = (rank) => {
        if (rank == "Family") {
            this.genus("");
            this.species("");
            this.author("");
        } else if (rank == "Genus") {
            this.species("");
            this.author("");
        }
    }

    public searchIsValid = () => {
        if (this.rank() == "Family" && this.family().length > 0) return true;
        if (this.rank() == "Genus" && this.genus().length > 0) return true;
        return this.rank() == "Species" && this.genus().length > 0 && this.species().length > 0;
    }

    public requestValidation = () => {
        let queryString;
        if (this.rank() == "Family") {
            queryString = "rank=Family&family=" + this.family() + "&latinname=" + this.family();
        } else if (this.rank() == 'Genus') {
            queryString = "rank=Genus&family=" + this.family() + "&genus=" + this.genus() + "&latinname=" + this.genus();
        } else if (this.rank() == "Species") {
            queryString = "rank=Species&family=" + this.family() + "&genus=" + this.genus() + "&species=" + 
                this.species() + "&latinname=" + this.genus() + " " + this.species() + 
                "&authorship=" + encodeURIComponent(this.author());
        }
        $.ajax({
            url: traceEndpoint + queryString,
            type: "GET"
        }).done(data => {
                if (data.length == 1 && data[0].TaxonomicStatus == "accepted") this.currentTaxon(data[0].Id);
                this.newSlideTaxonStatus(data);
            })
    }
    
    public getTaxonIdIfValid() {
        if (this.currentTaxon() != null) return this.currentTaxon();
        return null;
    }
    
    public capitaliseFirstLetter = element => {
        const currentValue = $(element).val();
        if (typeof(currentValue) == "string") {
            $(element).val(this.capitaliseString(currentValue));
        }
    }

    public suggest(entryBox:HTMLInputElement, rank:string) {
        window.clearTimeout(this.typingTimer);
        if (entryBox.value) {
            this.typingTimer = window.setTimeout(() => {
                this.rateLimitedSuggestionList(entryBox, rank);
            }, this.doneTypingInterval);
        }
    }

    // Hides the dropdown menu when the user navigates away
    // from the input box
    public disable(rank) {
        let element;
        if (rank == 'Family') element = 'FamilyList';
        if (rank == 'Genus') element = 'GenusList';
        if (rank == 'Species') element = 'SpeciesList';
        function fade() { $('#' + element).fadeOut(); }
        setTimeout(fade, 100);
    }

    // Renders a suggestion list for a taxonomic rank depending
    // on a rate-limiting timeout
    public rateLimitedSuggestionList(entryBox:HTMLInputElement, rank:string) {
        let query = '';
        let value = entryBox.value;
        if (rank == "Family" || rank == "Genus") {
            value = this.capitaliseString(value);
        }
        //Combine genus and species for canonical name
        if (rank == 'Species') {
            const genus = (<HTMLInputElement>document.getElementById('original-Genus')).value;
            query += genus + " ";
        }
        query += value;
        if (value != "") {
            const request = searchEndpoint + "rank=" + rank + "&latinName=" + query;
            $.ajax({
                url: request,
                type: "GET"
            }).done(data => {
                const list = document.getElementById(rank + 'List');
                $('#' + rank + 'List').css('display', 'block');
                list.innerHTML = "";
                for (let i = 0; i < data.length; i++) {
                    if (i > 10) continue;
                    const option = document.createElement('li');
                    const link = document.createElement('a');
                    option.appendChild(link);
                    link.innerHTML = data[i];

                    let matchCount = 0;
                    for (let j = 0; j < data.length; j++) {
                        if (data[j].latinName == data[i]) {
                            matchCount++;
                        }
                    }
                    link.addEventListener('click', e => {
                        const name = link.innerHTML;
                        if (rank == 'Species') {
                            $('#original-Species').val(name.split(' ')[1]).change();
                            $('#original-Genus').val(name.split(' ')[0]).change();
                        } else if (rank == 'Genus') {
                            $('#original-Genus').val(name).change();
                        } else if (rank == 'Family') {
                            $('#original-Family').val(name).change();
                        }
                        $('#' + rank + 'List').fadeOut();
                    });
                    list.appendChild(option);
                }
            });
        }
    }

    capitaliseString(string:string) {
        return string.charAt(0).toUpperCase() + string.slice(1);
    }

}