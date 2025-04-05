// --- site.js ---

document.addEventListener('DOMContentLoaded', function () {
    // --- Element References ---
    const sidebar = document.getElementById('sidebar');
    const body = document.body;
    const sidebarToggle = document.getElementById('sidebar-toggle'); // Desktop toggle
    const mobileSidebarToggle = document.getElementById('mobile-sidebar-toggle'); // Mobile toggle
    const themeToggleCheckbox = document.getElementById('theme-toggle-checkbox'); // Expanded slider checkbox
    const themeToggleLabel = document.getElementById('theme-switch-label'); // Expanded slider label
    const themeToggleIconButton = document.getElementById('theme-toggle-collapsed'); // Collapsed icon button
    const themeToggleIcon = document.getElementById('theme-toggle-icon'); // Icon within the button

    const MOBILE_BREAKPOINT = 992; // Corresponds to d-lg-none/d-lg-block

    // --- Sidebar State Logic ---
    function applySidebarState(collapse) {
        if (!sidebar || !body) return; // Ensure elements exist

        const isMobile = window.innerWidth < MOBILE_BREAKPOINT;

        if (isMobile) {
            // Mobile: Use overlay with 'active-mobile' class
            if (collapse) {
                sidebar.classList.remove('active-mobile');
            } else {
                sidebar.classList.add('active-mobile');
            }
            // Reset desktop classes/styles
            body.classList.remove('sidebar-collapsed');
            sidebar.classList.remove('collapsed'); // Remove desktop collapsed class if present
        } else {
            // Desktop: Use push content with 'collapsed' class on sidebar and body
            if (collapse) {
                sidebar.classList.add('collapsed');
                body.classList.add('sidebar-collapsed');
                localStorage.setItem('sidebarCollapsed', 'true');
            } else {
                sidebar.classList.remove('collapsed');
                body.classList.remove('sidebar-collapsed');
                localStorage.setItem('sidebarCollapsed', 'false');
            }
            // Reset mobile class
            sidebar.classList.remove('active-mobile');
        }
    }

    function toggleDesktopSidebar() {
        if (!sidebar) return;
        const isCurrentlyCollapsed = sidebar.classList.contains('collapsed');
        applySidebarState(!isCurrentlyCollapsed); // Toggle state
    }

    function toggleMobileSidebar() {
        if (!sidebar) return;
        const isCurrentlyActive = sidebar.classList.contains('active-mobile');
        applySidebarState(isCurrentlyActive); // If active, collapse it; if inactive, expand it.
    }

    // --- Theme Toggle Logic ---
    function applyTheme(isDark) {
        const currentTheme = isDark ? 'dark' : 'light';
        // Set theme attribute on root element
        document.documentElement.setAttribute('data-bs-theme', currentTheme);
        // Save preference
        localStorage.setItem('theme', currentTheme);

        // Update slider checkbox state
        if (themeToggleCheckbox) {
            themeToggleCheckbox.checked = isDark;
        }
        // Update slider label text
        if (themeToggleLabel) {
            themeToggleLabel.textContent = isDark ? 'Dark Mode' : 'Light Mode';
        }
        // Update icon button's icon and aria-label
        if (themeToggleIcon) {
            // Show SUN icon if currently DARK (indicates click switches to light)
            // Show MOON icon if currently LIGHT (indicates click switches to dark)
            themeToggleIcon.className = isDark ? 'bi bi-sun-fill' : 'bi bi-moon-fill';
        }
        if (themeToggleIconButton) {
            themeToggleIconButton.setAttribute('aria-label', isDark ? 'Switch to light theme' : 'Switch to dark theme');
        }
    }

    // --- Initial Load Logic ---

    // 1. Apply initial theme based on localStorage or default (dark)
    const savedTheme = localStorage.getItem('theme') || 'dark';
    applyTheme(savedTheme === 'dark'); // Applies theme & updates controls

    // 2. Apply initial sidebar state based on localStorage and screen size
    let initialSidebarCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (window.innerWidth < MOBILE_BREAKPOINT) {
        initialSidebarCollapsed = true; // Default to collapsed (hidden overlay) on mobile load
    }
    // Apply state without transition on initial load to prevent flicker
    if (sidebar) sidebar.style.transition = 'none';
    if (body) body.style.transition = 'none';
    applySidebarState(initialSidebarCollapsed);
    // Re-enable transitions shortly after load
    setTimeout(() => {
        if (sidebar) sidebar.style.transition = '';
        if (body) body.style.transition = '';
    }, 50);


    // --- Event Listeners ---

    // Desktop Sidebar Toggle
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', toggleDesktopSidebar);
    }

    // Mobile Sidebar Toggle
    if (mobileSidebarToggle) {
        mobileSidebarToggle.addEventListener('click', toggleMobileSidebar);
    }

    // Theme Toggle (Slider Checkbox)
    if (themeToggleCheckbox) {
        themeToggleCheckbox.addEventListener('change', function () {
            applyTheme(this.checked);
        });
        // Also allow clicking the wrapper (excluding the input itself) to toggle
        const themeWrapper = document.getElementById('theme-switch-expanded');
        if (themeWrapper) {
            themeWrapper.addEventListener('click', (event) => {
                // Only trigger if the click wasn't directly on the input/slider elements
                if (event.target.closest('.theme-switch') === null) {
                    themeToggleCheckbox.click();
                }
            });
        }
    }

    // Theme Toggle (Collapsed Icon Button)
    if (themeToggleIconButton) {
        themeToggleIconButton.addEventListener('click', function () {
            const isCurrentlyDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
            applyTheme(!isCurrentlyDark); // Apply the opposite theme
        });
    }

    // Close mobile sidebar overlay if clicking outside
    document.addEventListener('click', function (event) {
        if (window.innerWidth < MOBILE_BREAKPOINT && sidebar && sidebar.classList.contains('active-mobile')) {
            const isClickInsideSidebar = sidebar.contains(event.target);
            const isClickOnToggler = mobileSidebarToggle && mobileSidebarToggle.contains(event.target);

            // Check if click is on any part of the theme toggle components
            const isClickOnThemeCheckbox = themeToggleCheckbox && themeToggleCheckbox.contains(event.target);
            const isClickOnThemeIconBtn = themeToggleIconButton && themeToggleIconButton.contains(event.target);
            const isClickOnThemeWrapper = document.getElementById('theme-switch-expanded')?.contains(event.target);

            // If click is outside sidebar AND outside toggler AND outside theme controls
            if (!isClickInsideSidebar && !isClickOnToggler && !isClickOnThemeCheckbox && !isClickOnThemeIconBtn && !isClickOnThemeWrapper) {
                applySidebarState(true); // Collapse mobile sidebar
            }
        }
    });

    // Re-apply sidebar state on resize to switch between push/overlay correctly
    window.addEventListener('resize', () => {
        let shouldBeCollapsed;
        // Determine the *intended* state based on current window size and stored preference
        if (window.innerWidth < MOBILE_BREAKPOINT) {
            // On mobile, the overlay is considered 'collapsed' when hidden
            shouldBeCollapsed = true; // Default to hidden on resize to mobile (unless active)
            if (sidebar && sidebar.classList.contains('active-mobile')) {
                shouldBeCollapsed = false; // Keep it open if it was already active
            }
        } else {
            // On desktop, use the stored preference
            shouldBeCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        }
        // Use timeout to debounce resize events slightly
        setTimeout(() => applySidebarState(shouldBeCollapsed), 50);
    });

}); // End DOMContentLoaded
