// This script manages the maintenance dashboard, handling selection, search, and modal actions (Edit/Delete) for all entities.

// --- Data Selection and Display ---

// Updates the detail panel when a user selects an item from the entity dropdown list.
function selectEntityByName(selectElement, entity) {
    var id = selectElement.value;

    // Clear the search box to keep the UI clean and consistent.
    $('#' + entity + '-search-input').val('');

    if (parseInt(id) > 0) {
        var data = entityDataMap[entity][id];
        updatePanelDisplay(entity, data);

        // Store the ID of the currently selected record for use in Edit/Delete actions.
        $('#' + entity + '-current-id').val(id);
    } else {
        // Display a guidance message when no specific entity is selected.
        $('#' + entity + '-detail-container').html('<p class="text-info">Select a record from the dropdown or use the search box.</p>');
        $('#' + entity + '-current-id').val(0);
    }
}

/**
 * Displays pre-loaded entity data in the corresponding panel on the dashboard.
 */
function updatePanelDisplay(entity, data) {
    var panelBodyContainer = $('#' + entity + '-detail-container');
    var htmlContent = '';

    if (!data || data.id === 0) {
        panelBodyContainer.html('<p class="text-danger">No valid record found matching the search criteria.</p>');
        $('#' + entity + '-current-id').val(0);
        return;
    }

    // Update the hidden input that tracks the currently displayed record ID.
    $('#' + entity + '-current-id').val(data.id);

    // Dynamically generate the HTML structure to display the entity's properties.
    if (entity === 'staff') {
        htmlContent = `
            <h4>${data.name} <small>(ID: ${data.id})</small></h4>
            <ul class="list-unstyled detail-list">
                <li><i class="glyphicon glyphicon-envelope"></i> ${data.email}</li>
                <li><i class="glyphicon glyphicon-phone"></i> ${data.phone || 'N/A'}</li>
                <li><i class="glyphicon glyphicon-tag"></i> Store: **${data.store}**</li>
                <li><i class="glyphicon glyphicon-king"></i> Manager: ${data.manager}</li>
            </ul>
        `;
    } else if (entity === 'customer') {
        htmlContent = `
            <h4>${data.name} <small>(ID: ${data.id})</small></h4>
            <ul class="list-unstyled detail-list">
                <li><i class="glyphicon glyphicon-envelope"></i> ${data.email}</li>
                <li><i class="glyphicon glyphicon-phone"></i> ${data.phone || 'N/A'}</li>
                <li class="address"><i class="glyphicon glyphicon-home"></i> ${data.street}</li>
                <li class="address"><i class="glyphicon glyphicon-map-marker"></i> ${data.city}, ${data.state} ${data.zip}</li>
            </ul>
        `;
    } else if (entity === 'product') {
        htmlContent = `
            <img src="${data.imageUrl}" alt="${data.name}" class="img-responsive product-image center-block" onerror="this.src='/Images/default.jpeg';">
            <h4>${data.name} <small>(ID: ${data.id})</small></h4>
            <ul class="list-unstyled detail-list">
                <li><i class="glyphicon glyphicon-barcode"></i> Brand: **${data.brand}**</li>
                <li><i class="glyphicon glyphicon-th-list"></i> Category: ${data.category}</li>
                <li><i class="glyphicon glyphicon-calendar"></i> Year: ${data.year}</li>
                <li><i class="glyphicon glyphicon-usd"></i> Price: **${data.price}**</li>
                <li><i class="glyphicon glyphicon-tasks"></i> Stock: ${data.stock}</li>
            </ul>
        `;
    }

    panelBodyContainer.html(htmlContent);
}

// Filters the entity display based on text entered in the search input (matching name or ID).
function filterAndDisplay(entity) {
    var searchText = $('#' + entity + '-search-input').val().toLowerCase();
    var foundData = null;

    if (!searchText) {
        // If the search text is cleared, default the display back to the first record.
        var firstId = Object.keys(entityDataMap[entity])[0];
        foundData = entityDataMap[entity][firstId];
    } else {
        // Loop through the locally stored data to find a name or ID match for the search text.
        $.each(entityDataMap[entity], function (id, data) {
            if (data.name.toLowerCase().includes(searchText) || data.id.toString() === searchText) {
                foundData = data;

                // Exit the search loop once the first match is found.
                return false;
            }
        });
    }

    // Reset the dropdown selector to the ID of the item found via search.
    $('#' + entity + '-name-selector').val(foundData ? foundData.id : 0);

    // Call the display function to show the results of the search.
    updatePanelDisplay(entity, foundData);
}


