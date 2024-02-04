var Sdk = window.Sdk || {};

(function () {
    this.onInvoiceBooking = function (executionContext) {
        var formContext = executionContext.getFormContext();

        var bookingField = Xrm.Page.getAttribute("tr_bookingplan"); 
        if (bookingField && bookingField.getValue() && bookingField.getValue()[0] && bookingField.getValue()[0].id) {
            var bookingId = bookingField.getValue()[0].id;
            console.log('booking id :' + bookingId)

            Xrm.WebApi.retrieveRecord("tr_bookings", bookingId, "?$select=_tr_primarycontact_value,tr_cost").then(
                function success(result) {
                   
                    var bookingCost = result.tr_cost;
                    console.log('bookingCost is:' + bookingCost);
                    Xrm.Page.getAttribute("tr_bookingcost").setValue(bookingCost);

                    var primaryContactId = result["_tr_primarycontact_value"];
                    console.log('primaryContactId is:' + primaryContactId);


                    if (primaryContactId) {
                        Xrm.Page.getAttribute("tr_primarycontact").setValue([{ id: primaryContactId, entityType: "contact" }]);
                    }

                    //if (primaryContactId) {
                    //    var primaryContactEntityReference = [{
                    //        id: primaryContactId,
                    //        entityType: "contact"
                    //    }];

                    //    Xrm.Page.getAttribute("tr_primarycontact").setValue(primaryContactEntityReference);
                    //}
                },
                function error(error) {
                    console.log("Error retrieving booking information: " + error.message);
                }
            );
        }

        
    };
}).call(Sdk);
