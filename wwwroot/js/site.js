// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Theme toggle functionality
document.addEventListener('DOMContentLoaded', function () {
    const themeToggle = document.getElementById('theme-toggle');

    // Check for saved theme preference or use default
    const currentTheme = localStorage.getItem('theme') || 'dark';

    // Set initial state based on saved preference
    if (currentTheme === 'light') {
        document.body.classList.add('light-theme');
        themeToggle.checked = true;
    }

    // Toggle theme when switch is clicked
    themeToggle.addEventListener('change', function () {
        if (this.checked) {
            // Light mode
            document.body.classList.add('light-theme');
            localStorage.setItem('theme', 'light');
        } else {
            // Dark mode
            document.body.classList.remove('light-theme');
            localStorage.setItem('theme', 'dark');
        }
    });
});