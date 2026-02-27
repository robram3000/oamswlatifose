// src/api/authApi.js
import ApiService from './apiService';
import EncryptionUtil from '../utils/encryption';

/**
 * Authentication API Service
 * 
 * Handles all authentication-related API calls including login, registration,
 * password management, and session handling. Implements secure token storage
 * and automatic encryption for sensitive data.
 * 
 * @class AuthApi
 * @extends ApiService
 */
class AuthApi extends ApiService {
  /**
   * Creates an instance of AuthApi
   * Sets the base endpoint to '/auth' for all authentication requests
   * 
   * @constructor
   */
  constructor() {
    super('/auth');
  }

  /**
   * Authenticate user with credentials
   * 
   * Encrypts password before transmission and securely stores returned tokens.
   * Implements secure credential handling with field-level encryption.
   * 
   * @async
   * @param {Object} credentials - User login credentials
   * @param {string} credentials.email - User's email address
   * @param {string} credentials.password - User's password (will be encrypted)
   * @returns {Promise<Object>} Response containing user data and tokens
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await authApi.login({
   *   email: 'user@example.com',
   *   password: 'securePassword123'
   * });
   * if (response.isSuccess) {
   *   // User is logged in
   * }
   */
  async login(credentials) {
    // Encrypt password before sending
    const encryptedCredentials = EncryptionUtil.encryptSensitiveFields(credentials, ['password']);
    
    const response = await this.post('/login', encryptedCredentials, true, false);
    
    if (response.isSuccess) {
      // Store tokens securely
      localStorage.setItem('access_token', response.data.accessToken);
      localStorage.setItem('refresh_token', response.data.refreshToken);
      localStorage.setItem('user', EncryptionUtil.encrypt(response.data.user));
    }
    
    return response;
  }

  /**
   * Register a new user account
   * 
   * Creates a new user account with encrypted sensitive fields.
   * Automatically encrypts password fields for secure transmission.
   * 
   * @async
   * @param {Object} userData - User registration data
   * @param {string} userData.email - User's email address
   * @param {string} userData.password - User's chosen password
   * @param {string} userData.confirmPassword - Password confirmation
   * @param {string} [userData.firstName] - User's first name
   * @param {string} [userData.lastName] - User's last name
   * @returns {Promise<Object>} Response containing registration result
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await authApi.register({
   *   email: 'newuser@example.com',
   *   password: 'SecurePass123',
   *   confirmPassword: 'SecurePass123',
   *   firstName: 'John',
   *   lastName: 'Doe'
   * });
   */
  async register(userData) {
    const encryptedData = EncryptionUtil.encryptSensitiveFields(userData, ['password', 'confirmPassword']);
    return this.post('/register', encryptedData, true, false);
  }

  /**
   * Refresh the access token
   * 
   * Uses the refresh token to obtain a new access token without
   * requiring the user to re-authenticate.
   * 
   * @async
   * @param {string} refreshToken - The refresh token from previous authentication
   * @returns {Promise<Object>} Response containing new access and refresh tokens
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await authApi.refreshToken(storedRefreshToken);
   */
  async refreshToken(refreshToken) {
    return this.post('/refresh', { refreshToken }, true, false);
  }

  /**
   * Log out the current user
   * 
   * Invalidates the session on the server and clears all stored
   * authentication data from local storage.
   * 
   * @async
   * @returns {Promise<Object>} Response confirming logout
   * @throws {Object} Standardized error object
   * 
   * @example
   * await authApi.logout();
   * // User is now logged out
   */
  async logout() {
    const response = await this.post('/logout', {});
    
    // Clear local storage
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
    
    return response;
  }

