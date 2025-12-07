// 🌙 Dark Mode Toggle Script
// Runs early to prevent flash and manages theme persistence with localStorage

(function() {
    // Initialize theme based on localStorage or system preference
    function initializeTheme() {
        const savedTheme = localStorage.getItem('quillkit-theme');
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const isDark = savedTheme === 'dark' || (savedTheme === null && prefersDark === false);
        
        applyTheme(isDark);
    }

    // Apply theme to document
    function applyTheme(isDark) {
        const html = document.documentElement;
        
        if (isDark) {
            html.classList.add('dark-mode');
            html.classList.remove('light-mode');
            localStorage.setItem('quillkit-theme', 'dark');
        } else {
            html.classList.remove('dark-mode');
            html.classList.add('light-mode');
            localStorage.setItem('quillkit-theme', 'light');
        }
        
        updateToggleButton(isDark);
    }

    // Update toggle button icon
    function updateToggleButton(isDark) {
        const toggle = document.getElementById('theme-toggle');
        if (toggle) {
            const icon = toggle.querySelector('i');
            if (icon) {
                icon.classList.remove('bi-moon-fill', 'bi-sun-fill');
                icon.classList.add(isDark ? 'bi-moon-fill' : 'bi-sun-fill');
            }
        }
    }

    // Toggle theme
    function toggleTheme() {
        const html = document.documentElement;
        const isDark = html.classList.contains('dark-mode');
        applyTheme(!isDark);
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            initializeTheme();
            const toggle = document.getElementById('theme-toggle');
            if (toggle) {
                toggle.addEventListener('click', toggleTheme);
            }
        });
    } else {
        // DOM already loaded
        initializeTheme();
        const toggle = document.getElementById('theme-toggle');
        if (toggle) {
            toggle.addEventListener('click', toggleTheme);
        }
    }
})();
