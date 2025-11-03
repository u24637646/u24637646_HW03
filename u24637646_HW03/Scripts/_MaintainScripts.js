// Maintenance Panel Scripts - handles search, display, edit, and delete operations for entities
// Global variables declared in Maintain.cshtml: entityDataMap and currentDisplayId

// Handle dropdown selection to display entity details
function selectEntityByName(selectElement, entity) {
    var id = selectElement.value;

    // Clear the search input when using dropdown
    $('#' + entity + '-search-input').val('');

    if (parseInt(id) > 0) {
        var data = entityDataMap[entity][id];
        updatePanelDisplay(entity, data);
    } else {
        // Show placeholder when "Select Entity" option is chosen
        $('#' + entity + '-detail-container').html('<p class="text-info">Select a record from the dropdown or use the search box.</p>');
        $('#' + entity + '-current-id').val(0);
    }
}

// Update the panel display with entity data
function updatePanelDisplay(entity, data) {
    var panelBodyContainer = $('#' + entity + '-detail-container');

    if (!data || data.id === 0) {
        panelBodyContainer.html('<p class="text-danger">No valid record found matching the search criteria.</p>');
        $('#' + entity + '-current-id').val(0);
        return;
    }

    // Update the tracker for currently displayed ID
    $('#' + entity + '-current-id').val(data.id);

    // Build HTML content based on entity type
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
        // Include product image in the display
        htmlContent += '<div class="image-container">' +
            '<img id="product-image" src="' + data.imageUrl + '" alt="' + data.name + '" ' +
            'style="max-width: 100%; height: auto; margin-bottom: 10px;" ' +
            'onerror="this.onerror=null;this.src=\'/Images/placeholder.jpeg\';" />' +
            '</div>';

        htmlContent += '<p>Brand: <strong>' + data.brand + '</strong></p>' +
            '<p>Category: <strong>' + data.category + '</strong></p>' +
            '<p>Model Year: <strong>' + data.year + '</strong></p>' +
            '<p>Price: <strong>' + data.price + '</strong></p>' +
            '<p>Total Stock: <strong>' + data.stock + '</strong></p>';
    }

    // Update the panel content
    panelBodyContainer.html(htmlContent).show();
}

// Search and display entity by name or ID
function filterAndDisplay(entity) {
    var searchTerm = $('#' + entity + '-search-input').val().trim().toLowerCase();

    // Clear dropdown selection when using search
    $('#' + entity + '-name-selector').val('0');

    var foundData = null;

    if (!searchTerm) {
        alert("Please enter a name or ID to search.");
        return;
    }

    // Check if search term is numeric (ID lookup)
    var searchId = parseInt(searchTerm);
    if (!isNaN(searchId) && searchId > 0) {
        foundData = entityDataMap[entity][searchId];
    } else {
        // Search by name (substring match)
        for (var id in entityDataMap[entity]) {
            if (entityDataMap[entity].hasOwnProperty(id)) {
                var item = entityDataMap[entity][id];
                if (item.name && item.name.toLowerCase().includes(searchTerm)) {
                    foundData = item;
                    break; // Use first match
                }
            }
        }
    }

    if (foundData) {
        updatePanelDisplay(entity, foundData);
    } else {
        // Display no results message
        updatePanelDisplay(entity, { id: 0 });
    }
}

// Load edit or details modal for maintenance
function loadMaintenanceModal(entity, action) {
    var dbId = $('#' + entity + '-current-id').val();

    if (parseInt(dbId) <= 0) {
        alert("No valid record is currently selected for " + action + ". Please select a record first.");
        return;
    }

    var entityTitle = entity.charAt(0).toUpperCase() + entity.slice(1);
    var modalId = '#maintain' + entityTitle + 'Modal';

    // 🚨 FIX: Correct URL to target the HomeController actions (e.g., /Home/EditStaff/1)
    var url = '/Home/' + action + entityTitle + '/' + dbId;

    // Update modal title
    $('#maintain' + entityTitle + 'ModalTitle').text(entityTitle + ' ' + action);

    // Load form content via AJAX
    $.ajax({
        url: url,
        type: 'GET',
        cache: false,
        success: function (data) {
            $(modalId + 'Body').html(data);
            $(modalId).modal('show');

            // Re-parse validation for dynamically loaded forms
            $.validator.unobtrusive.parse(modalId + 'Body');
        },
        error: function () {
            alert("Could not load " + action + " details for " + entityTitle + " ID: " + dbId);
        }
    });
}

// Confirm and delete an entity record
function confirmAndDelete(entity) {
    var id = $('#' + entity + '-current-id').val();
    var name = $('#' + entity + '-detail-container h4').text();

    if (parseInt(id) <= 0) {
        alert("No valid record is currently selected to delete.");
        return;
    }

    var entityTitle = entity.charAt(0).toUpperCase() + entity.slice(1);

    if (confirm("Are you sure you want to permanently delete " + entityTitle + ": " + name + " (ID: " + id + ")? This action cannot be undone.")) {

        // 🚨 FIX: Correct URL to target the HomeController actions (e.g., /Home/DeleteStaff)
        var url = '/Home/Delete' + entityTitle;

        // 🚨 FIX: Include Anti-Forgery Token for [ValidateAntiForgeryToken]
        var token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: url,
            type: 'POST',
            data: {
                id: id,
                '__RequestVerificationToken': token // Pass the token with the ID
            },
            dataType: 'json',
            success: function (result) {
                if (result.success) {
                    alert(entityTitle + " deleted successfully. Refreshing the dashboard.");

                    // Remove from client-side data map
                    delete entityDataMap[entity][id];

                    // Redirect to refresh the page (now using the redirectUrl returned from C#)
                    window.location.href = result.redirectUrl || '/Home/Maintain';
                } else {
                    alert("Error deleting " + entityTitle + ": " + (result.message || "An unknown error occurred."));
                }
            },
            error: function (xhr) {
                alert("An unexpected server error occurred during deletion or validation.");
            }
        });
    }
}

// Document ready setup
$(document).ready(function () {

    // Bind Enter key to search function
    $('.search-group input[type="text"]').keypress(function (e) {
        if (e.which === 13) { // Enter key
            e.preventDefault();
            var entity = $(this).attr('id').split('-')[0];
            filterAndDisplay(entity);
        }
    });

    // Handle AJAX form submissions from edit modals
    // Forms must have class 'ajax-maintain-form' and data-entity-type attribute
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
                    // Hide modal
                    $('#maintain' + type.charAt(0).toUpperCase() + type.slice(1) + 'Modal').modal('hide');

                    // Reload maintain page to update all panels
                    // This now uses the result.redirectUrl which is set in the updated C# controller
                    window.location.href = result.redirectUrl || '/Home/Maintain';

                } else {
                    alert('Action failed: ' + (result.message || 'Please check the input.'));
                }
            },
            error: function (xhr) {
                // If controller returns partial view with validation errors
                if (xhr.status === 200 && xhr.responseText.indexOf('form-group') > -1) {
                    // Reload form content with validation messages
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