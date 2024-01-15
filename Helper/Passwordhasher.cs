using System.Security.Cryptography;

namespace AngularApp1.Server.Helper
{
	public class Passwordhasher
	{

		private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

		private static readonly int saltsize = 16;
		private static readonly int Hashsize = 20;
		private static readonly int Iterations = 10000;


		public static string HashPassword(string password)
		{
			byte[] salt;
			rng.GetBytes(salt = new byte[saltsize]);

			var key = new Rfc2898DeriveBytes(password, salt, Iterations);
			var hash = key.GetBytes(Hashsize);

			var hashbytes = new byte[saltsize + Hashsize];
			Array.Copy(salt, 0, hashbytes, 0, saltsize);
			Array.Copy(hash, 0, hashbytes, saltsize, Hashsize);

			var base64Hash = Convert.ToBase64String(hashbytes);
			return base64Hash;
		}

		public static Boolean VerifyPassword(string password,string base64Hash)
		{

			var hashBytes = Convert.FromBase64String(base64Hash);

			var salt = new byte[saltsize];
			Array.Copy(hashBytes, 0, salt, 0, saltsize);

			var key = new Rfc2898DeriveBytes(password, salt, Iterations);
			byte[] hash = key.GetBytes(Hashsize);

			for (var i = 0; i < Hashsize; i++)
			
				if (hashBytes[i + saltsize] != hash[i])
					return false;
			
			return true;

		}
	}
}
