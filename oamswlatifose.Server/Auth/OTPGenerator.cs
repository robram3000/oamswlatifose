
namespace oamswlatifose.Server.Auth
{
    public interface IOTPGenerator
    {
       
        (string OTP, DateTime ExpiryTime) GenerateOTPWithExpiry(int otpLength, int expiryMinutes);
    }

    public class OTPGenerator : IOTPGenerator
    {
        public (string OTP, DateTime ExpiryTime) GenerateOTPWithExpiry(int otpLength, int expiryMinutes)
        {
            var otp = GenerateOTP(otpLength);
            var expiryTime = DateTime.UtcNow.AddMinutes(expiryMinutes);

            return (otp, expiryTime);
        }

        private string GenerateOTP(int length)
        {
            var random = new Random();
            var otp = new char[length];
            for (int i = 0; i < length; i++)
            {
                otp[i] = (char)('0' + random.Next(10));
            }
            return new string(otp);
        }
    }
}