using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Domain;
using Tweetbook.Services;

namespace Tweetbook.Controllers.V1
{
	[Route("[controller]")]
	public class IdentityController : Controller
	{
		private readonly IIdentityService _identityService;

		public IdentityController(IIdentityService identityService)
		{
			_identityService = identityService;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] UserRegistrationRequest request)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(new AuthFailedResponse
				{
					Errors = ModelState.Values.SelectMany(value => value.Errors.Select(error => error.ErrorMessage))
				});
			}

			var authResult = await _identityService.RegisterAsync(request.Email, request.Password);

			return GenerateAuthResponse(authResult);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] UserRegistrationRequest request)
		{
			var authResult = await _identityService.LoginAsync(request.Email, request.Password);

			return GenerateAuthResponse(authResult);
		}

		[HttpPost("refresh")]
		public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
		{
			var authResult = await _identityService.RefreshTokenAsync(request.Token, request.RefreshToken);

			return GenerateAuthResponse(authResult);
		}

		private IActionResult GenerateAuthResponse(AuthenticationResult authResult)
		{
			if (!authResult.Success)
			{
				return BadRequest(new AuthFailedResponse
				{
					Errors = authResult.Errors
				});
			}

			return Ok(new AuthSuccessResponse
			{
				Token = authResult.Token,
				RefreshToken = authResult.RefreshToken
			});
		}
	}
}