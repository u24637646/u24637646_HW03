// ==================================================================================
// _MaintainPanelScripts.js - Search, Display, Edit/Delete Logic for Panels
// ==================================================================================

// Global map declared in Maintain.cshtml: entityDataMap and currentDisplayId

// ------------------------------------------------------------------
// NEW: Function to handle selection from the dropdown
// ------------------------------------------------------------------
function selectEntityByName(selectElement, entity) {
    var id = selectElement.value;

    // Clear search input if dropdown is used
    $('#' + entity + '-search-input').val('');

    if (parseInt(id) > 0) {
        var data = entityDataMap[entity][id];
        updatePanelDisplay(entity, data);
    } else {
        // Option "--- Select Entity ---" selected: show no data or placeholder
        $('#' + entity + '-detail-container').html('<p class="text-info">Select a record from the dropdown or use the search box.</p>');
        $('#' + entity + '-current-id').val(0);
    }
}

// ------------------------------------------------------------------
// 1. Function to Dynamically Update Panel Display (Includes Product Image)
// ------------------------------------------------------------------
function updatePanelDisplay(entity, data) {
    var panelBodyContainer = $('#' + entity + '-detail-container');

    if (!data || data.id === 0) {
        panelBodyContainer.html('<p class="text-danger">No valid record found matching the search criteria.</p>');
        $('#' + entity + '-current-id').val(0);
        return;
    }

    // Update the currently displayed ID tracker
    $('#' + entity + '-current-id').val(data.id);

    // Build the new HTML content
    var htmlContent = '<h4 class="text-center">' + data.name + '</h4>';

    if (entity === 'staff') {
        htmlContent += '<p>Email: <strong>' + data.email + '</strong></p>' +
            '<p>Phone: <strong>' + data.phone + '</strong></p>' +
            '<p>Store: <strong>' + data.store + '</strong></p>' +
            '<p>Manager: <strong>' + data.manager + '</strong></p>';

    } else if (entity === 'customer') {
        htmlContent += '<p>Email: <strong>' + data.email + '</strong></p>' +
            '<p>Phone: <strong>' + data.phone + '</strong></p>' +
            '<p>Location: <strong>' + data.city + ', ' + data.state + '</strong></p>' +
            '<p>Street: <strong>' + data.street + '</strong></p>' +
            '<p>Zip: <strong>' + data.zip + '</strong></p>';

    } else if (entity === 'product') {
        // Include Image Container
        htmlContent += '<div class="image-container">' +
            '<img id="product-image" src="' + data.imageUrl + '" alt="' + data.name + '" style="max-width: 100%; height: auto; margin-bottom: 10px;" onerror="this.onerror=null;this.src=\'/Images/placeholder.jpeg\';" />' + // Added error fallback
            '</div>';

        htmlContent += '<p>Brand: <strong>' + data.brand + '</strong></p>' +
            '<p>Category: <strong>' + data.category + '</strong></p>' +
            '<p>Model Year: <strong>' + data.year + '</strong></p>' +
            '<p>Price: <strong>' + data.price + '</strong></p>' +
            '<p>Total Stock: <strong>' + data.stock + '</strong></p>';
    }

    htmlContent += '<hr /><h5 class="text-info">Use the search box below or the selector above.</h5>';
    htmlContent += '<input type="hidden" id="' + entity + '-current-id" value="' + data.id + '" />';

    // Replace the content
    panelBodyContainer.html(htmlContent).show();
}


// ------------------------------------------------------------------
// 2. Function to Filter/Search and Display (Clears dropdown upon search)
// ------------------------------------------------------------------
function filterAndDisplay(entity) {
    var searchTerm = $('#' + entity + '-search-input').val().trim().toLowerCase();

    // Clear the dropdown when search is performed
    $('#' + entity + '-name-selector').val('0');

    var foundData = null;

    if (!searchTerm) {
        alert("Please enter a name or ID to search.");
        return;
    }

    // Check if search term is a number (ID lookup)
    var searchId = parseInt(searchTerm);
    if (!isNaN(searchId) && searchId > 0) {
        foundData = entityDataMap[entity][searchId];
    } else {
        // Search by name (simple substring match)
        for (var id in entityDataMap[entity]) {
            if (entityDataMap[entity].hasOwnProperty(id)) {
                var item = entityDataMap[entity][id];
                if (item.name && item.name.toLowerCase().includes(searchTerm)) {
                    foundData = item;
                    break;
                }
            }
        }
    }

    if (foundData) {
        updatePanelDisplay(entity, foundData);
    } else {
        updatePanelDisplay(entity, { id: 0 }); // Call display function with null data
    }
}


