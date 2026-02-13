using System.Security.Cryptography;
using System.Text;

namespace oamswlatifose.Server.Utilities.Security
{
    /// <summary>
    /// Provides cryptographic password hashing and verification services using HMACSHA512.
    /// Implements secure password storage with unique per-user salts and constant-time
    /// comparison to prevent timing attacks.
    /// 
    /// <para>Security Features:</para>
    /// <para>- 512-bit HMAC SHA-2 for strong cryptographic hashing</para>
    /// <para>- Unique 128-byte salt per password for rainbow table resistance</para>
    /// <para>- Constant-time comparison algorithm to prevent timing attacks</para>
    /// <para>- No plaintext password storage or transmission</para>
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Hashes a plaintext password using HMACSHA512 with a randomly generated salt.
        /// </summary>
        /// <param name="password">The plaintext password to hash</param>
        /// <returns>Tuple containing the Base64-encoded hash and salt</returns>
        public static (string hash, string salt) HashPassword(string password)
        {
            using (var hmac = new HMACSHA512())
            {
                var salt = hmac.Key;
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                return (
                    Convert.ToBase64String(hash),
                    Convert.ToBase64String(salt)
                );
            }
        }

        /// <summary>
        /// Verifies a plaintext password against a stored hash and salt.
        /// Uses constant-time comparison to prevent timing attacks.
        /// </summary>
        /// <param name="password">The plaintext password to verify</param>
        /// <param name="storedHash">The stored Base64-encoded hash</param>
        /// <param name="storedSalt">The stored Base64-encoded salt</param>
        /// <returns>True if the password matches the hash; otherwise, false</returns>
        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var salt = Convert.FromBase64String(storedSalt);
            var hash = Convert.FromBase64String(storedHash);

            using (var hmac = new HMACSHA512(salt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                return ConstantTimeComparison(computedHash, hash);
            }
        }

        /// <summary>
        /// Performs constant-time comparison of two byte arrays to prevent timing attacks.
        /// </summary>
        private static bool ConstantTimeComparison(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            var result = 0;
            for (var i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];

            return result == 0;
        }

        /// <summary>
        /// Generates a cryptographically secure random token for password reset or email verification.
        /// </summary>
        /// <param name="length">The desired token length in bytes (will be Base64 encoded)</param>
        /// <returns>A URL-safe Base64 encoded random token</returns>
        public static string GenerateSecureToken(int length = 32)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenData = new byte[length];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData)
                    .Replace("/", "_")
                    .Replace("+", "-")
                    .TrimEnd('=');
            }
        }

        /// <summary>
        /// Generates a numeric OTP of specified length using cryptographic randomness.
        /// </summary>
        /// <param name="length">Number of digits (default: 6)</param>
        /// <returns>Numeric OTP string</returns>
        public static string GenerateNumericOtp(int length = 6)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                var otp = new StringBuilder();

                for (var i = 0; i < length; i++)
                {
                    rng.GetBytes(bytes);
                    var randomNumber = BitConverter.ToUInt32(bytes, 0);
                    var digit = randomNumber % 10;
                    otp.Append(digit);
                }

                return otp.ToString();
            }
        }
    }
}
