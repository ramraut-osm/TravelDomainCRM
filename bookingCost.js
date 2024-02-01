
var Sdk = window.Sdk || {};

(function () {
    this.onCostBooking = function (executionContext) {
        var formContext = executionContext.getFormContext();

        var adultsChosen = formContext.getAttribute("tr_noofadults");
        var childrenChosen = formContext.getAttribute("tr_noofchildren");
        var daysChosen = formContext.getAttribute("tr_noofdays");
        var packageLookup = formContext.getAttribute("tr_packagename");

        if (adultsChosen.getValue() !== null && childrenChosen.getValue() !== null && daysChosen.getValue() !== null && packageLookup.getValue() !== null) {

            var packageId = packageLookup.getValue()[0].id;

            Xrm.WebApi.retrieveRecord("tr_package", packageId, "?$select=tr_packagecostpercouple,tr_packagecostperchild,tr_packagecostperday").then(
                function success(result) {

                    console.log("adultsChosen is :" + adultsChosen);
                    console.log("childrenChosen is :" + childrenChosen);
                    console.log("daysChosen is :" + daysChosen);

                    var adultRate = result.tr_packagecostpercouple;
                    var childrenRate = result.tr_packagecostperchild;
                    var daysRate = result.tr_packagecostperday;
                    console.log("adultRate is :" + adultRate);
                    console.log("childrenRate is :" + childrenRate);
                    console.log("daysRate is :" + daysRate);

                    var currentAdultsValue = adultsChosen.getValue();
                    var currentChildrenValue = childrenChosen.getValue();
                    var currentDaysValue = daysChosen.getValue();

                    var adultsCost = currentAdultsValue * adultRate;
                    var childrenCost = currentChildrenValue * childrenRate;
                    var daysCost = currentDaysValue * daysRate;
                    var totalCost = adultsCost + childrenCost + daysCost;

                    console.log("totalCost is :" + totalCost)

                    formContext.getAttribute("tr_cost").setValue(totalCost);
                },  
            function error(error) {
                    console.error("Error occurred while retrieving package details: " + error.message);
                }

            );
        }
    }
}).call(Sdk);


