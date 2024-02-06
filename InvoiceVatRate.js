var Sdk = window.Sdk || {};

(function () {
    this.onInvoiceVatRate = function (executionContext) {
        var formContext = executionContext.getFormContext();

        var bookingCost = formContext.getAttribute("tr_bookingcost").getValue();
        var discountAmount = formContext.getAttribute("tr_discountapplied").getValue() || 0;
        var vatRate = formContext.getAttribute("tr_vatratenew").getValue();

        if (bookingCost !== null) {
            var totalCost = bookingCost - discountAmount + (bookingCost * (vatRate / 100));

            formContext.getAttribute("tr_total").setValue(totalCost);
        }
    };
}).call(Sdk);
