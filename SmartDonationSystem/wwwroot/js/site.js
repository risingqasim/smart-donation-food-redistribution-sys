// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Site-wide JavaScript functionality

// SignalR Connection Management
let connection = null;
let retryCount = 0;
const maxRetries = 5;
const retryDelay = 1000; // 1 second

// Initialize SignalR connection
function initializeSignalR() {
    // Check if SignalR is loaded
    if (typeof signalR === 'undefined') {
        retryCount++;
        if (retryCount < maxRetries) {
            console.log(`SignalR not yet loaded, retrying... (${retryCount}/${maxRetries})`);
            setTimeout(initializeSignalR, retryDelay);
        } else {
            console.error('SignalR failed to load after maximum retries');
        }
        return;
    }

    try {
        // Create connection
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect()
            .build();

        // Connection event handlers
        connection.onclose((error) => {
            console.log('SignalR connection closed', error);
        });

        connection.onreconnecting((error) => {
            console.log('SignalR reconnecting...', error);
        });

        connection.onreconnected((connectionId) => {
            console.log('SignalR reconnected', connectionId);
        });

        // Start connection
        connection.start()
            .then(() => {
                console.log('SignalR connected');
                retryCount = 0; // Reset retry count on success
            })
            .catch((error) => {
                console.error('SignalR connection error:', error);
            });
    } catch (error) {
        console.error('Error initializing SignalR:', error);
    }
}

// Ensure DOM is loaded before initializing
document.addEventListener('DOMContentLoaded', function() {
    console.log('Smart Donation System loaded');
    
    // Initialize SignalR after a short delay to ensure SignalR script is loaded
    // SignalR script is loaded in _Layout.cshtml before site.js
    setTimeout(initializeSignalR, 100);
});

// Export connection for use in other scripts if needed
window.signalRConnection = function() {
    return connection;
};