// ------------------------------------------------------------------
// 3. Function to Load Maintenance Modal (Edit/Details)
// ------------------------------------------------------------------
function loadMaintenanceModal(entity, action) {
    var dbId = $('#' + entity + '-current-id').val();

    if (parseInt(dbId) <= 0) {
        alert("No valid record is currently selected for " + action + ". Please select a record first.");
        return;
    }

    var entityTitle = entity.charAt(0).toUpperCase() + entity.slice(1);
    var modalId = '#maintain' + entityTitle + 'Modal';
    var url = '/' + entityTitle + 's/' + action + '/' + dbId; // e.g., /Staffs/Edit/5

    // Update Modal Title
    $('#maintain' + entityTitle + 'ModalTitle').text(entityTitle + ' ' + action);

    $.ajax({
        url: url,
        type: 'GET',
        cache: false,
        success: function (data) {
            $(modalId + 'Body').html(data);
            $(modalId).modal('show');

            // Re-parse validation for forms loaded via AJAX
            $.validator.unobtrusive.parse(modalId + 'Body');
        },
        error: function () {
            alert("Could not load " + action + " details for " + entityTitle + " ID: " + dbId);
        }
    });
}


// ------------------------------------------------------------------
// 4. Function to Confirm and Delete
// ------------------------------------------------------------------
function confirmAndDelete(entity) {
    var id = $('#' + entity + '-current-id').val();
    var name = $('#' + entity + '-detail-container h4').text();

    if (parseInt(id) <= 0) {
        alert("No valid record is currently selected to delete.");
        return;
    }

    var entityTitle = entity.charAt(0).toUpperCase() + entity.slice(1);

    if (confirm("Are you sure you want to permanently delete " + entityTitle + ": " + name + " (ID: " + id + ")? This action cannot be undone.")) {

        var url = '/' + entityTitle + 's/DeleteConfirmed/' + id;

        $.ajax({
            url: url,
            type: 'POST',
            data: { id: id },
            dataType: 'json',
            success: function (result) {
                if (result.success) {
                    alert(entityTitle + " deleted successfully. Refreshing the dashboard.");
                    // Remove item from client-side map
                    delete entityDataMap[entity][id];
                    // Redirect or reload the page to update the panel content
                    window.location.href = result.redirectUrl || '/Home/Maintain';
                } else {
                    alert("Error deleting " + entityTitle + ": " + (result.message || "An unknown error occurred."));
                }
            },
            error: function (xhr) {
                alert("An unexpected server error occurred during deletion.");
            }
        });
    }
}


// ------------------------------------------------------------------
// 5. General Document Ready Setup
// ------------------------------------------------------------------
$(document).ready(function () {

    // Bind Enter keypress to the search function
    $('.search-group input[type="text"]').keypress(function (e) {
        if (e.which === 13) {
            e.preventDefault();
            var entity = $(this).attr('id').split('-')[0];
            filterAndDisplay(entity);
        }
    });

    // Handle AJAX Form Submissions from Edit Modals
    // IMPORTANT: Your modal partial forms must have the class 'ajax-maintain-form' 
    // and the data-entity-type attribute (e.g., data-entity-type="staff")
    $(document).on('submit', 'form.ajax-maintain-form', function (e) {
        e.preventDefault();
        var form = $(this);
        var type = form.data('entity-type');

        if (!form.valid()) return false;

        $.ajax({
            url: form.attr('action'),
            type: form.attr('method'),
            data: form.serialize(),
            dataType: 'json',
            success: function (result) {
                if (result.success) {
                    // Hide the modal
                    $('#maintain' + type.charAt(0).toUpperCase() + type.slice(1) + 'Modal').modal('hide');

                    // Reload the maintain page to update all panels and map
                    window.location.href = result.redirectUrl || '/Maintain/Index';

                } else {
                    alert('Action failed: ' + (result.message || 'Please check the input.'));
                }
            },
            error: function (xhr) {
                // If the controller returns the partial view with validation errors
                if (xhr.status === 200 && xhr.responseText.indexOf('form-group') > -1) {
                    // Reload the form content with validation messages
                    $('#maintain' + type.charAt(0).toUpperCase() + type.slice(1) + 'ModalBody').html(xhr.responseText);
                    $.validator.unobtrusive.parse('#maintain' + type.charAt(0).toUpperCase() + type.slice(1) + 'ModalBody');
                } else {
                    alert("An unexpected error occurred during the maintenance action.");
                }
            }
        });
        return false;
    });

    // Clean up modal content when hidden
    $('.modal').on('hidden.bs.modal', function () {
        $(this).find('.modal-body').empty();
    });
});