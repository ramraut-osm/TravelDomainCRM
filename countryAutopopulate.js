var Sdk = window.Sdk || {};

(function () {
    this.onHotelSelection = function (executionContext) {
        var formContext = executionContext.getFormContext();

        var placeField = formContext.getAttribute("tr_place");
        if (placeField && placeField.getValue() && placeField.getValue()[0] && placeField.getValue()[0].id) {
            var placeId = placeField.getValue()[0].id;
            console.log('place id :' + placeId)

            Xrm.WebApi.retrieveRecord("tr_places", placeId, "?$select=_tr_country_value").then(
                function success(result) {
          
                    var countryId = result["_tr_country_value"];

                    if (countryId) {
                        formContext.getAttribute("tr_country").setValue([{ id: countryId, entityType: "tr_country" }]);
                    }

                },
                function error(error) {
                    console.log("Error retrieving place information: " + error.message);
                }
            );
        } else {
            console.log("place field is not selected.");
            formContext.getAttribute("tr_country").setValue(null);
        }

    };
}).call(Sdk);
