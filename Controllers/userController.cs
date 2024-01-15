using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Text;
using AngularApp1.Server.Context;
using AngularApp1.Server.Helper;
using AngularApp1.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AngularApp1.Server.Migrations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using AngularApp1.Server.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;

namespace AngularApp1.Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class userController : ControllerBase
	{

        // created a varaible of appdbcontext
        private readonly AppDbcontext _appdbcontext;

        // constructor injection of dbcontext object via constructor
        public userController(AppDbcontext appdbcontext)
        {
            _appdbcontext = appdbcontext;
        }

        // endpoint for authenticate the user that is been registered by asking him login and password
          [HttpPost("authenticate")]
       public async Task<IActionResult> Authenticate([FromBody] User userobj)
        {

            if (userobj == null)

				

				return BadRequest();

			// authentication logic 
            var user = await _appdbcontext.Users.FirstOrDefaultAsync(x => x.Firstname == userobj.Firstname );

            if (user == null)
                return NotFound(new { Message = "user not found" });


            // verify password as it is hashed
            if (Passwordhasher.VerifyPassword(userobj.Password, user.Password))
            {

                return BadRequest(new {message="password is incorrect"});
            }

			// if authenticate create jwt token 
			user.token = CreateJwtToken(user);

            return Ok(new
            {
                Token=user.token,
                Message = "Login success"
            });



        }



        // endpoint for  register(signup) the user name should not be null
        [HttpPost ("register")]

        public async Task<IActionResult> RegisterUser([FromBody] User userobj)
        {

            if (userobj == null)
            

                return BadRequest();

			// check email
			if (await CheckEmailExistAsync(userobj.Email))
				return BadRequest(new { Message = "Email Already Exist" });

			//check username
			if (await CheckUsernameExistAsync(userobj.Firstname))
				return BadRequest(new { Message = "Username Already Exist" });

			// check the password strength

			var passMessage = CheckPasswordStrength(userobj.Password);
			if (!string.IsNullOrEmpty(passMessage))
				return BadRequest(new { Message = passMessage.ToString() });


			// hash the password logi is writeen in seperate class
			userobj.Password = Passwordhasher.HashPassword(userobj.Password);
            userobj.Role = "user";
            userobj.token = " ";
            await _appdbcontext.Users.AddAsync(userobj);
            await _appdbcontext.SaveChangesAsync();

            return Ok(new { 


               Message ="User Register successful"

            });


		}




		// seperate method is created for check email is unique are not
		private Task<bool> CheckEmailExistAsync(string? email)
			=> _appdbcontext.Users.AnyAsync(x => x.Email == email);


        // seperate method is created for check username is unique are not
        private Task<bool> CheckUsernameExistAsync(string? firstname)
            => _appdbcontext.Users.AnyAsync(x => x.Firstname == firstname);



		// seperate method for checking the password strength
		private static string CheckPasswordStrength(string pass)
		{
			StringBuilder sb = new StringBuilder();
			if (pass.Length < 9)
				sb.Append("Minimum password length should be 8" + Environment.NewLine);
			if (!(Regex.IsMatch(pass, "[a-z]") && Regex.IsMatch(pass, "[A-Z]") && Regex.IsMatch(pass, "[0-9]")))
				sb.Append("Password should be AlphaNumeric" + Environment.NewLine);
			if (!Regex.IsMatch(pass, "[<,>,@,!,#,$,%,^,&,*,(,),_,+,\\[,\\],{,},?,:,;,|,',\\,.,/,~,`,-,=]"))
				sb.Append("Password should contain special charcter" + Environment.NewLine);
			return sb.ToString();
		}


		//  100  lines of code for create and refresh token of jwt


		// In this method we created the token
		private string CreateJwtToken(User user)
        {

            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes("very very secret.....");

            var identity = new ClaimsIdentity(new Claim[]
            {

                new Claim(ClaimTypes.Role,user.Role),
                new Claim(ClaimTypes.Name,$"{user.Firstname}{user.Lastname}")
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddSeconds(10),
                SigningCredentials = credentials
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);



        }

		//created refreshtoken so that if token expires then automatic new token gets generated
		// without throwing out user of loginpage

		private string CreateRefreshToken()
		{
			var tokenBytes = RandomNumberGenerator.GetBytes(64);
			var refreshToken = Convert.ToBase64String(tokenBytes);

			var tokenInUser = _appdbcontext.Users
				.Any(a => a.RefreshToken == refreshToken);
			if (tokenInUser)
			{
				return CreateRefreshToken();
			}
			return refreshToken;
		}


		//
		private ClaimsPrincipal GetPrincipleFromExpiredToken(string token)
		{
			var key = Encoding.ASCII.GetBytes("veryverysceret.....");
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateAudience = false,
				ValidateIssuer = false,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateLifetime = false
			};
			var tokenHandler = new JwtSecurityTokenHandler();
			SecurityToken securityToken;
			var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
			var jwtSecurityToken = securityToken as JwtSecurityToken;
			if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
				throw new SecurityTokenException("This is Invalid Token");
			return principal;

		}

		// return user once authenticated
		[Authorize]
		[HttpGet]
		public async Task<ActionResult<User>> GetAllUsers()
		{
			return Ok(await _appdbcontext.Users.ToListAsync());
		}



		// end point for refresh token
		[HttpPost("refresh")]
		public async Task<IActionResult> Refresh([FromBody] TokenapiDto tokenApiDto)
		{
			if (tokenApiDto is null)
				return BadRequest("Invalid Client Request");
			string accessToken = tokenApiDto.AccessToken;
			string refreshToken = tokenApiDto.RefreshToken;
			var principal = GetPrincipleFromExpiredToken(accessToken);
			var username = principal.Identity.Name;
			var user = await _appdbcontext.Users.FirstOrDefaultAsync(u => u.Firstname == username);
			if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
				return BadRequest("Invalid Request");
			var newAccessToken = CreateJwtToken(user);
			var newRefreshToken = CreateRefreshToken();
			user.RefreshToken = newRefreshToken;
			await _appdbcontext.SaveChangesAsync();
			return Ok(new TokenapiDto()
			{
				AccessToken = newAccessToken,
				RefreshToken = newRefreshToken,
			});
		}

	}
}
