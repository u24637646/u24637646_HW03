
       
        $(document).ready(function () {
            const BASE_NAV_HEIGHT = 70;
            const CARD_STAGGER_DELAY = 80;

            // FUNCTION DEFINITION IS CRITICAL FOR BOTH OPEN AND CLOSE SEQUENCES
            function animateCards(navEl, direction) {
                var $cards = navEl.find('.nav-card');

                if (direction === 'open') {
                    // Open: Animate cards in order (top to bottom).
                    $cards.each(function (index) {
                        setTimeout(() => {
                            $(this).addClass('active');
                        }, index * CARD_STAGGER_DELAY);
                    });
                } else {
                    // Close: Animate cards in reverse order (bottom to top) for a smooth exit.
                    $cards.get().reverse().forEach(function (card, index) {
                        setTimeout(() => {
                            $(card).removeClass('active');
                        }, index * (CARD_STAGGER_DELAY / 2));
                    });
                }
            }

            // Click handler for the hamburger menu button.
            $('.hamburger-menu').on('click', function () {
                var $nav = $(this).closest('.card-nav');
                var $hamburger = $(this);
                var isExpanded = $nav.hasClass('open');

                if (!isExpanded) {
                    // --- OPEN SEQUENCE ---
                    var $content = $nav.find('.card-nav-content');

                    // 1. Calculate open height dynamically
                    $content.css({ 'opacity': 0, 'pointer-events': 'auto', 'visibility': 'visible', 'position': 'static', 'height': 'auto' });

                    var contentHeight = $content.get(0).scrollHeight;
                    var newHeight = contentHeight + BASE_NAV_HEIGHT + 10 + 20; // 20 accounts for the new top padding

                    // Restore temporary styles
                    $content.css({ 'visibility': 'hidden', 'position': 'absolute', 'height': '' });

                    // 2. Animate nav height and set classes
                    $nav.css('height', newHeight + 'px');
                    $nav.addClass('open');
                    $hamburger.addClass('open').attr('aria-label', 'Close menu'); // ADDS .open CLASS

                    // 3. Stagger cards once height transition has started.
                    setTimeout(() => {
                        $content.css({ 'opacity': 1, 'visibility': 'visible', 'pointer-events': 'auto' });
                        animateCards($nav, 'open');
                    }, 50);

                } else {
                    // --- CLOSE SEQUENCE ---
                    var $content = $nav.find('.card-nav-content');

                    // 1. Stagger card exit animation first.
                    animateCards($nav, 'close');

                    // 2. Collapse nav height and remove classes
                    setTimeout(() => {
                        $nav.css('height', BASE_NAV_HEIGHT + 'px');
                        $content.css({ 'opacity': 0, 'pointer-events': 'none' });
                        $nav.removeClass('open');
                        $hamburger.removeClass('open').attr('aria-label', 'Open menu'); // REMOVES .open CLASS

                        // 3. Clean up content visibility after height transition ends.
                        setTimeout(() => {
                            $content.css('visibility', 'hidden');
                        }, 400);

                    }, 150);
                }
            });
        });
 