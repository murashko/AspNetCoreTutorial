using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

using Tweetbook.Data;
using Tweetbook.Domain;
using Tweetbook.Options;

namespace Tweetbook.Services
{
	public class IdentityService : IIdentityService
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly JwtSettings _jwtSettings;
		private readonly TokenValidationParameters _tokenValidationParameters;
		private readonly DataContext _dataContext;

		public IdentityService(UserManager<IdentityUser> userManager, JwtSettings jwtSettings,
			TokenValidationParameters tokenValidationParameters, DataContext dataContext)
		{
			_userManager = userManager;
			_jwtSettings = jwtSettings;
			_tokenValidationParameters = tokenValidationParameters;
			_dataContext = dataContext;
		}

		public async Task<AuthenticationResult> LoginAsync(string email, string password)
		{
			var user = await _userManager.FindByEmailAsync(email);

			if (user == null || await _userManager.CheckPasswordAsync(user, password))
			{
				return new AuthenticationResult
				{
					Errors = new[] { "Incorrect user email or password" }
				};
			}

			return await GenerateAuthenticationResultForUserAsync(user);
		}

		public async Task<AuthenticationResult> RegisterAsync(string email, string password)
		{
			var user = await _userManager.FindByEmailAsync(email);

			if (user != null)
			{
				return new AuthenticationResult
				{
					Errors = new[] { "User with such email address already exists" }
				};
			}

			var newUser = new IdentityUser
			{
				Email = email,
				UserName = email
			};

			var createdUser = await _userManager.CreateAsync(newUser, password);

			if (!createdUser.Succeeded)
			{
				return new AuthenticationResult
				{
					Errors = createdUser.Errors.Select(error => error.Description)
				};
			}

			return await GenerateAuthenticationResultForUserAsync(newUser);
		}

		public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken)
		{
			var validatedToken = GetPrincipalFromToken(token);
			
			if (validatedToken == null)
			{
				return new AuthenticationResult { Errors = new[] { "Invalid Token" } };
			}

			var expiryDateUnix = long.Parse(validatedToken.Claims.Single(claim =>
				claim.Type == JwtRegisteredClaimNames.Exp).Value);

			var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
				.AddSeconds(expiryDateUnix);

			if (expiryDateTimeUtc > DateTime.UtcNow)
			{
				return new AuthenticationResult { Errors = new[] { "This token hasn't expired yet" } };
			}

			var storedRefreshToken = _dataContext.RefreshTokens.SingleOrDefault(item => item.Token == refreshToken);

			if (storedRefreshToken == null)
			{
				return new AuthenticationResult { Errors = new[] { "This refresh token does not exists" } };
			}

			if (DateTime.UtcNow > storedRefreshToken.ExpireDate)
			{
				return new AuthenticationResult { Errors = new[] { "This refresh token has expired" } };
			}

			if (storedRefreshToken.Invalidated)
			{
				return new AuthenticationResult { Errors = new[] { "This refresh token has been invalidated" } };
			}

			if (storedRefreshToken.Used)
			{
				return new AuthenticationResult { Errors = new[] { "This refresh token has been used" } };
			}

			var jti = validatedToken.Claims
				.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti)
				.Value;
			if (storedRefreshToken.JwtId != jti)
			{
				return new AuthenticationResult { Errors = new[] { "This refresh token does not mutch this JWT" } };
			}

			storedRefreshToken.Used = true;
			_dataContext.RefreshTokens.Update(storedRefreshToken);
			await _dataContext.SaveChangesAsync();

			var user = await _userManager.FindByIdAsync(validatedToken.Claims.Single(claim => claim.Type == "id").Value);

			return await GenerateAuthenticationResultForUserAsync(user);
		}

		private ClaimsPrincipal GetPrincipalFromToken(string token)
		{
			var tokentHandler = new JwtSecurityTokenHandler();

			try
			{
				var principal = tokentHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
				if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
				{
					return null;
				}

				return principal;
			}
			catch
			{
				return null;
			}
		}

		private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
		{
			return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
				jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
					StringComparison.InvariantCultureIgnoreCase);
		}

		private async Task<AuthenticationResult> GenerateAuthenticationResultForUserAsync(IdentityUser user)
		{
			var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[] {
					new Claim(JwtRegisteredClaimNames.Sub, user.Email),
					new Claim(JwtRegisteredClaimNames.Email, user.Email),
					new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
					new Claim("id", user.Id)
				}),
				Expires = DateTime.UtcNow.Add(_jwtSettings.TokenLifetime),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			var token = tokenHandler.CreateToken(tokenDescriptor);

			var refreshToken = new RefreshToken
			{
				JwtId = token.Id,
				UserId = user.Id,
				CreationDate = DateTime.UtcNow,
				ExpireDate = DateTime.UtcNow.AddMonths(6)
			};

			_dataContext.RefreshTokens.Add(refreshToken);
			await _dataContext.SaveChangesAsync();
			
			return new AuthenticationResult
			{
				Success = true,
				Token = tokenHandler.WriteToken(token),
				RefreshToken = refreshToken.Token
			}; 
		}
	}
}