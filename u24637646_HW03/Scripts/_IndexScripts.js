// ==================================================================================
// _IndexScripts.js - Fixed Structure
// ==================================================================================


// ------------------------------------------------------------------
// Helper function to update "View Details" link
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
// Carousel Navigation Function (Panel Change Logic Only)
// ------------------------------------------------------------------
function navigate(type, direction) {
    var indexInput = $('#' + type + '-index');
    var currentIdx = parseInt(indexInput.val());
    // Get total count from the hidden element
    var totalCount = parseInt($('#' + type + '-count').val());
    var lastIndex = totalCount - 1;

    if (totalCount === 0) return;

    var newIdx = currentIdx + direction;

    // --- Boundary and Wrapping Logic ---

    // Case 1: Wrap forward (from last item to first)
    if (newIdx > lastIndex) {
        newIdx = 0;
    }
    // Case 2: Wrap backward (from first item to last)
    else if (newIdx < 0) {
        newIdx = lastIndex;
    }

    // --- Navigation Execution ---
    $('#' + type + '-' + currentIdx).fadeOut(100, function () {
        var $newItem = $('#' + type + '-' + newIdx);
        $newItem.fadeIn(200);

        // Ensure the ID attribute is correct (e.g., data-staff-id)
        var dbId = $newItem.data(type + '-id');

        // REMOVED: Counter update logic is no longer needed.

        // Update the ActionLink
        updateDetailsLink(type, dbId);
    });

    // Update the hidden index input for the next navigation call
    indexInput.val(newIdx);
}

// ------------------------------------------------------------------
// Document Ready - Initialization and Modal Logic
// ------------------------------------------------------------------
$(document).ready(function () {

    // --- Modal Closing Logic ---
    $(document).on('click', '.modal-close-btn', function () {
        var $currentModal = $(this).closest('.modal');
        if ($currentModal.length) {
            $currentModal.modal('hide');
        }
    });

    // --- Initialization of Panels ---
    // Initialize all three panels: set initial details link
    ['staff', 'customer', 'product'].forEach(function (type) {
        var initialItem = $('#' + type + '-0');
        var totalCount = parseInt($('#' + type + '-count').val() || '0');

        if (totalCount === 0) {
            return;
        }

        if (initialItem.length > 0) {
            var initialId = initialItem.data(type + '-id');

            if (initialId) {
                // REMOVED: Counter initialization is no longer needed.
                updateDetailsLink(type, initialId);
            }
        }
    });

    // --- Product Filter: Submit form on dropdown change ---
    $('#brand-filter, #category-filter').on('change', function () {
        $('.product-filter-form').submit();
    });

    // --- Staff Modal Logic ---
    $('#createStaffBtn').click(function () {
        $.ajax({
            url: '/Staffs/CreatePartial',
            type: 'GET',
            cache: false,
            success: function (data) {
                $('#staffModalBody').html(data);
                $('#createStaffModal').modal('show');
                $.validator.unobtrusive.parse('#staffModalBody');
            }
        });
    });

    $(document).on('submit', '#createStaffForm', function (e) {
        e.preventDefault();
        var form = $(this);
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

    $('#createStaffModal').on('hidden.bs.modal', function () {
        $('#staffModalBody').empty();
    });

    // --- Customer Modal Logic ---
    $('#createCustomerBtn').click(function () {
        $.ajax({
            url: '/Customers/CreatePartial',
            type: 'GET',
            cache: false,
            success: function (data) {
                $('#customerModalBody').html(data);
                $('#createCustomerModal').modal('show');
                $.validator.unobtrusive.parse('#customerModalBody');
            }
        });
    });

    $(document).on('submit', '#createCustomerForm', function (e) {
        e.preventDefault();
        var form = $(this);
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

    $('#createCustomerModal').on('hidden.bs.modal', function () {
        $('#customerModalBody').empty();
    });
});

// Function to handle the AJAX form submission and modal management
var modalManagement = function () {

    // 1. Handlers for loading partial views into the modal
    $('.modal-link').click(function (e) {
        e.preventDefault();
        var url = $(this).attr('href');

        // Load the partial view into the modal body
        $('#modalBodyContent').load(url, function () {
            // Re-parse validation rules (crucial for partial views)
            $.validator.unobtrusive.parse('#modalBodyContent');

            // Show the modal
            $('#myModal').modal('show');

            // Check if it's a Create/Edit form being loaded
            var formId = $('#modalBodyContent').find('form').attr('id');
            if (formId) {
                // Attach submit handler to the form once it's loaded
                attachFormSubmitHandler(formId);
            }
        });
    });

    // 2. Core function to attach the AJAX submit handler to forms
    function attachFormSubmitHandler(formId) {
        var formElement = $('#' + formId);

        // Remove previous handler to prevent multiple executions
        formElement.off('submit.ajaxForm').on('submit.ajaxForm', function (e) {
            e.preventDefault();

            // Ensure client-side validation passes
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
                        // Success: Hide modal and redirect (or refresh)
                        $('#myModal').modal('hide');
                        // Redirect to 'Maintain' page which will show TempData message
                        window.location.href = response.redirectUrl || '@Url.Action("Maintain", "Home")';
                    } else if (response.message) {
                        // Handle server-side errors that return JSON
                        alert('Error: ' + response.message);
                    } else {
                        // Validation failed (server returns PartialView with errors)
                        $('#modalBodyContent').html(response);
                        // Re-parse validation rules for the updated content
                        $.validator.unobtrusive.parse('#modalBodyContent');
                        // Re-attach the submit handler to the new form content
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
    $('#myModal').on('hidden.bs.modal', function () {
        $('#modalBodyContent').empty();
        $('#modalFooterContent').empty();
    });
};

// Run the modal management logic when the document is ready
$(document).ready(modalManagement);