  /**
   * Change user's password
   * 
   * Allows authenticated user to change their password.
   * All password fields are encrypted before transmission.
   * 
   * @async
   * @param {Object} passwordData - Password change data
   * @param {string} passwordData.currentPassword - User's current password
   * @param {string} passwordData.newPassword - Desired new password
   * @param {string} passwordData.confirmPassword - New password confirmation
   * @returns {Promise<Object>} Response indicating password change status
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await authApi.changePassword({
   *   currentPassword: 'oldPass123',
   *   newPassword: 'newPass456',
   *   confirmPassword: 'newPass456'
   * });
   */
  async changePassword(passwordData) {
    const encryptedData = EncryptionUtil.encryptSensitiveFields(passwordData, ['currentPassword', 'newPassword', 'confirmPassword']);
    return this.post('/change-password', encryptedData, true, false);
  }

  /**
   * Initiate password reset process
   * 
   * Sends a password reset email to the specified address.
   * 
   * @async
   * @param {string} email - Email address of the account
   * @returns {Promise<Object>} Response indicating reset email sent
   * @throws {Object} Standardized error object
   * 
   * @example
   * await authApi.forgotPassword('user@example.com');
   */
  async forgotPassword(email) {
    return this.post('/forgot-password', { email }, true, false);
  }

  /**
   * Reset password with token
   * 
   * Completes the password reset process using the token received via email.
   * 
   * @async
   * @param {Object} resetData - Password reset data
   * @param {string} resetData.token - Reset token from email
   * @param {string} resetData.password - New password
   * @param {string} resetData.confirmPassword - Password confirmation
   * @returns {Promise<Object>} Response indicating reset completion
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await authApi.resetPassword({
   *   token: 'reset-token-from-email',
   *   password: 'newPassword123',
   *   confirmPassword: 'newPassword123'
   * });
   */
  async resetPassword(resetData) {
    const encryptedData = EncryptionUtil.encryptSensitiveFields(resetData, ['password', 'confirmPassword']);
    return this.post('/reset-password', encryptedData, true, false);
  }

  /**
   * Get current authenticated user's data
   * 
   * Retrieves the profile data of the currently logged-in user.
   * Caches the encrypted user data in local storage.
   * 
   * @async
   * @returns {Promise<Object>} Response containing user profile data
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await authApi.getCurrentUser();
   * if (response.isSuccess) {
   *   console.log(response.data.user);
   * }
   */
  async getCurrentUser() {
    const response = await this.get('/me');
    
    if (response.isSuccess && response.data) {
      // Cache encrypted user data
      localStorage.setItem('user', EncryptionUtil.encrypt(response.data));
    }
    
    return response;
  }

  /**
   * Verify user's email address
   * 
   * Completes email verification process using the token sent via email.
   * 
   * @async
   * @param {string} userId - ID of the user to verify
   * @param {string} token - Verification token from email
   * @returns {Promise<Object>} Response indicating verification status
   * @throws {Object} Standardized error object
   * 
   * @example
   * await authApi.verifyEmail('user123', 'verification-token');
   */
  async verifyEmail(userId, token) {
    return this.get('/verify-email', { userId, token });
  }

  /**
   * Retrieve cached user data
   * 
   * Gets the user data from local storage cache without making an API call.
   * Data is automatically decrypted before return.
   * 
   * @returns {Object|null} Decrypted user data or null if not found
   * 
   * @example
   * const user = authApi.getCachedUser();
   * if (user) {
   *   console.log(user.firstName);
   * }
   */
  getCachedUser() {
    const encryptedUser = localStorage.getItem('user');
    if (encryptedUser) {
      return EncryptionUtil.decrypt(encryptedUser);
    }
    return null;
  }

  /**
   * Check if user is currently authenticated
   * 
   * Verifies the presence of an access token in local storage.
   * Does not validate token validity with the server.
   * 
   * @returns {boolean} True if access token exists, false otherwise
   * 
   * @example
   * if (authApi.isAuthenticated()) {
   *   // Show authenticated content
   * }
   */
  isAuthenticated() {
    return !!localStorage.getItem('access_token');
  }
}

// Export a singleton instance
export default new AuthApi();