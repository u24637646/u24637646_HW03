// ==================================================================================
// _IndexScripts.js - Simplified & Fixed
// ==================================================================================


// ------------------------------------------------------------------
// Helper function to update "View Details" link
// ------------------------------------------------------------------
function updateDetailsLink(type, dbId) {
    var $link = $('#' + type + '-details-link');
    if ($link.length > 0) {
        var currentHref = $link.attr('href');
        // Safely replace the ID part of the URL (assuming /Controller/Details/ID)
        var newHref = currentHref.replace(/\/[0-9]+$/, '/' + dbId);
        $link.attr('href', newHref);
    }
}

// ------------------------------------------------------------------
// Carousel Navigation Function (FIXED for Backward Wrapping)
// ------------------------------------------------------------------
function navigate(type, direction) {
    var indexInput = $('#' + type + '-index');
    var currentIdx = parseInt(indexInput.val());
    var totalCount = parseInt($('#' + type + '-count').text());
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

        var dbId = $newItem.data(type + '-id');

        // Update placeholder and clear input
        $('#' + type + '-id-jump').attr('placeholder', dbId);
        $('#' + type + '-id-jump').val('');

        // Update the ActionLink
        updateDetailsLink(type, dbId);
    });

    indexInput.val(newIdx);
}

// ------------------------------------------------------------------
// Jump to Specific ID Function (FIXED for Placeholder Usage and Map check)
// ------------------------------------------------------------------
function jumpToId(type) {
    var $input = $('#' + type + '-id-jump');
    var inputId = $input.val();
    var idToJump;

    // --- 1. Determine the ID to jump to ---
    if (inputId === null || inputId.trim() === '') {
        var currentPlaceholder = $input.attr('placeholder');

        if (currentPlaceholder && /^\d+$/.test(currentPlaceholder)) {
            idToJump = parseInt(currentPlaceholder);
        } else {
            alert("Please enter a valid ID number.");
            $input.val('');
            return;
        }
    } else {
        if (!/^\d+$/.test(inputId)) {
            alert("Please enter a valid ID number.");
            $input.val('');
            return;
        }
        idToJump = parseInt(inputId);
    }

    // --- 2. Perform Lookup and Validation ---
    if (typeof entityIdMap == 'undefined' || !entityIdMap[type]) {
        console.error("entityIdMap or the map for type '" + type + "' is not initialized.");
        alert("Navigation map is not ready. Please refresh the page.");
        $input.val('');
        return;
    }

    var newIdx = entityIdMap[type][idToJump];

    if (newIdx !== undefined) {
        var indexInput = $('#' + type + '-index');
        var currentIdx = parseInt(indexInput.val());

        if (newIdx === currentIdx) {
            $input.val('');
            return;
        }

        // --- 3. Animation and Index Update ---
        $('#' + type + '-' + currentIdx).fadeOut(100, function () {
            var $newItem = $('#' + type + '-' + newIdx);
            $newItem.fadeIn(200);

            var dbId = $newItem.data(type + '-id');

            $input.attr('placeholder', dbId);
            $input.val('');

            updateDetailsLink(type, dbId);
        });

        indexInput.val(newIdx);
    } else {
        alert(type.charAt(0).toUpperCase() + type.slice(1) + " ID " + idToJump + " not found in the current list.");
        $input.val('');
    }
}

// ------------------------------------------------------------------
// Document Ready - Initialization and Modal Logic
// ------------------------------------------------------------------
$(document).ready(function () {

    // Initialize all three panels: set initial placeholder and details link
    ['staff', 'customer', 'product'].forEach(function (type) {
        var initialItem = $('#' + type + '-0');

        if (initialItem.length > 0) {
            var initialId = initialItem.data(type + '-id');

            if (initialId) {
                $('#' + type + '-id-jump').attr('placeholder', initialId);
                updateDetailsLink(type, initialId);
            }
        }
    });

    // Product Filter: Submit form on dropdown change
    $('#brand-filter, #category-filter').on('change', function () {
        $('.product-filter-form').submit();
    });

    // --- Modal Popups for Staff Creation ---
    $('#createStaffBtn').click(function () {
        $.ajax({
            url: '/Staffs/CreatePartial', // Use relative path
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
                    alert('An error occurred. Please try again.');
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

    // --- Modal Popups for Customer Creation ---
    $('#createCustomerBtn').click(function () {
        $.ajax({
            url: '/Customers/CreatePartial', // Use relative path
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
                    alert('An error occurred. Please try again.');
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