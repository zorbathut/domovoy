/**
 * Converts UTC timestamps to the user's local timezone.
 * Finds all elements with data-utc-timestamp attribute and updates their display.
 */
(function() {
    'use strict';

    function convertTimestamps() {
        // Find all timestamp elements
        const timestampElements = document.querySelectorAll('[data-utc-timestamp]');

        timestampElements.forEach(element => {
            const utcString = element.getAttribute('data-utc-timestamp');
            if (!utcString) return;

            try {
                // Parse the UTC timestamp
                const utcDate = new Date(utcString);

                // Format in user's local timezone with readable format
                // Example: "11/12/2025, 10:45:23 AM PST"
                const options = {
                    year: 'numeric',
                    month: '2-digit',
                    day: '2-digit',
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit',
                    timeZoneName: 'short'
                };

                const localTimeString = utcDate.toLocaleString(undefined, options);

                // Update the element's text content
                element.textContent = localTimeString;

            } catch (error) {
                console.error('Error converting timestamp:', utcString, error);
                // Leave original UTC display on error
            }
        });
    }

    // Run on initial page load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', convertTimestamps);
    } else {
        convertTimestamps();
    }

    // Re-run after Blazor updates the DOM
    // Blazor Server uses MutationObserver pattern for dynamic content
    if (typeof MutationObserver !== 'undefined') {
        const observer = new MutationObserver(function(mutations) {
            // Check if any mutations added timestamp elements
            let shouldConvert = false;
            for (let mutation of mutations) {
                if (mutation.addedNodes.length > 0) {
                    for (let node of mutation.addedNodes) {
                        if (node.nodeType === 1) { // Element node
                            if (node.hasAttribute && node.hasAttribute('data-utc-timestamp')) {
                                shouldConvert = true;
                                break;
                            }
                            // Check children
                            if (node.querySelectorAll && node.querySelectorAll('[data-utc-timestamp]').length > 0) {
                                shouldConvert = true;
                                break;
                            }
                        }
                    }
                }
                if (shouldConvert) break;
            }

            if (shouldConvert) {
                convertTimestamps();
            }
        });

        // Start observing the document body for changes
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }
})();
