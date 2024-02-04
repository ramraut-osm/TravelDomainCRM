var Sdk = window.Sdk || {};

(function () {
    this.onInvoiceDiscount = function (executionContext) {
        var formContext = executionContext.getFormContext();

        var discountField = Xrm.Page.getAttribute("tr_discountplan");

        if (discountField && discountField.getValue()!== null && discountField.getValue()[0] && discountField.getValue()[0].id) {
            var discountId = discountField.getValue()[0].id;
            console.log('discountId is :' + discountId)

            Xrm.WebApi.retrieveRecord("tr_discounts", discountId, "?$select=tr_discountamount").then(
                function success(result) {
                    var discountAmount = result.tr_discountamount;
                    console.log('discountAmount :' + discountAmount)

                    Xrm.Page.getAttribute("tr_discountapplied").setValue(discountAmount);

                    var bookingCost = Xrm.Page.getAttribute("tr_bookingcost").getValue();
                    var vatRate = Xrm.Page.getAttribute("tr_vatratenew").getValue();

                    if (bookingCost !== null && vatRate !== null) {
                        var totalCost = bookingCost - discountAmount + (bookingCost * (vatRate / 100));
                        Xrm.Page.getAttribute("tr_total").setValue(totalCost);
                    }
                },
                function error(error) {
                    console.log("Error retrieving discount information: " + error.message);
                }
            );
        } else {
            console.log("Discount field is not selected.");
            Xrm.Page.getAttribute("tr_discountapplied").setValue(null);

            var bookingCost = Xrm.Page.getAttribute("tr_bookingcost").getValue() || 0;
            var vatRate = Xrm.Page.getAttribute("tr_vatratenew").getValue() || 0;

            if (bookingCost !== null && vatRate !== null) {
                var totalCost = bookingCost + (bookingCost * (vatRate / 100));
                Xrm.Page.getAttribute("tr_total").setValue(totalCost);
            }
        }
    };
}).call(Sdk);
