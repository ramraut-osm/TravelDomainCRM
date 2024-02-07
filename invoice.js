var Sdk = window.Sdk || {};

(function () {
    this.onInvoiceBooking = function (executionContext) {
        var formContext = executionContext.getFormContext();

        var bookingField = formContext.getAttribute("tr_bookingplan"); 
        if (bookingField && bookingField.getValue() && bookingField.getValue()[0] && bookingField.getValue()[0].id) {
            var bookingId = bookingField.getValue()[0].id;
            console.log('booking id :' + bookingId)

            Xrm.WebApi.retrieveRecord("tr_bookings", bookingId, "?$select=_tr_primarycontact_value,tr_cost").then(
                function success(result) {

                    var bookingCost = result.tr_cost;
                    console.log('bookingCost is:' + bookingCost);
                    formContext.getAttribute("tr_bookingcost").setValue(bookingCost);

                    var primaryContactId = result["_tr_primarycontact_value"];
                    console.log('primaryContactId is:' + primaryContactId);

                    if (primaryContactId) {
                        formContext.getAttribute("tr_primarycontact").setValue([{ id: primaryContactId, entityType: "contact" }]);
                        //formContext.getAttribute("tr_name").setValue('INV-' + primaryContactId);
                    }

                    var primaryContact = formContext.getAttribute("tr_primarycontact").getValue()[0];
                    var fullName = primaryContact.name;
                    formContext.getAttribute("tr_name").setValue('INV-' + fullName);

                },
                function error(error) {
                    console.log("Error retrieving booking information: " + error.message);
                }
            );
        } else {
            console.log("Booking field is not selected.");
            formContext.getAttribute("tr_bookingcost").setValue(null);
            formContext.getAttribute("tr_discountplan").setValue(null);
            formContext.getAttribute("tr_discountapplied").setValue(null);
            formContext.getAttribute("tr_vatratenew").setValue(null);
            formContext.getAttribute("tr_total").setValue(null);          
        }
        
    };
}).call(Sdk);
