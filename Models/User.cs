using System.ComponentModel.DataAnnotations;

namespace AngularApp1.Server.Models
{
	public class User
	{
		[Key]
        public   int  id { get; set; }

		[Required]
        public  string? Firstname  { get; set; }

		
		public string? Lastname { get; set; }


		public string? Password { get; set; }
		public string? token { get; set; }

		public string? Role { get; set; }

		public string? Email { get; set; }


		public string? RefreshToken { get; set; }
		public DateTime RefreshTokenExpiryTime { get; set; }


	}
}
