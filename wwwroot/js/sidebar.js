// sidebar.js - Collapsible sidebar functionality for both desktop and mobile
document.addEventListener('DOMContentLoaded', function () {
    // Elements
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const mobileSidebarToggle = document.getElementById('mobile-sidebar-toggle');
    const sidebar = document.getElementById('sidebar');
    const contentWrapper = document.querySelector('.content-wrapper');

    // State tracking
    let sidebarCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';

    // Initialize sidebar state
    if (sidebarCollapsed) {
        sidebar.classList.add('collapsed');
        contentWrapper.classList.add('expanded');
    }

    // Toggle sidebar function
    function toggleSidebar() {
        sidebar.classList.toggle('collapsed');
        contentWrapper.classList.toggle('expanded');

        // Save state to localStorage
        sidebarCollapsed = sidebar.classList.contains('collapsed');
        localStorage.setItem('sidebarCollapsed', sidebarCollapsed);
    }

    // Toggle mobile sidebar function
    function toggleMobileSidebar() {
        sidebar.classList.toggle('show');
    }

    // Desktop toggle button
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleSidebar();
        });
    }

    // Mobile toggle button
    if (mobileSidebarToggle) {
        mobileSidebarToggle.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleMobileSidebar();
        });
    }

    // Close sidebar when clicking outside on mobile
    document.addEventListener('click', function (event) {
        const isClickInside = sidebar.contains(event.target) ||
            (sidebarToggle && sidebarToggle.contains(event.target)) ||
            (mobileSidebarToggle && mobileSidebarToggle.contains(event.target));

        if (!isClickInside && sidebar.classList.contains('show') && window.innerWidth < 768) {
            sidebar.classList.remove('show');
        }
    });

    // Handle window resize
    window.addEventListener('resize', function () {
        if (window.innerWidth >= 768) {
            sidebar.classList.remove('show');
        }
    });
});