import apiClient from './axiosConfig';
import EncryptionUtil from '../utils/encryption';

/**
 * ApiService - A comprehensive HTTP client service for making API requests
 * 
 * This service provides a wrapper around axios with built-in error handling,
 * file upload/download capabilities, and optional encryption support.
 * It follows a consistent pattern for all HTTP methods and provides
 * standardized error responses.
 * 
 * @class ApiService
 * @example
 * const userService = new ApiService('/api/users');
 * const users = await userService.get();
 */
class ApiService {
  /**
   * Creates an instance of ApiService for a specific API endpoint
   * 
   * @constructor
   * @param {string} endpoint - The base endpoint for API calls (e.g., '/api/users')
   */
  constructor(endpoint) {
    this.endpoint = endpoint;
  }

  /**
   * Performs an HTTP GET request
   * 
   * @async
   * @param {string} [path=''] - Additional path to append to the base endpoint
   * @param {Object} [params={}] - Query parameters to include in the request
   * @param {boolean} [encryptResponse=false] - Whether to encrypt the response data
   * @returns {Promise<Object>} Response data from the server
   * @throws {Object} Standardized error object
   * 
   * @example
   * // Simple GET request
   * const users = await userService.get();
   * 
   * // GET request with query parameters
   * const filteredUsers = await userService.get('/active', { role: 'admin' });
   * 
   * // GET request with encrypted response
   * const encryptedData = await userService.get('/profile', {}, true);
   */
  async get(path = '', params = {}, encryptResponse = false) {
    try {
      const response = await apiClient.get(`${this.endpoint}${path}`, { params });
      
      if (encryptResponse) {
        return {
          ...response,
          data: {
            ...response.data,
            data: EncryptionUtil.encrypt(response.data.data)
          }
        };
      }
      
      return response.data;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Performs an HTTP POST request with optional encryption support
   * 
   * @async
   * @param {string} [path=''] - Additional path to append to the base endpoint
   * @param {Object} [data={}] - Request body data to send
   * @param {boolean} [encryptRequest=false] - Whether to encrypt the request data
   * @param {boolean} [encryptResponse=false] - Whether to encrypt the response data
   * @returns {Promise<Object>} Response data from the server
   * @throws {Object} Standardized error object
   * 
   * @example
   * // Simple POST request
   * const newUser = await userService.post('', { name: 'John', email: 'john@example.com' });
   * 
   * // POST request with encrypted request
   * const secureUser = await userService.post('/secure', sensitiveData, true);
   * 
   * // POST request with both request and response encryption
   * const encryptedUser = await userService.post('/secure', sensitiveData, true, true);
   */
  async post(path = '', data = {}, encryptRequest = false, encryptResponse = false) {
    try {
      let requestData = data;
      
      if (encryptRequest) {
        requestData = {
          encrypted: EncryptionUtil.encrypt(data),
          signature: EncryptionUtil.generateSignature(data)
        };
      }
      
      const response = await apiClient.post(`${this.endpoint}${path}`, requestData);
      
      if (encryptResponse && response.data.data) {
        return {
          ...response.data,
          data: EncryptionUtil.encrypt(response.data.data)
        };
      }
      
      return response.data;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Performs an HTTP PUT request for updating resources
   * 
   * @async
   * @param {string} [path=''] - Additional path to append to the base endpoint (usually includes ID)
   * @param {Object} [data={}] - Updated data to send
   * @param {boolean} [encryptRequest=false] - Whether to encrypt the request data
   * @param {boolean} [encryptResponse=false] - Whether to encrypt the response data
   * @returns {Promise<Object>} Response data from the server
   * @throws {Object} Standardized error object
   * 
   * @example
   * // Simple PUT request
   * const updatedUser = await userService.put('/123', { name: 'Updated Name' });
   * 
   * // PUT request with encrypted data
   * const secureUpdate = await userService.put('/123/secure', sensitiveData, true);
   */
  async put(path = '', data = {}, encryptRequest = false, encryptResponse = false) {
    try {
      let requestData = data;
      
      if (encryptRequest) {
        requestData = {
          encrypted: EncryptionUtil.encrypt(data),
          signature: EncryptionUtil.generateSignature(data)
        };
      }
      
      const response = await apiClient.put(`${this.endpoint}${path}`, requestData);
      
      if (encryptResponse && response.data.data) {
        return {
          ...response.data,
          data: EncryptionUtil.encrypt(response.data.data)
        };
      }
      
      return response.data;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Performs an HTTP DELETE request
   * 
   * @async
   * @param {string} [path=''] - Additional path to append to the base endpoint (usually includes ID)
   * @param {Object} [params={}] - Query parameters for the delete operation
   * @returns {Promise<Object>} Response data from the server
   * @throws {Object} Standardized error object
   * 
   * @example
   * // Simple DELETE request
   * const result = await userService.delete('/123');
   * 
   * // DELETE request with query parameters
   * const result = await userService.delete('/123', { permanent: true });
   */
  async delete(path = '', params = {}) {
    try {
      const response = await apiClient.delete(`${this.endpoint}${path}`, { params });
      return response.data;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Uploads a file with progress tracking
   * 
   * @async
   * @param {string} [path=''] - Additional path for the upload endpoint
   * @param {File} file - File object to upload
   * @param {Function} [onProgress=null] - Callback function for upload progress (receives percentage)
   * @returns {Promise<Object>} Response data from the server
   * @throws {Object} Standardized error object
   * 
   * @example
   * // Upload with progress tracking
   * const file = document.querySelector('input[type="file"]').files[0];
   * const result = await userService.upload('/avatar', file, (percent) => {
   *   console.log(`Upload progress: ${percent}%`);
   * });
   */
  async upload(path = '', file, onProgress = null) {
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await apiClient.post(`${this.endpoint}${path}`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
          if (onProgress) {
            const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
            onProgress(percentCompleted);
          }
        },
      });

      return response.data;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Downloads a file from the server
   * 
   * @async
   * @param {string} [path=''] - Additional path for the download endpoint
   * @param {Object} [params={}] - Query parameters for the download
   * @param {string} [fileName='download'] - Name to save the downloaded file as
   * @returns {Promise<Blob>} The downloaded file as a Blob
   * @throws {Object} Standardized error object
   * 
   * @example
   * // Download a file
   * await userService.download('/export', { format: 'pdf' }, 'users-report.pdf');
   * 
   * // Download with custom parameters
   * await userService.download('/documents/123', { version: 'latest' }, 'document.pdf');
   */
  async download(path = '', params = {}, fileName = 'download') {
    try {
      const response = await apiClient.get(`${this.endpoint}${path}`, {
        params,
        responseType: 'blob',
      });

      // Create a download link and trigger the download
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);

      return response.data;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Handles and standardizes API errors
   * 
   * @private
   * @param {Error} error - The error object from axios
   * @returns {Object} Standardized error object with consistent structure
   * @property {boolean} isSuccess - Always false for errors
   * @property {string} message - Human-readable error message
   * @property {number} statusCode - HTTP status code or custom code (0 for no response, -1 for other errors)
   * @property {null} data - Always null for errors
   * 
   * @example
   * // Returns error object like:
   * // {
   * //   isSuccess: false,
   * //   message: 'User not found',
   * //   statusCode: 404,
   * //   data: null
   * // }
   */
  handleError(error) {
    if (error.response) {
      // Server responded with error status code (4xx, 5xx)
      return {
        isSuccess: false,
        message: error.response.data.message || 'Server error occurred',
        statusCode: error.response.status,
        data: null
      };
    } else if (error.request) {
      // Request was made but no response received (network error)
      return {
        isSuccess: false,
        message: 'No response from server. Please check your connection.',
        statusCode: 0,
        data: null
      };
    } else {
      // Error occurred in setting up the request
      return {
        isSuccess: false,
        message: error.message || 'An unexpected error occurred',
        statusCode: -1,
        data: null
      };
    }
  }
}

export default ApiService;