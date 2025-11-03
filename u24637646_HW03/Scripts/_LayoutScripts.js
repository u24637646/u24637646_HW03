// This script manages the expanding card-style navigation menu.

$(document).ready(function () {
    const BASE_NAV_HEIGHT = 70;
    const CARD_STAGGER_DELAY = 80;

    // Controls the staggered animation of the individual navigation cards.
    function animateCards(navEl, direction) {
        var $cards = navEl.find('.nav-card');

        if (direction === 'open') {
            // For the opening sequence, cards fade in one by one from top to bottom.
            $cards.each(function (index) {
                setTimeout(() => {
                    $(this).addClass('active');
                }, index * CARD_STAGGER_DELAY);
            });
        } else {
            // For closing, cards fade out in reverse order (bottom to top).
            $cards.get().reverse().forEach(function (card, index) {
                setTimeout(() => {
                    $(card).removeClass('active');
                }, index * (CARD_STAGGER_DELAY / 2));
            });
        }
    }

    // Main click handler for the menu button.
    $('.hamburger-menu').on('click', function () {
        var $nav = $(this).closest('.card-nav');
        var $hamburger = $(this);
        var isExpanded = $nav.hasClass('open');

        if (!isExpanded) {
            // --- OPEN MENU ---
            var $content = $nav.find('.card-nav-content');

            // 1. Calculate the required final height based on the content.
            $content.css({ 'opacity': 0, 'pointer-events': 'auto', 'visibility': 'visible', 'position': 'static', 'height': 'auto' });

            var contentHeight = $content.get(0).scrollHeight;
            var newHeight = contentHeight + BASE_NAV_HEIGHT + 10 + 20; // 20 accounts for the new top padding

            // Reset temporary styles applied for height calculation.
            $content.css({ 'visibility': 'hidden', 'position': 'absolute', 'height': '' });

            // 2. Apply the calculated height and the 'open' class to start the transition.
            $nav.css('height', newHeight + 'px');
            $nav.addClass('open');

            // Add class and update accessibility tag.
            $hamburger.addClass('open').attr('aria-label', 'Close menu');

            // 3. Start the card animation after a brief delay.
            setTimeout(() => {
                $content.css({ 'opacity': 1, 'visibility': 'visible', 'pointer-events': 'auto' });
                animateCards($nav, 'open');
            }, 50);

        } else {
            // --- CLOSE MENU ---
            var $content = $nav.find('.card-nav-content');

            // 1. Initiate the staggered card exit animation.
            animateCards($nav, 'close');

            // 2. Collapse the navigation back to its base height.
            setTimeout(() => {
                $nav.css('height', BASE_NAV_HEIGHT + 'px');
                $content.css({ 'opacity': 0, 'pointer-events': 'none' });
                $nav.removeClass('open');

                // Remove class and update accessibility tag.
                $hamburger.removeClass('open').attr('aria-label', 'Open menu');

                // 3. Hide content after the transition finishes to prevent interaction.
                setTimeout(() => {
                    $content.css('visibility', 'hidden');
                }, 400);

            }, 150);
        }
    });
});