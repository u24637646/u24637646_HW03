// This script manages the dashboard view, including the carousel and modal forms for creating staff and customers.

// ------------------------------------------------------------------
// Updates the 'View Details' link to point to the current record being displayed in the carousel.
// ------------------------------------------------------------------
function updateDetailsLink(type, dbId) {
    var $link = $('#' + type + '-details-link');
    if ($link.length > 0) {
        var currentHref = $link.attr('href');
        
        // Safely replace the ID part of the URL (assuming /Controller/Details/ID)
        // Ensure a fall-back if href is initially empty or just /Controller/Details
        var newHref = currentHref ? currentHref.replace(/\/[0-9]+$/, '/' + dbId) : ('/' + type + 's/Details/' + dbId);
        $link.attr('href', newHref);
    }
}

// ------------------------------------------------------------------
// Handles the primary navigation logic for moving between items in the carousels.
// ------------------------------------------------------------------
function navigate(type, direction) {
    var indexInput = $('#' + type + '-index');
    var currentIdx = parseInt(indexInput.val());
    
    // Get the total number of items available from a hidden input.
    var totalCount = parseInt($('#' + type + '-count').val());
    var lastIndex = totalCount - 1;

    if (totalCount === 0) return;

    var newIdx = currentIdx + direction;

    // --- Boundary and Wrapping Logic ---

    // If we move past the last item, wrap back to the beginning (index 0).
    if (newIdx > lastIndex) {
        newIdx = 0;
    }
    // If we move before the first item, wrap around to the end (the last index).
    else if (newIdx < 0) {
        newIdx = lastIndex;
    }

    // --- Navigation Execution ---
    $('#' + type + '-' + currentIdx).fadeOut(100, function () {
        var $newItem = $('#' + type + '-' + newIdx);
        $newItem.fadeIn(200);

        // Retrieve the unique database ID for the newly visible item.
        var dbId = $newItem.data(type + '-id');

        // Not needed for this iteration of the code.

        // Update the 'View Details' link's destination URL.
        updateDetailsLink(type, dbId);
    });

    // Save the new index so the next call knows where to start.
    indexInput.val(newIdx);
}

// ------------------------------------------------------------------
// Executes once the page is fully loaded, setting up event handlers and initial state.
// ------------------------------------------------------------------
$(document).ready(function () {

    // --- Modal Closing Logic ---
    // Universal handler to close any modal when its dedicated close button is clicked.
    $(document).on('click', '.modal-close-btn', function () {
        var $currentModal = $(this).closest('.modal');
        if ($currentModal.length) {
            $currentModal.modal('hide');
        }
    });

    // --- Initialization of Panels ---
    // Initial setup for the Staff, Customer, and Product carousels.
    ['staff', 'customer', 'product'].forEach(function (type) {
        var initialItem = $('#' + type + '-0');
        var totalCount = parseInt($('#' + type + '-count').val() || '0');

        // Skip initialization if no records are available for this entity.
        if (totalCount === 0) {
            return;
        }

        if (initialItem.length > 0) {
            var initialId = initialItem.data(type + '-id');

            if (initialId) {
                // Not needed for this iteration of the code.
                updateDetailsLink(type, initialId);
            }
        }
    });

    // --- Product Filter: Submit form on dropdown change ---
    // Automatically submits the product filter form whenever the brand or category dropdown value changes.
    $('#brand-filter, #category-filter').on('change', function () {
        $('.product-filter-form').submit();
    });

    // --- Staff Modal Logic ---
    // Handles the button click to load the Staff creation form into a modal via AJAX.
    $('#createStaffBtn').click(function () {
        $.ajax({
            url: '/Staffs/CreatePartial',
            type: 'GET',
            cache: false,
            success: function (data) {
                $('#staffModalBody').html(data);
                $('#createStaffModal').modal('show');
                
                // Re-parse the validation rules since the form content was loaded dynamically.
                $.validator.unobtrusive.parse('#staffModalBody');
            }
        });
    });

    // Universal submit handler for the staff creation form (delegated).
    $(document).on('submit', '#createStaffForm', function (e) {
        e.preventDefault();
        var form = $(this);
        
        // Stop execution if client-side validation detects errors.
        if (!form.valid()) return false;

        $.ajax({
            url: form.attr('action'),
            type: form.attr('method'),
            data: form.serialize(),
            dataType: 'json',
            success: function (result) {
                if (result.success) {
                    $('#createStaffModal').modal('hide');
                    window.location.href = result.redirectUrl;
                } else {
                    if (result.html) {
                        $('#staffModalBody').html(result.html);
                        $.validator.unobtrusive.parse('#staffModalBody');
                    } else {
                        alert('An error occurred. Please try again.');
                    }
                }
            },
            error: function (xhr) {
                // Handles server response when it returns the form with validation errors (HTTP 200).
                if (xhr.status === 200 && xhr.responseText.indexOf('form-group') > -1) {
                    $('#staffModalBody').html(xhr.responseText);
                    $.validator.unobtrusive.parse('#staffModalBody');
                } else {
                    alert("An error occurred during staff creation.");
                }
            }
        });
        return false;
    });

    // Cleans out the modal content when the modal is fully hidden.
    $('#createStaffModal').on('hidden.bs.modal', function () {
        $('#staffModalBody').empty();
    });

    // --- Customer Modal Logic ---
    // Handles the button click to load the Customer creation form into a modal via AJAX.
    $('#createCustomerBtn').click(function () {
        $.ajax({
            url: '/Customers/CreatePartial',
            type: 'GET',
            cache: false,
            success: function (data) {
                $('#customerModalBody').html(data);
                $('#createCustomerModal').modal('show');
                
                // Re-parse the validation rules since the form content was loaded dynamically.
                $.validator.unobtrusive.parse('#customerModalBody');
            }
        });
    });

    // Universal submit handler for the customer creation form (delegated).
    $(document).on('submit', '#createCustomerForm', function (e) {
        e.preventDefault();
        var form = $(this);
        
        // Stop execution if client-side validation detects errors.
        if (!form.valid()) return false;

        $.ajax({
            url: form.attr('action'),
            type: form.attr('method'),
            data: form.serialize(),
            dataType: 'json',
            success: function (result) {
                if (result.success) {
                    $('#createCustomerModal').modal('hide');
                    window.location.href = result.redirectUrl;
                } else {
                    if (result.html) {
                        $('#customerModalBody').html(result.html);
                        $.validator.unobtrusive.parse('#customerModalBody');
                    } else {
                        alert('An error occurred. Please try again.');
                    }
                }
            },
            error: function (xhr) {
                // Handles server response when it returns the form with validation errors (HTTP 200).
                if (xhr.status === 200 && xhr.responseText.indexOf('form-group') > -1) {
                    $('#customerModalBody').html(xhr.responseText);
                    $.validator.unobtrusive.parse('#customerModalBody');
                } else {
                    alert("An error occurred during customer creation.");
                }
            }
        });
        return false;
    });

    // Cleans out the modal content when the modal is fully hidden.
    $('#createCustomerModal').on('hidden.bs.modal', function () {
        $('#customerModalBody').empty();
    });
});

