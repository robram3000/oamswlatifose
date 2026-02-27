// src/api/otpApi.js
import ApiService from './apiService';

/**
 * OTP (One-Time Password) API Service
 * 
 * Handles all OTP-related operations including sending verification codes,
 * password reset codes, and two-factor authentication codes.
 * 
 * @class OtpApi
 * @extends ApiService
 */
class OtpApi extends ApiService {
  /**
   * Creates an instance of OtpApi
   * Sets the base endpoint to '/requestotp' for all OTP-related requests
   * 
   * @constructor
   */
  constructor() {
    super('/requestotp');
  }

  /**
   * Send email verification OTP
   * 
   * Sends a one-time password to the user's email for account verification.
   * Used during registration or when adding a new email address.
   * 
   * @async
   * @param {string} email - Email address to send verification code to
   * @returns {Promise<Object>} Response indicating OTP sent status
   * @property {boolean} isSuccess - Whether the OTP was sent successfully
   * @property {string} message - Status message
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await otpApi.sendVerificationOTP('user@example.com');
   * if (response.isSuccess) {
   *   console.log('Verification code sent');
   * }
   */
  async sendVerificationOTP(email) {
    return this.post('/send-verification', { email });
  }

  /**
   * Send password reset OTP
   * 
   * Sends a one-time password to the user's email for password reset.
   * Used in the "forgot password" workflow.
   * 
   * @async
   * @param {string} email - Email address of the account requiring password reset
   * @returns {Promise<Object>} Response indicating OTP sent status
   * @property {boolean} isSuccess - Whether the OTP was sent successfully
   * @property {string} message - Status message
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await otpApi.sendPasswordResetOTP('user@example.com');
   * if (response.isSuccess) {
   *   // Proceed to password reset form
   * }
   */
  async sendPasswordResetOTP(email) {
    return this.post('/send-password-reset', { email });
  }

  /**
   * Send two-factor authentication OTP
   * 
   * Sends a one-time password for two-factor authentication.
   * Used during login when 2FA is enabled for the account.
   * 
   * @async
   * @returns {Promise<Object>} Response indicating OTP sent status
   * @property {boolean} isSuccess - Whether the OTP was sent successfully
   * @property {string} message - Status message
   * @property {string} data.method - Delivery method (sms/email)
   * @throws {Object} Standardized error object
   * 
   * @example
   * const response = await otpApi.sendTwoFactorOTP();
   * if (response.isSuccess) {
   *   // Show OTP input form
   * }
   */
  async sendTwoFactorOTP() {
    return this.post('/send-2fa', {});
  }
}

// Export a singleton instance
export default new OtpApi();