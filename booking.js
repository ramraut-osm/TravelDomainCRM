var Sdk = window.Sdk || {};

(function () {
    this.onSaveBooking = function (executionContext) {
        var formContext = executionContext.getFormContext();

         
        















        var adultsChosen = formContext.getAttribute("tr_noofadults");
        var childrenChosen = formContext.getAttribute("tr_noofchildren");
        var daysChosen = formContext.getAttribute("tr_noofdays");
        var packageLookup = formContext.getAttribute("tr_packagename"); 

        if (adultsChosen.getValue() !== null && childrenChosen.getValue() !== null && daysChosen.getValue() !== null && packageLookup.getValue() !== null) {
            
            var packageId = packageLookup.getValue()[0].id;

            Xrm.WebApi.retrieveRecord("tr_package", packageId, "?$select=tr_noofadultsallowed,tr_noofchildrenallowed,tr_maximumnoofdays").then(
                function success(result) {

                    var maxAdultsAllowed = result.tr_noofadultsallowed;
                    var maxChildrenAllowed = result.tr_noofchildrenallowed;
                    var maxDaysAllowed = result.tr_maximumnoofdays;

                    var currentAdultsValue = adultsChosen.getValue();
                    var currentChildrenValue = childrenChosen.getValue();
                    var currentDaysValue = daysChosen.getValue();

                    console.log("maxAdultsAllowed" + maxAdultsAllowed)
                   
                    if (currentAdultsValue > maxAdultsAllowed) {                      
                        alert("Number of adults exceeds the maximum allowed adults " + maxAdultsAllowed + " for the chosen package.");
                        executionContext.getEventArgs().preventDefault();
                    } else if (currentChildrenValue > maxChildrenAllowed) {                       
                        alert("Number of children exceeds the maximum allowed children " + maxChildrenAllowed + " for the chosen package.");
                        executionContext.getEventArgs().preventDefault();
                    } else if( currentDaysValue > maxDaysAllowed) {                      
                        alert("Number of days exceeds the maximum allowed days " + maxDaysAllowed + " for the chosen package.");
                        executionContext.getEventArgs().preventDefault();
                    }
                },
                function error(error) {
                    console.error("Error occurred while retrieving package details: " + error.message);
                }
            );
        }
    };
}).call(Sdk);
