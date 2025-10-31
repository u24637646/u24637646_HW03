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
// Product Filtering Logic (UPDATED)
// ------------------------------------------------------------------

// Function to handle the text search submission
function filterProductsBySearch() {
    var type = 'product';
    var searchTerm = $('#' + type + '-search-input').val().trim();

    // 1. Set the search term into a hidden input field of the main form
    //    (You'll need to add this hidden input to the Razor form above)
    $('#search-term-input').val(searchTerm);

    // 2. Submit the main filtering form (which contains all filters)
    $('.product-filter-form').submit();
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

    // ADD EVENT HANDLERS FOR THE PRODUCT TEXT SEARCH
    var $searchInput = $('#product-search-input');

    // 1. Add a hidden input to the form to capture text search
    // (This is often easier to put in the Razor view, but we'll inject it here for robust client-side handling)
    $('.product-filter-form').append('<input type="hidden" name="searchTerm" id="search-term-input" value="@(ViewData["SearchTerm"])" />');

    // 2. Clear the search term on any other filter change (Dropdowns)
    $('#brand-filter, #category-filter').on('change', function () {
        $searchInput.val('');
    });

    // 3. Update text search input to submit the form on Enter key press
    $searchInput.on('keypress', function (e) {
        if (e.which === 13) {
            e.preventDefault(); // Prevent default form submission if outside the form
            filterProductsBySearch();
        }
    });

    // Handle initial state: if a filter is applied, hide the navigator
    if ($('#brand-filter').val() !== '' || $('#category-filter').val() !== '' || $searchInput.val() !== '') {
        $('.product-navigator').hide();

        // Hide all items and show them *all* when filters are applied
        // This is necessary because the default Razor logic only shows product-0
        $('.product-item').show();

    } else {
        $('.product-navigator').show();
        $('.product-item').hide(); // Hide all but the first item if no filters are active
        $('#product-0').show(); // Show the first item if no filters are active (default view)
    }
});