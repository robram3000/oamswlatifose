import CryptoJS from 'crypto-js';

/**
 * Encryption Key for AES-256 operations
 * 
 * Retrieved from environment variables with a fallback for development.
 * IMPORTANT: Always set a strong, unique key in production environment!
 * 
 * @constant {string}
 * @default 'your-secret-key-here-change-in-production'
 * @security This key must be kept secret and never exposed client-side in production
 */
const ENCRYPTION_KEY = process.env.REACT_APP_ENCRYPTION_KEY || 'your-secret-key-here-change-in-production';

/**
 * Initialization Vector length for AES encryption
 * 
 * @constant {number}
 * @default 16
 */
const IV_LENGTH = 16;

/**
 * Encryption Utility Class
 * 
 * Provides comprehensive encryption, decryption, and signature generation
 * functionality for securing sensitive data in the application. Implements:
 * - AES-256 encryption for data protection
 * - HMAC-SHA256 signatures for request integrity
 * - Field-level encryption for sensitive form data
 * - Automatic JSON serialization/deserialization
 * 
 * @class EncryptionUtil
 * @static
 * 
 * @example
 * // Encrypt sensitive data
 * const encrypted = EncryptionUtil.encrypt({ ssn: '123-45-6789' });
 * 
 * // Decrypt data
 * const decrypted = EncryptionUtil.decrypt(encrypted);
 * 
 * // Generate request signature
 * const signature = EncryptionUtil.generateSignature(requestData);
 * 
 * // Encrypt specific fields
 * const secureData = EncryptionUtil.encryptSensitiveFields(
 *   userData, 
 *   ['password', 'ssn', 'bankAccount']
 * );
 */
class EncryptionUtil {
  /**
   * Encrypt data using AES-256 encryption
   * 
   * Converts input data to JSON string (if not already string) and encrypts it
   * using AES-256. Returns the encrypted data as a base64 string suitable for
   * transmission or storage.
   * 
   * @static
   * @param {any} data - Data to encrypt (string, object, array, etc.)
   * @returns {string} Encrypted data as base64 string
   * @throws {Error} If encryption fails
   * 
   * @example
   * // Encrypt an object
   * const userData = { 
   *   name: 'John Doe', 
   *   ssn: '123-45-6789',
   *   creditCard: '4111-1111-1111-1111' 
   * };
   * const encrypted = EncryptionUtil.encrypt(userData);
   * // Returns: 'U2FsdGVkX1+xyz...' (base64 string)
   * 
   * @example
   * // Encrypt a simple string
   * const encryptedPassword = EncryptionUtil.encrypt('MySecretPassword123');
   */
  static encrypt(data) {
    try {
      // Convert data to JSON string if it's not already a string
      const jsonString = typeof data === 'string' ? data : JSON.stringify(data);
      
      // Perform AES encryption
      const encrypted = CryptoJS.AES.encrypt(jsonString, ENCRYPTION_KEY).toString();
      
      return encrypted;
    } catch (error) {
      console.error('Encryption error:', error);
      throw new Error('Failed to encrypt data');
    }
  }

  /**
   * Decrypt AES-256 encrypted data
   * 
   * Decrypts base64-encoded encrypted data and attempts to parse it as JSON.
   * If JSON parsing fails, returns the raw decrypted string.
   * 
   * @static
   * @param {string} encryptedData - Encrypted data as base64 string
   * @returns {any} Decrypted data (parsed JSON or raw string)
   * @throws {Error} If decryption fails
   * 
   * @example
   * // Decrypt previously encrypted data
   * const encrypted = 'U2FsdGVkX1+xyz...';
   * const decrypted = EncryptionUtil.decrypt(encrypted);
   * 
   * if (typeof decrypted === 'object') {
   *   console.log(decrypted.ssn); // Access decrypted fields
   * }
   * 
   * @example
   * // Handle decrypted data
   * try {
   *   const userData = EncryptionUtil.decrypt(storedUserData);
   *   updateUserState(userData);
   * } catch (error) {
   *   console.error('Invalid or corrupted encrypted data');
   * }
   */
  static decrypt(encryptedData) {
    try {
      // Perform AES decryption
      const bytes = CryptoJS.AES.decrypt(encryptedData, ENCRYPTION_KEY);
      const decryptedString = bytes.toString(CryptoJS.enc.Utf8);
      
      // Attempt to parse as JSON, return raw string if parsing fails
      try {
        return JSON.parse(decryptedString);
      } catch {
        return decryptedString;
      }
    } catch (error) {
      console.error('Decryption error:', error);
      throw new Error('Failed to decrypt data');
    }
  }