// --- Modal Interaction and Server Communication ---

/**
 * Sets up the modal with the correct title and fetches the partial view for Edit or Delete actions.
 */
function loadMaintenanceModal(type, action) {
    var entityId = $('#' + type + '-current-id').val();

    // Convert the entity type string to a proper Casing for controller action names.
    var entityName = type.charAt(0).toUpperCase() + type.slice(1);
    var modalId = '#maintain' + entityName + 'Modal';
    var modalBodyId = '#maintain' + entityName + 'ModalBody';
    var modalTitleId = '#maintain' + entityName + 'ModalTitle';

    if (parseInt(entityId) === 0) {
        alert('Please select a valid ' + type + ' record before performing an action.');
        return;
    }

    // Build the server-side URL endpoint to fetch the form content.
    var url = '/Home/' + entityName + action + 'Partial?id=' + entityId;

    // Change the modal's header text to reflect the action being taken (Edit or Delete).
    $(modalTitleId).text(action + ' ' + entityName + ' Record');

    // Display a loading message and then fetch the form content from the server.
    $(modalBodyId).html('<div class="text-center"><p><i class="glyphicon glyphicon-refresh spinning"></i> Loading...</p></div>');
    $(modalBodyId).load(url, function (response, status, xhr) {
        if (status === "error") {
            // Handle server errors that occur during the fetch operation.
            $(modalBodyId).html('<p class="text-danger">Error loading form: ' + xhr.status + ' ' + xhr.statusText + '</p>');
        } else {
            // Re-parse validation rules since the form content was loaded dynamically.
            $.validator.unobtrusive.parse(modalBodyId);

            // Display the modal to the user.
            $(modalId).modal('show');
        }
    });
}

/**
 * Presents a confirmation dialog before proceeding with the delete action.
 */
function confirmAndDelete(type) {
    var entityId = $('#' + type + '-current-id').val();
    var entityName = type.charAt(0).toUpperCase() + type.slice(1);
    var currentData = entityDataMap[type][entityId];
    var displayName = currentData ? (currentData.name || currentData.product_name) : entityId;

    if (parseInt(entityId) === 0) {
        alert('Please select a valid ' + type + ' record before attempting to delete.');
        return;
    }

    if (confirm(`Are you sure you want to delete the ${type} record: ${displayName} (ID: ${entityId})?`)) {
        // If the user confirms deletion, load the final Delete form into the modal.
        loadMaintenanceModal(type, 'Delete');
    }
}


// --- Form Submission Handler ---

$(document).ready(function () {
    // Catches the submit event for all forms that are dynamically loaded into the maintenance modals.
    $('.modal').on('submit', 'form', function (e) {
        e.preventDefault();
        var form = $(this);

        // Extract the entity type from the form's data attribute.
        var type = form.data('entity-type');
        var action = form.attr('action');

        // Prevent form submission if client-side validation fails.
        if (!form.valid()) {
            return;
        }

        $.ajax({
            url: action,
            type: 'POST',
            data: form.serialize(),
            success: function (result) {
                if (result.success) {

                    // Close the modal window upon successful server action.
                    $('#maintain' + type.charAt(0).toUpperCase() + type.slice(1) + 'Modal').modal('hide');

                    // Redirect to refresh the page and display the successful operation message.
                    window.location.href = result.redirectUrl || '/Home/Maintain';

                } else {
                    // Display a friendly error message returned from the controller on failure.
                    alert('Action failed: ' + (result.message || 'Please check the input.'));
                }
            },
            error: function (xhr) {
                // Handles server-side validation errors where the server returns the form partial view with error messages.
                if (xhr.status === 200 && xhr.responseText.includes('form-group')) {

                    // Replace the modal body content with the form that includes validation errors.
                    $('#maintain' + type.charAt(0).toUpperCase() + type.slice(1) + 'ModalBody').html(xhr.responseText);

                    // Ensure validation rules are active for the newly rendered form.
                    $.validator.unobtrusive.parse('#maintain' + type.charAt(0).toUpperCase() + type.slice(1) + 'ModalBody');
                } else {
                    // General error handling
                    alert("An unexpected error occurred during the maintenance action.");
                }
            }
        });
        return false;
    });

    // Clean up modal content when hidden
    $('.modal').on('hidden.bs.modal', function () {
        // Clear the modal's content after it is closed to avoid seeing old data.
        $(this).find('.modal-body').empty();
    });
});