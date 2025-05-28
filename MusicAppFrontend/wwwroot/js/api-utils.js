/**
 * Enhanced API response handling utilities for the Music App
 * Provides consistent error handling for empty responses and false status scenarios
 */

window.MusicAppUtils = window.MusicAppUtils || {};

/**
 * Safely handles API responses with consistent error handling
 * @param {Response} response - The fetch response object
 * @param {string} operation - The operation being performed (for error messages)
 * @returns {Promise<Object>} - The parsed response data with success flag
 */
window.MusicAppUtils.handleApiResponse = async function(response, operation = 'operation') {
    let data;
    
    try {
        const responseText = await response.text();
        
        // Handle empty responses
        if (!responseText || responseText.trim() === '') {
            console.warn(`Empty response received for ${operation}`);
            return { 
                success: false, 
                message: 'Empty response from server',
                data: null
            };
        }
        
        // Try to parse as JSON
        try {
            data = JSON.parse(responseText);
            console.log(`${operation} response data:`, data);
        } catch (jsonError) {
            console.error(`Error parsing ${operation} response JSON:`, jsonError);
            return { 
                success: false, 
                message: 'Invalid response format from server',
                data: null
            };
        }
    } catch (fetchError) {
        console.error(`Error reading ${operation} response:`, fetchError);
        return {
            success: false,
            message: 'Failed to read server response',
            data: null
        };
    }
    
    // Handle null/undefined data
    if (data === null || data === undefined) {
        return { 
            success: false, 
            message: 'Empty response from server',
            data: null
        };
    }
    
    // Check for explicit success=false in response
    if (typeof data === 'object' && data.success === false) {
        console.warn(`${operation} returned success=false:`, data.message);
        return {
            success: false,
            message: data.message || data.Message || `${operation} operation failed`,
            data: data
        };
    }
    
    // For non-ok responses, extract error message
    if (!response.ok) {
        const errorMessage = typeof data === 'string' ? data : 
                           data.message || data.Message || 
                           `${operation} failed with status ${response.status}`;
        return {
            success: false,
            message: errorMessage,
            data: data
        };
    }
    
    return {
        success: true,
        message: data.message || 'Operation completed successfully',
        data: data
    };
};

/**
 * Enhanced alert display with better error message handling
 * @param {Element} alertElement - The alert element to show message in
 * @param {string|Object} message - The message to display
 * @param {string} type - The alert type (success, danger, etc.)
 */
window.MusicAppUtils.showAlert = function(alertElement, message, type) {
    if (!alertElement) {
        console.error('Alert element not found');
        return;
    }
    
    // Extract message from object if necessary
    let displayMessage = message;
    if (typeof message === 'object' && message !== null) {
        if (message.errors) {
            // Handle validation errors format
            const errorList = [];
            Object.keys(message.errors).forEach(key => {
                if (Array.isArray(message.errors[key])) {
                    message.errors[key].forEach(error => errorList.push(error));
                } else {
                    errorList.push(message.errors[key]);
                }
            });
            displayMessage = errorList.join(', ');
        } else {
            displayMessage = message.message || message.Message || 'An error occurred';
        }
    }
    
    alertElement.className = `alert alert-${type}`;
    alertElement.textContent = displayMessage;
    alertElement.style.display = 'block';
    
    // Auto-hide success messages after 5 seconds
    if (type === 'success') {
        setTimeout(() => {
            alertElement.style.display = 'none';
        }, 5000);
    }
};

/**
 * Performs safe API call with built-in error handling and user feedback
 * @param {string} url - The API endpoint URL
 * @param {Object} options - Fetch options (method, headers, body, etc.)
 * @param {Element} alertElement - Element to show error messages in
 * @param {string} operation - Operation name for logging and error messages
 * @returns {Promise<Object>} - Result with success flag and data
 */
window.MusicAppUtils.safeApiCall = async function(url, options = {}, alertElement = null, operation = 'API call') {
    try {
        const response = await fetch(url, {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        });
        
        const result = await window.MusicAppUtils.handleApiResponse(response, operation);
        
        if (!result.success && alertElement) {
            window.MusicAppUtils.showAlert(alertElement, result.message, 'danger');
        }
        
        return result;
    } catch (error) {
        console.error(`Network error in ${operation}:`, error);
        
        let errorMessage = `An error occurred during ${operation}.`;
        
        if (!navigator.onLine) {
            errorMessage = 'You appear to be offline. Please check your internet connection and try again.';
        } else if (error.name === 'TypeError' && error.message === 'Failed to fetch') {
            errorMessage = 'Cannot connect to the server. Please ensure the server is running and accessible.';
        } else if (error.message) {
            errorMessage += ' ' + error.message;
        }
        
        if (alertElement) {
            window.MusicAppUtils.showAlert(alertElement, errorMessage, 'danger');
        }
        
        return {
            success: false,
            message: errorMessage,
            data: null
        };
    }
};

/**
 * Validates that response data contains expected properties for collection operations
 * @param {Object} data - The response data to validate
 * @param {Array<string>} requiredProps - Required properties for the data
 * @returns {Object} - Validation result with success flag and message
 */
window.MusicAppUtils.validateResponseData = function(data, requiredProps = []) {
    if (!data) {
        return {
            success: false,
            message: 'No data received from server'
        };
    }
    
    // Check for required properties
    for (const prop of requiredProps) {
        if (!(prop in data)) {
            return {
                success: false,
                message: `Invalid response format: missing ${prop} property`
            };
        }
    }
    
    // For paginated responses, ensure Data is an array
    if (data.Data && !Array.isArray(data.Data)) {
        return {
            success: false,
            message: 'Invalid response format: Data should be an array'
        };
    }
    
    return {
        success: true,
        message: 'Response data is valid'
    };
};

console.log('MusicApp enhanced error handling utilities loaded');
