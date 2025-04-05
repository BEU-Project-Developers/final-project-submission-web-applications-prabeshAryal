    document.addEventListener('DOMContentLoaded', function () {
            const sidebar = document.getElementById('sidebar');
    const body = document.body;
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const mobileSidebarToggle = document.getElementById('mobile-sidebar-toggle');
    const themeToggle = document.getElementById('theme-toggle');
    const themeLabel = document.querySelector('.theme-switch-wrapper .sidebar-text'); // Target the span for text update

    const MOBILE_BREAKPOINT = 992;

    function applySidebarState(isCollapsed) {
                if (!sidebar) return; // Guard clause

    if (window.innerWidth < MOBILE_BREAKPOINT) {
                     // Mobile logic (Overlay)
                     if (isCollapsed) {
        sidebar.classList.remove('active-mobile');
                     } else {
        sidebar.classList.add('active-mobile');
                     }
    body.classList.remove('sidebar-collapsed'); // Ensure body padding is correct for overlay
                } else {
        // Desktop logic (Push content)
        sidebar.classList.remove('active-mobile'); // Ensure mobile class is removed
    if (isCollapsed) {
        sidebar.classList.add('collapsed');
    body.classList.add('sidebar-collapsed');
    localStorage.setItem('sidebarCollapsed', 'true');
                    } else {
        sidebar.classList.remove('collapsed');
    body.classList.remove('sidebar-collapsed');
    localStorage.setItem('sidebarCollapsed', 'false');
                    }
                }
            }

    function toggleDesktopSidebar() {
                 if (!sidebar) return;
    const isCurrentlyCollapsed = sidebar.classList.contains('collapsed');
    applySidebarState(!isCurrentlyCollapsed);
             }

    function toggleMobileSidebar() {
                 if (!sidebar) return;
    const isCurrentlyActive = sidebar.classList.contains('active-mobile');
    applySidebarState(isCurrentlyActive);
             }

    // Theme Toggle Logic
    function applyTheme(isDark) {
                 const currentTheme = isDark ? 'dark' : 'light';
    document.documentElement.setAttribute('data-bs-theme', currentTheme);
    localStorage.setItem('theme', currentTheme);

    // Update checkbox state
    if(themeToggle) {
        themeToggle.checked = isDark;
                 }
    // Update label text
    if(themeLabel) {
        themeLabel.textContent = isDark ? 'Dark Mode' : 'Light Mode';
                 }
             }

    if (themeToggle) {
        themeToggle.addEventListener('change', function () {
            applyTheme(this.checked);
        });
             }

    // --- Initial Load ---

    // 1. Apply initial theme
    const savedTheme = localStorage.getItem('theme') || 'dark'; // Default to dark if nothing saved
    applyTheme(savedTheme === 'dark'); // This now also sets checkbox and label

    // 2. Apply initial sidebar state
    let initialSidebarCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (window.innerWidth < MOBILE_BREAKPOINT) {
        initialSidebarCollapsed = true; // Default to collapsed (hidden) on mobile
             }
    applySidebarState(initialSidebarCollapsed);


    // --- Event Listeners ---
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', toggleDesktopSidebar);
             }
    if (mobileSidebarToggle) {
        mobileSidebarToggle.addEventListener('click', toggleMobileSidebar);
             }

    // Optional: Close mobile sidebar overlay if clicking outside
    document.addEventListener('click', function(event) {
                 if (window.innerWidth < MOBILE_BREAKPOINT && sidebar && sidebar.classList.contains('active-mobile')) {
                     const isClickInsideSidebar = sidebar.contains(event.target);
    const isClickOnToggler = mobileSidebarToggle && mobileSidebarToggle.contains(event.target);
    if (!isClickInsideSidebar && !isClickOnToggler) {
        applySidebarState(true); // Collapse sidebar
                     }
                 }
             });

             // Re-apply sidebar state on resize to switch between push/overlay
             window.addEventListener('resize', () => {
        let currentCollapsedState;
    if (window.innerWidth < MOBILE_BREAKPOINT) {
        currentCollapsedState = !(sidebar && sidebar.classList.contains('active-mobile'));
                 } else {
        currentCollapsedState = body.classList.contains('sidebar-collapsed');
                 }
                 // Small delay to prevent rapid toggling during resize drag
                 setTimeout(() => applySidebarState(currentCollapsedState), 50);
             });

        });
