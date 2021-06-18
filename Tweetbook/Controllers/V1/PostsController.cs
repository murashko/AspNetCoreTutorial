using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Domain;
using Tweetbook.Extentions;
using Tweetbook.Services;

namespace Tweetbook.Controllers.V1
{
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[Route("api/[controller]")]
	public class PostsController : Controller
	{
		private readonly IPostService _postService;

		public PostsController(IPostService postService)
		{
			_postService = postService;
		}

		[HttpGet]
		public async Task<IActionResult> GetAllAsync()
		{
			return Ok(await _postService.GetPostsAsync());
		}

		[HttpGet("{postId}")]
		public async Task<IActionResult> GetAsync([FromRoute] Guid postId)
		{
			var post = await _postService.GetPostByIdAsync(postId);

			if (post == null)
				return NotFound();

			var response = new PostResponse {
				Id = postId,
				Name = post.Name
			};
			return Ok(response);
		}

		[HttpPost]
		public async Task<IActionResult> CreateAsync([FromBody] CreatePostRequest request)
		{
			var post = new Post {
				Name = request.Name,
				UserId = HttpContext.GetUserId()
			};

			var created = await _postService.CreatePostAsync(post);

			if (!created)
				return BadRequest();

			var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.ToUriComponent()}";
			var locationUrl = $"{baseUrl}/api/v1/posts/{post.Id}";

			var response = new PostResponse {
				Id = post.Id,
				Name = post.Name
			};

			return Created(locationUrl, response);
		}

		[HttpPut("{postId}")]
		public async Task<IActionResult> UpdateAsync([FromRoute] Guid postId, [FromBody] UpdatePostRequest request)
		{
			var isUserOwnsPost = await _postService.IsUserOwnsPostAsync(postId, HttpContext.GetUserId());
			if (!isUserOwnsPost)
			{
				return BadRequest(new { error = "You do not own this post" });
			}

			var post = await _postService.GetPostByIdAsync(postId);
			post.Name = request.Name;

			var updated = await _postService.UpdatePostAsync(post);

			if (!updated)
				return NotFound();

			var response = new PostResponse {
				Id = post.Id,
				Name = post.Name
			};

			return Ok(response);
		}

		[HttpDelete("{postId}")]
		public async Task<IActionResult> DeleteAsync([FromRoute] Guid postId)
		{
			var isUserOwnsPost = await _postService.IsUserOwnsPostAsync(postId, HttpContext.GetUserId());
			if (!isUserOwnsPost)
			{
				return BadRequest(new { error = "You do not own this post" });
			}

			var deleted = await _postService.DeletePostAsync(postId);

			if (!deleted)
				return NotFound();

			return NoContent();
		}
	}
}