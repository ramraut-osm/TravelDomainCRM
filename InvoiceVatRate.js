var Sdk = window.Sdk || {};

(function () {
    this.onInvoiceVatRate = function (executionContext) {
        var formContext = executionContext.getFormContext();

        var bookingCost = Xrm.Page.getAttribute("tr_bookingcost").getValue();
        var discountAmount = Xrm.Page.getAttribute("tr_discountapplied").getValue() || 0;
        var vatRate = Xrm.Page.getAttribute("tr_vatratenew").getValue();

        if (bookingCost !== null) {
            var totalCost = bookingCost - discountAmount + (bookingCost * (vatRate / 100));

            Xrm.Page.getAttribute("tr_total").setValue(totalCost);
        }
    };
}).call(Sdk);
