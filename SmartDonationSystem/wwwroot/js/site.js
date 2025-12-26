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

// ============================================
// Theme Management System
// ============================================

const ThemeManager = {
    // Theme storage key
    STORAGE_KEY: 'smartDonationTheme',
    
    // Initialize theme on page load
    init: function() {
        // Get saved theme or default to light
        const savedTheme = localStorage.getItem(this.STORAGE_KEY) || 'light';
        // Set theme immediately (before page render to prevent flash)
        if (savedTheme === 'dark') {
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.setAttribute('data-bs-theme', 'dark');
        }
        // Update icon after DOM is ready
        setTimeout(() => {
            this.updateToggleIcon(savedTheme);
        }, 0);
    },
    
    // Set theme
    setTheme: function(theme) {
        // Validate theme
        if (theme !== 'light' && theme !== 'dark') {
            theme = 'light';
        }
        
        // Apply theme with smooth transition
        if (theme === 'dark') {
            document.documentElement.setAttribute('data-theme', 'dark');
        } else {
            document.documentElement.removeAttribute('data-theme');
        }
        
        // Save to localStorage
        localStorage.setItem(this.STORAGE_KEY, theme);
        
        // Update toggle icon
        this.updateToggleIcon(theme);
        
        // Trigger custom event for any components that need to react to theme change
        window.dispatchEvent(new CustomEvent('themeChange', { detail: { theme } }));
        
        // Update Bootstrap data attribute for compatibility
        document.documentElement.setAttribute('data-bs-theme', theme);
    },
    
    // Toggle between light and dark
    toggle: function() {
        const currentTheme = localStorage.getItem(this.STORAGE_KEY) || 'light';
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        this.setTheme(newTheme);
        
        // Prevent page reload
        return false;
    },
    
    // Get current theme
    getCurrentTheme: function() {
        return localStorage.getItem(this.STORAGE_KEY) || 'light';
    },
    
    // Update toggle icon based on theme
    updateToggleIcon: function(theme) {
        const themeIcon = document.getElementById('themeIcon');
        if (themeIcon) {
            if (theme === 'dark') {
                themeIcon.classList.remove('fa-moon');
                themeIcon.classList.add('fa-sun');
                themeIcon.setAttribute('title', 'Switch to Light Mode');
            } else {
                themeIcon.classList.remove('fa-sun');
                themeIcon.classList.add('fa-moon');
                themeIcon.setAttribute('title', 'Switch to Dark Mode');
            }
        }
    }
};

// ============================================
// Theme Toggle Event Handler
// ============================================
function setupThemeToggle() {
    const themeToggle = document.getElementById('themeToggle');
    
    if (themeToggle) {
        // Click handler
        themeToggle.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            ThemeManager.toggle();
        });
        
        // Keyboard accessibility
        themeToggle.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                e.stopPropagation();
                ThemeManager.toggle();
            }
        });
        
        // Set initial icon state
        const currentTheme = ThemeManager.getCurrentTheme();
        ThemeManager.updateToggleIcon(currentTheme);
    }
}

// Ensure DOM is loaded before initializing
document.addEventListener('DOMContentLoaded', function() {
    console.log('Smart Donation System loaded');
    
    // Initialize theme system (before other components)
    ThemeManager.init();
    setupThemeToggle();
    
    // Initialize SignalR after a short delay to ensure SignalR script is loaded
    // SignalR script is loaded in _Layout.cshtml before site.js
    setTimeout(initializeSignalR, 100);
});

// Make ThemeManager available globally for any custom scripts
window.ThemeManager = ThemeManager;

// Export connection for use in other scripts if needed
window.signalRConnection = function() {
    return connection;
};
