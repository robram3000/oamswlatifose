// src/api/axiosConfig.js
import axios from 'axios';
import EncryptionUtil from '../utils/encryption';

/**
 * Axios Configuration Module
 * 
 * This module configures and exports an axios instance with comprehensive interceptors
 * for request/response handling, security features, and token refresh functionality.
 * It serves as the foundation for all API communications in the application.
 * 
 * @module axiosConfig
 */

const BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';

/**
 * Create and configure the main axios instance
 * 
 * The instance is configured with:
 * - Base URL from environment variables
 * - 30-second timeout for requests
 * - Standard JSON headers
 * - Client version identification
 * - Cookie support enabled
 * 
 * @constant {axios.AxiosInstance}
 */
const apiClient = axios.create({
  baseURL: BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
    'X-Client-Version': '1.0.0'
  },
  withCredentials: true // Important for cookies
});

/**
 * Request Interceptor
 * 
 * This interceptor runs before every request and adds:
 * - Security headers (timestamp, request ID)
 * - Authorization tokens
 * - Encryption for sensitive endpoints
 * 
 * @param {Object} config - The axios request configuration
 * @returns {Object} Modified request configuration
 * @throws {Error} If request configuration fails
 */
apiClient.interceptors.request.use(
  async (config) => {
    // Add timestamp to prevent replay attacks
    // This helps prevent request replay attacks by making each request unique
    config.headers['X-Timestamp'] = Date.now();
    
    // Add request ID for tracking
    // Useful for debugging and tracing requests through logs
    config.headers['X-Request-ID'] = generateRequestId();
    
    // Encrypt sensitive data in request body
    // Automatically encrypts data for authentication endpoints
    if (config.data && config.method !== 'get') {
      const sensitiveEndpoints = [
        '/auth/login',
        '/auth/register',
        '/auth/change-password',
        '/auth/reset-password'
      ];
      
      if (sensitiveEndpoints.some(endpoint => config.url.includes(endpoint))) {
        config.data = {
          encrypted: EncryptionUtil.encrypt(config.data),
          signature: EncryptionUtil.generateSignature(config.data)
        };
      }
    }
    
    // Add authorization header if token exists
    // Automatically attaches JWT token to every request
    const token = localStorage.getItem('access_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    
    return config;
  },
  (error) => {
    // Handle request configuration errors
    return Promise.reject(error);
  }
);

/**
 * Response Interceptor
 * 
 * This interceptor processes all responses and handles:
 * - Decryption of encrypted responses
 * - Token refresh on 401 errors
 * - Automatic redirection on authentication failure
 * 
 * @param {Object} response - The axios response object
 * @returns {Object} Processed response data
 * @throws {Error} If response processing fails
 */
apiClient.interceptors.response.use(
  (response) => {
    // Decrypt encrypted responses if needed
    // Automatically decrypts data marked as encrypted
    if (response.data && response.data.encrypted) {
      response.data = EncryptionUtil.decrypt(response.data.encrypted);
    }
    return response;
  },
  async (error) => {
    const originalRequest = error.config;
    
    // Handle token refresh on 401
    // Attempts to refresh the access token when receiving a 401 error
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      
      try {
        const refreshToken = localStorage.getItem('refresh_token');
        const response = await apiClient.post('/auth/refresh', { refreshToken });
        
        if (response.data.isSuccess) {
          // Store new tokens
          localStorage.setItem('access_token', response.data.data.accessToken);
          localStorage.setItem('refresh_token', response.data.data.refreshToken);
          
          // Retry the original request with new token
          originalRequest.headers.Authorization = `Bearer ${response.data.data.accessToken}`;
          return apiClient(originalRequest);
        }
      } catch (refreshError) {
        // Redirect to login on refresh failure
        // Clear session and redirect user to login page
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }
    
    return Promise.reject(error);
  }
);

/**
 * Generate a unique request ID for tracking
 * 
 * Creates a RFC4122 version 4 compliant UUID for request identification.
 * Used in headers to track requests through logs and debugging.
 * 
 * @private
 * @function generateRequestId
 * @returns {string} A unique UUID v4 string
 * 
 * @example
 * // Returns: '123e4567-e89b-12d3-a456-426614174000'
 * const requestId = generateRequestId();
 */
const generateRequestId = () => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
};

export default apiClient;