// This function handles the general modal management for Edit/Delete actions in the 'Maintain' section.
var modalManagement = function () {

    // 1. Handlers for loading partial views into the modal
    $('.modal-link').click(function (e) {
        e.preventDefault();
        var url = $(this).attr('href');

        // Use AJAX to fetch and insert the partial view (form content) into the modal body.
        $('#modalBodyContent').load(url, function () {
            
            // Re-parse the validation rules since the form content was loaded dynamically.
            $.validator.unobtrusive.parse('#modalBodyContent');

            // Show the modal.
            $('#myModal').modal('show');

            // Check if a form element was successfully loaded.
            var formId = $('#modalBodyContent').find('form').attr('id');
            if (formId) {
                
                // Hook up the form submission handler to manage the AJAX response.
                attachFormSubmitHandler(formId);
            }
        });
    });

    // 2. Core function to attach the AJAX submit handler to forms
    function attachFormSubmitHandler(formId) {
        var formElement = $('#' + formId);

        // Important: unbind any previous submit handler to prevent this logic from running multiple times.
        formElement.off('submit.ajaxForm').on('submit.ajaxForm', function (e) {
            e.preventDefault();

            // Verify the form is valid before attempting an AJAX submission.
            if (!formElement.valid()) {
                return;
            }

            var url = formElement.attr('action');
            var formData = formElement.serialize();

            $.ajax({
                url: url,
                type: 'POST',
                data: formData,
                success: function (response) {
                    if (response.success) {
                        
                        // On successful action, close the modal and redirect to the maintenance page.
                        $('#myModal').modal('hide');
                        
                        // Redirect to 'Maintain' page which will show TempData message
                        window.location.href = response.redirectUrl || '@Url.Action("Maintain", "Home")';
                    } else if (response.message) {
                        
                        // If the server returns a JSON error object, display the message to the user.
                        alert('Error: ' + response.message);
                    } else {
                        
                        // If server-side model state failed, update the modal content with the form containing validation messages.
                        $('#modalBodyContent').html(response);
                        
                        // Re-apply validation to the new form content.
                        $.validator.unobtrusive.parse('#modalBodyContent');
                        
                        // Re-attach the handler since the form content was replaced.
                        attachFormSubmitHandler(formId);
                    }
                },
                error: function (xhr, status, error) {
                    alert('An unexpected error occurred: ' + xhr.responseText);
                }
            });
        });
    }

    // 3. Delete confirmation handler (if implemented via modal)
    // Handles click events for delete links, prompting the user for confirmation before executing an AJAX delete.
    $('.delete-link').click(function (e) {
        e.preventDefault();
        var deleteUrl = $(this).data('delete-url');
        var recordId = $(this).data('record-id');
        var recordName = $(this).data('record-name');
        var controller = $(this).data('controller');

        if (confirm('Are you sure you want to delete ' + recordName + ' (ID: ' + recordId + ')?')) {
            $.ajax({
                url: deleteUrl,
                type: 'POST',
                data: { id: recordId, __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val() },
                success: function (response) {
                    if (response.success) {
                        
                        // Success: Redirect to 'Maintain' page
                        window.location.href = response.redirectUrl || '@Url.Action("Maintain", "Home")';
                    } else {
                        
                        // Failure: Show error message
                        alert('Deletion failed: ' + response.message);
                    }
                },
                error: function (xhr, status, error) {
                    alert('An unexpected error occurred during deletion.');
                }
            });
        }
    });

    // Handle modal closing: clean up content
    // Cleans out the modal's body and footer every time it is closed.
    $('#myModal').on('hidden.bs.modal', function () {
        $('#modalBodyContent').empty();
        $('#modalFooterContent').empty();
    });
};

// Execute the main modal management function on page load.
$(document).ready(modalManagement);