
namespace oamswlatifose.Server.Model.smtp
{
    public class EMEmaillogs
    {

        public int id { get; set; }


        public string Emaillogsid { get; set; } 

        public string Email { get; set; }


        public virtual EMOtpUserRequest OtpUserRequest { get; set; }

    }
}
