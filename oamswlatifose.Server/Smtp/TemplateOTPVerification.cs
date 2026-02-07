namespace oamswlatifose.Server.Smtp
{
    public class TemplateOTPVerification
    {

        public TemplateOTPVerification()
        {
        }
        public string GenerateOTPEmailTemplate(string otpCode, string userName, int expirationMinutes)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .otp-code {{ 
                            font-size: 24px; 
                            font-weight: bold; 
                            color: #2c3e50; 
                            background-color: #f8f9fa; 
                            padding: 15px; 
                            border-radius: 5px; 
                            text-align: center;
                            margin: 20px 0;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Verification Code</h2>
                        <p>Hello {userName},</p>
                        <p>Your One-Time Password (OTP) for verification is:</p>
                        <div class='otp-code'>{otpCode}</div>
                        <p>This code will expire in {expirationMinutes} minutes.</p>
                        <p>If you didn't request this code, please ignore this email.</p>
                    </div>
                </body>
                </html>";
        }
    }
}