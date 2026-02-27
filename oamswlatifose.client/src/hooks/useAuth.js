import { useState, useEffect } from 'react';
import authApi from '../api/authApi';
import EncryptionUtil from '../utils/encryption';

/**
 * Custom React hook for authentication management
 * 
 * This hook provides comprehensive authentication functionality including:
 * - User state management across the application
 * - Automatic user loading on component mount
 * - Login, logout, and registration operations
 * - Loading and error state handling
 * - Cached user data retrieval
 * 
 * @module useAuth
 * @returns {Object} Authentication state and methods
 * 
 * @example
 * // Basic usage in a component
 * function LoginComponent() {
 *   const { user, login, loading, error, isAuthenticated } = useAuth();
 * 
 *   const handleLogin = async (credentials) => {
 *     const response = await login(credentials);
 *     if (response.isSuccess) {
 *       // Redirect to dashboard
 *     }
 *   };
 * 
 *   if (loading) return <div>Loading...</div>;
 *   
 *   return (
 *     <div>
 *       {error && <div className="error">{error}</div>}
 *       {!isAuthenticated() ? (
 *         <LoginForm onSubmit={handleLogin} />
 *       ) : (
 *         <div>Welcome {user?.firstName}!</div>
 *       )}
 *     </div>
 *   );
 * }
 */
export const useAuth = () => {
  /**
   * Current authenticated user data
   * @type {[Object|null, Function]}
   */
  const [user, setUser] = useState(null);

  /**
   * Loading state for ongoing authentication operations
   * @type {[boolean, Function]}
   */
  const [loading, setLoading] = useState(true);

  /**
   * Error message from last failed authentication operation
   * @type {[string|null, Function]}
   */
  const [error, setError] = useState(null);

  /**
   * Effect hook to load user data on component mount
   * 
   * Automatically attempts to:
   * 1. Load cached user data from local storage
   * 2. Verify authentication status with server
   * 3. Fetch fresh user data if authenticated
   * 
   * @effect
   * @dependency [] - Runs once on component mount
   */
  useEffect(() => {
    loadUser();
  }, []);

  /**
   * Load user data from cache and server
   * 
   * This function:
   * 1. Sets loading state to true
   * 2. Attempts to load cached user data for immediate display
   * 3. Verifies authentication and fetches fresh data from server
   * 4. Handles any errors during the process
   * 
   * @async
   * @function loadUser
   * @throws {Error} If user data loading fails
   * 
   * @example
   * // Called automatically on mount, but can be manually triggered
   * const { loadUser } = useAuth();
   * await loadUser(); // Manually refresh user data
   */
  const loadUser = async () => {
    try {
      setLoading(true);
      // Attempt to load cached user data for immediate UI display
      const cachedUser = authApi.getCachedUser();
      
      if (cachedUser) {
        setUser(cachedUser);
      }
      
      // Verify with server and get fresh data if authenticated
      if (authApi.isAuthenticated()) {
        const response = await authApi.getCurrentUser();
        if (response.isSuccess) {
          setUser(response.data);
        }
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Authenticate user with credentials
   * 
   * Handles the login process:
   * 1. Resets any previous errors
   * 2. Sets loading state
   * 3. Calls the login API
   * 4. Updates user state on success
   * 5. Manages error state on failure
   * 
   * @async
   * @function login
   * @param {Object} credentials - User login credentials
   * @param {string} credentials.email - User's email address
   * @param {string} credentials.password - User's password
   * @returns {Promise<Object>} Response from login API
   * @throws {Error} If login fails with unexpected error
   * 
   * @example
   * const { login } = useAuth();
   * 
   * const handleSubmit = async (e) => {
   *   e.preventDefault();
   *   const response = await login({
   *     email: 'user@example.com',
   *     password: 'password123'
   *   });
   *   
   *   if (response.isSuccess) {
   *     toast.success('Login successful!');
   *   } else {
   *     toast.error(response.message);
   *   }
   * };
   */
  const login = async (credentials) => {
    try {
      setLoading(true);
      setError(null);
      const response = await authApi.login(credentials);
      
      if (response.isSuccess) {
        setUser(response.data.user);
        return response;
      } else {
        setError(response.message);
        return response;
      }
    } catch (err) {
      setError(err.message);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Log out current user
   * 
   * Handles the logout process:
   * 1. Calls the logout API
   * 2. Clears user state regardless of API response
   * 3. Sets error if logout API fails
   * 
   * @async
   * @function logout
   * @returns {Promise<void>}
   * @throws {Error} If logout fails (error is caught and stored in state)
   * 
   * @example
   * const { logout } = useAuth();
   * 
   * const handleLogout = async () => {
   *   await logout();
   *   // User is now logged out, redirect to login
   *   navigate('/login');
   * };
   */
  const logout = async () => {
    try {
      await authApi.logout();
      setUser(null);
    } catch (err) {
      setError(err.message);
    }
  };

  /**
   * Register a new user account
   * 
   * Handles the registration process:
   * 1. Resets errors and sets loading state
   * 2. Calls the registration API
   * 3. Does NOT automatically log in the user (separate step)
   * 4. Returns API response for further handling
   * 
   * @async
   * @function register
   * @param {Object} userData - User registration data
   * @param {string} userData.email - User's email address
   * @param {string} userData.password - User's chosen password
   * @param {string} userData.confirmPassword - Password confirmation
   * @param {string} [userData.firstName] - User's first name
   * @param {string} [userData.lastName] - User's last name
   * @returns {Promise<Object>} Response from registration API
   * @throws {Error} If registration fails with unexpected error
   * 
   * @example
   * const { register } = useAuth();
   * 
   * const handleRegister = async (formData) => {
   *   const response = await register({
   *     email: 'new@example.com',
   *     password: 'SecurePass123',
   *     confirmPassword: 'SecurePass123',
   *     firstName: 'John',
   *     lastName: 'Doe'
   *   });
   *   
   *   if (response.isSuccess) {
   *     toast.success('Registration successful! Please login.');
   *     navigate('/login');
   *   } else {
   *     toast.error(response.message);
   *   }
   * };
   */
  const register = async (userData) => {
    try {
      setLoading(true);
      setError(null);
      const response = await authApi.register(userData);
      
      if (!response.isSuccess) {
        setError(response.message);
      }
      
      return response;
    } catch (err) {
      setError(err.message);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Return authentication state and methods
   * 
   * @returns {Object} Authentication context value
   * @property {Object|null} user - Current authenticated user data or null
   * @property {boolean} loading - Whether any authentication operation is in progress
   * @property {string|null} error - Error message from last failed operation
   * @property {Function} login - Authenticate user with credentials
   * @property {Function} logout - Log out current user
   * @property {Function} register - Register new user account
   * @property {Function} isAuthenticated - Check if user has valid access token
   * @property {Function} getCachedUser - Retrieve cached user data
   */
  return {
    user,
    loading,
    error,
    login,
    logout,
    register,
    isAuthenticated: authApi.isAuthenticated,
    getCachedUser: authApi.getCachedUser
  };
};