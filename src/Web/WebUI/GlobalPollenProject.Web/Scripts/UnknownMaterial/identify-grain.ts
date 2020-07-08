import { BotanicalLookupToolViewModel } from "../Components/botanical-lookup";
import * as ko from 'knockout';

export class IdentifyViewModel extends BotanicalLookupToolViewModel {
    
    constructor() {
        super();
        this.rank("Genus");
    }
    
    public validateAndSubmit() {
        this.requestValidation();
        this.currentTaxon.subscribe(function(newValue) {
            if (newValue != null) { (<HTMLFormElement>document.getElementById("identify-form")).submit(); }
        });
    }
}

export function activate(_: HTMLElement) {
    $(() => {
        ko.applyBindings(new IdentifyViewModel());
    })
}