using System.Linq;

using Microsoft.AspNetCore.Http;

namespace Tweetbook.Extentions
{
	public static class GeneralExtentions
	{
		public static string GetUserId(this HttpContext httpContext)
		{
			if (httpContext.User == null)
			{
				return string.Empty;
			}

			return httpContext.User.Claims.Single(claim => claim.Type == "id").Value;
		}
	}
}
