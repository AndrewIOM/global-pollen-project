function IdentifyViewModel() {
    var self = this;
    BotanicalLookupToolViewModel.call(self);
    self.rank() == "Genus";

    self.validateAndSubmit = function () {
        self.validateTaxon();
        self.currentTaxon.subscribe(function(newValue) {
            if (newValue != null) { document.getElementById("identify-form").submit(); }
        });
    }
};
ko.applyBindings(new IdentifyViewModel());