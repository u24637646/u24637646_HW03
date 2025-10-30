// Helper function to consolidate link updating logic for View Details button
function updateDetailsLink(type, dbId) {
    var $link = $('#' + type + '-details-link');
    if ($link.length > 0) {
        var currentHref = $link.attr('href');
        var newHref;

        // Logic to correctly replace/append the ID to the URL
        if (/\/[0-9]+$/.test(currentHref)) {
            // Replace existing ID at the end
            newHref = currentHref.replace(/\/[0-9]+$/, '/' + dbId);
        } else if (/\/Details$/.test(currentHref) || /\/Details\/$/.test(currentHref)) {
            // Append ID after Details (handling trailing slashes)
            newHref = currentHref.replace(/\/+$/, '') + '/' + dbId;
        } else {
            // General case (e.g., if there are query strings)
            newHref = currentHref.split('?')[0].replace(/\/+$/, '') + '/' + dbId;
        }
        $link.attr('href', newHref);
    }
}

function navigate(type, direction) {
    var indexInput = $('#' + type + '-index');
    var currentIdx = parseInt(indexInput.val());
    var totalCount = parseInt($('#' + type + '-count').text());
    var lastIndex = totalCount - 1;

    if (totalCount === 0) return;

    var newIdx = currentIdx + direction;

    // 1. Minimum Boundary Check: Stop navigation at the first item (index 0)
    if (direction === -1 && currentIdx === 0) {
        return;
    }

    // 2. Maximum Boundary Check (Wrapping): If newIdx exceeds the max index, wrap to 0.
    if (newIdx > lastIndex) {
        newIdx = 0;
    }

    // Ensure the navigator is visible (in case a filter was applied and then reset)
    $('.product-navigator').show();

    // --- Navigation Execution ---

    $('#' + type + '-' + currentIdx).fadeOut(100, function () {
        var $newItem = $('#' + type + '-' + newIdx);
        $newItem.fadeIn(200);

        var dbId = $newItem.data(type + '-id');

        // Update the input placeholder to show the current ID
        $('#' + type + '-id-jump').attr('placeholder', dbId);
        $('#' + type + '-id-jump').val(''); // Clear input field

        // Update the ActionLink's href (using the helper)
        updateDetailsLink(type, dbId);
    });

    indexInput.val(newIdx);
}


function jumpToId(type) {
    var inputId = $('#' + type + '-id-jump').val();

    if (!/^\d+$/.test(inputId)) {
        alert("Please enter a valid ID number.");
        $('#' + type + '-id-jump').val('');
        return;
    }

    var idToJump = parseInt(inputId);
    // Accesses the globally populated map
    // Note: The global map is called staffIdMap in the Razor code, 
    // but its keys are 'staff', 'customer', 'product'.
    var newIdx = staffIdMap[type][idToJump];

    if (newIdx !== undefined) {
        var indexInput = $('#' + type + '-index');
        var currentIdx = parseInt(indexInput.val());

        if (newIdx === currentIdx) {
            $('#' + type + '-id-jump').val('');
            return;
        }

        // Ensure the navigator is visible (in case a filter was applied and then reset)
        $('.product-navigator').show();

        $('#' + type + '-' + currentIdx).fadeOut(100, function () {
            var $newItem = $('#' + type + '-' + newIdx);
            $newItem.fadeIn(200);

            var dbId = $newItem.data(type + '-id');

            // Set the new item's ID as the placeholder and clear the value
            $('#' + type + '-id-jump').attr('placeholder', dbId);
            $('#' + type + '-id-jump').val('');

            // Update the ActionLink's href (using the helper)
            updateDetailsLink(type, dbId);
        });

        indexInput.val(newIdx);
    } else {
        alert(type.charAt(0).toUpperCase() + type.slice(1) + " ID " + idToJump + " not found.");
        $('#' + type + '-id-jump').val('');
    }
}


// ------------------------------------------------------------------
// Product Filtering Logic (NEW)
// ------------------------------------------------------------------

function filterProducts() {
    var type = 'product';
    var searchTerm = $('#' + type + '-search-input').val().toLowerCase().trim();
    var foundProduct = false;
    var firstVisibleIndex = -1;
    var $productItems = $('.product-item');
    var $resetBtn = $('#product-reset-btn');
    var $navigator = $('.product-navigator');

    // Hide all product items initially
    $productItems.hide();

    // If no search term, reset the filter
    if (searchTerm.length === 0) {
        resetProductFilter();
        return;
    }

    // Loop through all products to find matches and show them
    $productItems.each(function () {
        var $item = $(this);
        // Extract product name from <h4> by removing the ID and dot prefix
        // Example: "1. Trek Fuel EX 8" -> "Trek Fuel EX 8"
        var productName = $item.find('h4').text().toLowerCase().split('.').slice(1).join('.').trim();

        if (productName.includes(searchTerm)) {
            $item.show();
            foundProduct = true;
            // Capture the array index of the first matching item
            if (firstVisibleIndex === -1) {
                firstVisibleIndex = parseInt($item.attr('id').split('-')[1]);
            }
        }
    });

    // Show the reset button
    $resetBtn.show();

    if (foundProduct) {
        // Update the navigation index to the first visible item's array index
        var indexInput = $('#' + type + '-index');
        indexInput.val(firstVisibleIndex);

        // Update Details Link and placeholder for the first found product
        var $firstItem = $('#' + type + '-' + firstVisibleIndex);
        var dbId = $firstItem.data(type + '-id');

        updateDetailsLink(type, dbId);

        $('#' + type + '-id-jump').attr('placeholder', dbId);
        $('#' + type + '-id-jump').val('');

        // Hide the default navigator as the list view changes
        $navigator.hide();
    } else {
        // Handle no results
        alert("No products found matching '" + searchTerm + "'.");
        // Ensure navigator remains hidden when there are no matching results
        $navigator.hide();
    }
}

function resetProductFilter() {
    var type = 'product';
    var indexInput = $('#' + type + '-index');
    var currentIdx = parseInt(indexInput.val());

    // 1. Hide the reset button and clear the search input
    $('#' + type + '-search-input').val('');
    $('#product-reset-btn').hide();

    // 2. Hide all items and show only the item at the current navigation index
    $('.product-item').hide();
    $('#' + type + '-' + currentIdx).show();

    // 3. Show the navigator footer
    $('.product-navigator').show();

    // Ensure link and placeholder are correct for the current item
    var $currentItem = $('#' + type + '-' + currentIdx);
    var dbId = $currentItem.data(type + '-id');
    updateDetailsLink(type, dbId);
    $('#' + type + '-id-jump').attr('placeholder', dbId);
    $('#' + type + '-id-jump').val('');
}

// ------------------------------------------------------------------
// Initialization on Document Ready: Sets initial placeholder and link URL
// ------------------------------------------------------------------

$(document).ready(function () {
    // Panels to initialize: staff, customer, product
    ['staff', 'customer', 'product'].forEach(function (type) {
        var initialItem = $('#' + type + '-0');

        if (initialItem.length > 0) {
            var initialId = initialItem.data(type + '-id');

            if (initialId) {
                $('#' + type + '-id-jump').attr('placeholder', initialId);
                // Update the ActionLink's href (using the helper)
                updateDetailsLink(type, initialId);
            }
        }
    });
});