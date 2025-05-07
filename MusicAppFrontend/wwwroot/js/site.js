// --- site.js ---

document.addEventListener('DOMContentLoaded', function () {
    // --- Test API Connectivity ---
    const API_BASE_URL = 'http://localhost:5117'; // Make sure this matches your ApiSettings.BaseUrl
    
    // Debug API connectivity
    console.log('Testing API connectivity to:', API_BASE_URL);
    fetch(API_BASE_URL + '/api')
        .then(response => {
            if (response.ok) {
                console.log('API server is accessible!');
                return response.json();
            }
            throw new Error('API server returned status: ' + response.status);
        })
        .then(data => {
            console.log('API response data:', data.length, 'endpoints available');
        })
        .catch(error => {
            console.error('API connectivity test failed:', error);
            // Create a warning banner at the top of the page
            const banner = document.createElement('div');
            banner.className = 'alert alert-warning alert-dismissible fade show';
            banner.style.margin = '0';
            banner.style.borderRadius = '0';
            banner.innerHTML = `
                <strong>API Server Connection Issue:</strong> Cannot connect to the API server at ${API_BASE_URL}. 
                Registration and login functionality may not work properly.
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            `;
            document.body.insertBefore(banner, document.body.firstChild);
        });

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

    // --- Authentication Handling ---
    
    // Form elements
    const registerForm = document.getElementById('registerForm');
    const registerAlert = document.getElementById('registerFormAlert');
    const registerSubmitBtn = document.getElementById('registerSubmitBtn');
    const registerLoadingBtn = document.getElementById('registerLoadingBtn');
    
    const loginForm = document.getElementById('loginForm');
    const loginAlert = document.getElementById('loginFormAlert');
    const loginSubmitBtn = document.getElementById('loginSubmitBtn');
    const loginLoadingBtn = document.getElementById('loginLoadingBtn');
    
    // Reset forms when modal is closed
    const authModal = document.getElementById('authModal');
    if (authModal) {
        authModal.addEventListener('hidden.bs.modal', function() {
            // Reset register form
            if (registerForm) {
                registerForm.reset();
                registerAlert.classList.add('d-none');
                registerSubmitBtn.classList.remove('d-none');
                registerLoadingBtn.classList.add('d-none');
            }
            
            // Reset login form
            if (loginForm) {
                loginForm.reset();
                loginAlert.classList.add('d-none');
                loginSubmitBtn.classList.remove('d-none');
                loginLoadingBtn.classList.add('d-none');
            }
        });
    }
    
    // Register Form Handling
    if (registerForm) {
        registerForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            // Validate passwords match
            const password = document.getElementById('registerPassword').value;
            const confirmPassword = document.getElementById('registerConfirmPassword').value;
            
            if (password !== confirmPassword) {
                showAlert(registerAlert, 'Passwords do not match', 'danger');
                return;
            }
            
            // Switch to loading state
            registerSubmitBtn.classList.add('d-none');
            registerLoadingBtn.classList.remove('d-none');
            
            // Get form data
            const formData = {
                firstName: document.getElementById('registerFirstName').value,
                lastName: document.getElementById('registerLastName').value,
                username: document.getElementById('registerUsername').value,
                email: document.getElementById('registerEmail').value,
                password: password,
                confirmPassword: confirmPassword
            };
            
            try {
                console.log('Sending registration request to:', `${API_BASE_URL}/api/Auth/register`);
                console.log('With data:', JSON.stringify(formData, null, 2));
                
                // Call the register API directly
                const response = await fetch(`${API_BASE_URL}/api/Auth/register`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(formData)
                });
                
                console.log('Registration response status:', response.status);
                
                let data;
                try {
                    data = await response.json();
                    console.log('Registration response data:', data);
                } catch (jsonError) {
                    console.error('Error parsing registration response JSON:', jsonError);
                    data = { message: 'Error parsing server response' };
                }
                
                if (!response.ok) {
                    // Show error message
                    showAlert(registerAlert, data, 'danger');
                    
                    // Switch back to submit button
                    registerSubmitBtn.classList.remove('d-none');
                    registerLoadingBtn.classList.add('d-none');
                    return;
                }
                
                // Registration successful
                showAlert(registerAlert, 'Registration successful!', 'success');
                
                // Store the token in localStorage
                if (data.token) {
                    localStorage.setItem('jwt_token', data.token);
                }
                if (data.refreshToken) {
                    localStorage.setItem('refresh_token', data.refreshToken);
                }
                if (data.user) {
                    localStorage.setItem('user_info', JSON.stringify(data.user));
                }
                
                // Redirect to dashboard after a short delay
                setTimeout(() => {
                    window.location.href = document.querySelector('input[name="ReturnUrl"]').value || '/Account/Dashboard';
                }, 1000);
                
            } catch (error) {
                console.error('Registration error:', error);
                let errorMessage = 'An error occurred during registration.';
                
                if (error.message) {
                    errorMessage += ' ' + error.message;
                    console.error('Error message:', error.message);
                }
                
                if (error.stack) {
                    console.error('Error stack:', error.stack);
                }
                
                if (!navigator.onLine) {
                    errorMessage = 'You appear to be offline. Please check your internet connection and try again.';
                }
                
                // Try to get more information if it's a network error
                if (error.name === 'TypeError' && error.message === 'Failed to fetch') {
                    errorMessage = 'Cannot connect to the server. Please ensure the API server is running and accessible.';
                    
                    // Check if API server is running by making a simple fetch request
                    fetch(`${API_BASE_URL}/api`)
                        .then(response => {
                            if (response.ok) {
                                console.log('API server is running but registration endpoint may have issues');
                            }
                        })
                        .catch(e => {
                            console.error('API server connectivity check failed:', e);
                            errorMessage = 'Cannot connect to the API server. Please ensure it is running at ' + API_BASE_URL;
                        });
                }
                
                showAlert(registerAlert, errorMessage, 'danger');
                
                // Switch back to submit button
                registerSubmitBtn.classList.remove('d-none');
                registerLoadingBtn.classList.add('d-none');
            }
        });
    }
    
    // Login Form Handling
    if (loginForm) {
        loginForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            // Switch to loading state
            loginSubmitBtn.classList.add('d-none');
            loginLoadingBtn.classList.remove('d-none');
            
            // Get form data
            const formData = {
                email: document.getElementById('loginEmail').value,
                password: document.getElementById('loginPassword').value,
                rememberMe: document.getElementById('loginRememberMe').checked
            };
            
            try {
                console.log('Sending login request to:', `${API_BASE_URL}/api/Auth/login`);
                console.log('With data:', JSON.stringify(formData, null, 2));
                
                // Call the login API directly
                const response = await fetch(`${API_BASE_URL}/api/Auth/login`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(formData)
                });
                
                console.log('Login response status:', response.status);
                
                let data;
                try {
                    data = await response.json();
                    console.log('Login response data:', data);
                } catch (jsonError) {
                    console.error('Error parsing login response JSON:', jsonError);
                    data = { message: 'Error parsing server response' };
                }
                
                if (!response.ok) {
                    // Show error message
                    showAlert(loginAlert, data, 'danger');
                    
                    // Switch back to submit button
                    loginSubmitBtn.classList.remove('d-none');
                    loginLoadingBtn.classList.add('d-none');
                    return;
                }
                
                // Login successful
                showAlert(loginAlert, 'Login successful!', 'success');
                
                // Store the token in localStorage
                if (data.token) {
                    localStorage.setItem('jwt_token', data.token);
                }
                if (data.refreshToken) {
                    localStorage.setItem('refresh_token', data.refreshToken);
                }
                if (data.user) {
                    localStorage.setItem('user_info', JSON.stringify(data.user));
                }
                
                // Redirect to dashboard after a short delay
                setTimeout(() => {
                    window.location.href = document.querySelector('#loginForm input[name="ReturnUrl"]').value || '/Account/Dashboard';
                }, 1000);
                
            } catch (error) {
                console.error('Login error:', error);
                let errorMessage = 'An error occurred during login.';
                
                if (error.message) {
                    errorMessage += ' ' + error.message;
                }
                
                if (!navigator.onLine) {
                    errorMessage = 'You appear to be offline. Please check your internet connection and try again.';
                }
                
                showAlert(loginAlert, errorMessage, 'danger');
                
                // Switch back to submit button
                loginSubmitBtn.classList.remove('d-none');
                loginLoadingBtn.classList.add('d-none');
            }
        });
    }
    
    // Helper function to show alerts
    function showAlert(alertElement, message, type) {
        if (alertElement) {
            // Clear previous alerts
            alertElement.innerHTML = '';
            
            // Handle different message types
            if (typeof message === 'object' && message !== null) {
                // If it's an object with errors property (common API validation response)
                if (message.errors) {
                    const errorList = document.createElement('ul');
                    errorList.className = 'mb-0 ps-3';
                    
                    // Collect all error messages
                    Object.keys(message.errors).forEach(key => {
                        message.errors[key].forEach(error => {
                            const li = document.createElement('li');
                            li.textContent = error;
                            errorList.appendChild(li);
                        });
                    });
                    
                    alertElement.className = `alert alert-${type}`;
                    alertElement.classList.remove('d-none');
                    
                    // Add title if available
                    if (message.title) {
                        const strong = document.createElement('strong');
                        strong.textContent = message.title;
                        alertElement.appendChild(strong);
                        alertElement.appendChild(document.createElement('br'));
                    }
                    
                    alertElement.appendChild(errorList);
                } else {
                    // Just use string representation or message property
                    alertElement.textContent = message.message || JSON.stringify(message);
                    alertElement.className = `alert alert-${type}`;
                    alertElement.classList.remove('d-none');
                }
            } else {
                // Simple string message
                alertElement.textContent = message;
                alertElement.className = `alert alert-${type}`;
                alertElement.classList.remove('d-none');
            }
            
            // Auto-hide success alerts after 5 seconds
            if (type === 'success') {
                setTimeout(() => {
                    alertElement.classList.add('d-none');
                }, 5000);
            }
        }
    }

}); // End DOMContentLoaded