  /**
   * Generate HMAC-SHA256 signature for request validation
   * 
   * Creates a cryptographic signature combining timestamp and request data.
   * Used to prevent replay attacks and verify request integrity.
   * 
   * @static
   * @param {object} data - Request data to sign
   * @returns {string} HMAC-SHA256 signature as hex string
   * 
   * @example
   * // Generate signature for API request
   * const requestData = { 
   *   userId: 123, 
   *   action: 'transfer', 
   *   amount: 1000 
   * };
   * 
   * const signature = EncryptionUtil.generateSignature(requestData);
   * 
   * // Include in API request headers
   * const headers = {
   *   'X-Signature': signature,
   *   'X-Timestamp': Date.now()
   * };
   * 
   * @example
   * // Server-side verification (pseudo-code)
   * const isValid = verifySignature(
   *   receivedSignature,
   *   requestData,
   *   request.headers['X-Timestamp']
   * );
   */
  static generateSignature(data) {
    const timestamp = Date.now();
    const payload = `${timestamp}.${JSON.stringify(data)}`;
    return CryptoJS.HmacSHA256(payload, ENCRYPTION_KEY).toString();
  }

  /**
   * Encrypt specific sensitive fields within an object
   * 
   * Creates a new object with specified fields encrypted while leaving
   * non-sensitive fields unchanged. Useful for form submissions where
   * only certain fields require encryption.
   * 
   * @static
   * @param {object} obj - Source object containing fields to encrypt
   * @param {Array<string>} fields - Array of field names to encrypt
   * @returns {object} New object with specified fields encrypted
   * 
   * @example
   * // User registration data with sensitive fields
   * const userData = {
   *   firstName: 'John',
   *   lastName: 'Doe',
   *   email: 'john@example.com',
   *   password: 'SecurePass123',
   *   ssn: '123-45-6789',
   *   creditCard: '4111-1111-1111-1111',
   *   preferences: { theme: 'dark' }
   * };
   * 
   * // Encrypt only sensitive fields
   * const securedData = EncryptionUtil.encryptSensitiveFields(
   *   userData,
   *   ['password', 'ssn', 'creditCard']
   * );
   * 
   * // Result:
   * // {
   * //   firstName: 'John',                    // Unchanged
   * //   lastName: 'Doe',                       // Unchanged
   * //   email: 'john@example.com',             // Unchanged
   * //   password: 'U2FsdGVkX1+...',            // Encrypted
   * //   ssn: 'U2FsdGVkX1+...',                 // Encrypted
   * //   creditCard: 'U2FsdGVkX1+...',          // Encrypted
   * //   preferences: { theme: 'dark' }         // Unchanged
   * // }
   * 
   * @example
   * // In an API service
   * async function updateUserProfile(userId, profileData) {
   *   const secureData = EncryptionUtil.encryptSensitiveFields(
   *     profileData,
   *     ['phoneNumber', 'address', 'emergencyContact']
   *   );
   *   
   *   return await apiClient.put(`/users/${userId}`, secureData);
   * }
   */
  static encryptSensitiveFields(obj, fields) {
    const encrypted = { ...obj };
    fields.forEach(field => {
      if (encrypted[field]) {
        encrypted[field] = this.encrypt(encrypted[field]);
      }
    });
    return encrypted;
  }
}

export default EncryptionUtil;