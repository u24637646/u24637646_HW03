// ==================================================================================
// _IndexScripts.js - REVISED (Jump by List Index)
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

        // Update placeholder to the new INDEX (1-based), and clear input
        $('#' + type + '-id-jump').attr('placeholder', newIdx + 1);
        $('#' + type + '-id-jump').val('');

        // Update the ActionLink
        updateDetailsLink(type, dbId);
    });

    indexInput.val(newIdx);
}

// ------------------------------------------------------------------
// Jump to Specific Index Function (REVISED to jump by List Index)
// NOTE: Function name kept as jumpToId for compatibility with Razor
// ------------------------------------------------------------------
function jumpToId(type) {
    var $input = $('#' + type + '-id-jump');
    var inputVal = $input.val();
    var totalCount = parseInt($('#' + type + '-count').text());

    if (totalCount === 0) {
        $input.val('');
        return;
    }

    // 1. Check for empty input (exit early)
    if (inputVal === null || inputVal.trim() === '') {
        $input.val('');
        return;
    }

    // 2. Validate input format (must be a number)
    if (!/^\d+$/.test(inputVal)) {
        alert("Please enter a valid list index number.");
        $input.val('');
        return;
    }

    // Input is 1-based (e.g., 1 to N), convert to 0-based
    var jumpIndexOneBased = parseInt(inputVal);
    var newIdx = jumpIndexOneBased - 1;

    // 3. Validate input range
    if (newIdx < 0 || newIdx >= totalCount) {
        alert(type.charAt(0).toUpperCase() + type.slice(1) + " index " + jumpIndexOneBased + " is out of range (1 to " + totalCount + ").");
        $input.val('');
        return;
    }

    // 4. Perform Navigation
    var indexInput = $('#' + type + '-index');
    var currentIdx = parseInt(indexInput.val());

    if (newIdx === currentIdx) {
        $input.val('');
        return;
    }

    $('#' + type + '-' + currentIdx).fadeOut(100, function () {
        var $newItem = $('#' + type + '-' + newIdx);
        $newItem.fadeIn(200);

        var dbId = $newItem.data(type + '-id');

        // Update placeholder to the new 1-based index
        $input.attr('placeholder', newIdx + 1);
        $input.val('');

        updateDetailsLink(type, dbId);
    });

    indexInput.val(newIdx);
}

// ------------------------------------------------------------------
// Document Ready - Initialization and Modal Logic
// ------------------------------------------------------------------
$(document).ready(function () {

    // Initialize all three panels: set initial placeholder to index 1
    ['staff', 'customer', 'product'].forEach(function (type) {
        var initialItem = $('#' + type + '-0');

        if (initialItem.length > 0) {
            var initialId = initialItem.data(type + '-id');

            if (initialId) {
                // Initialize placeholder to the first INDEX: 1
                $('#' + type + '-id-jump').attr('placeholder', 1);
                updateDetailsLink(type, initialId);
            }
        }
    });

    // Product Filter: Submit form on dropdown change
    $('#brand-filter, #category-filter').on('change', function () {
        $('.product-filter-form').submit();
    });

    // --- Modal Popups for Staff Creation (Unchanged) ---
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

    // --- Modal Popups for Customer Creation (Unchanged) ---
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