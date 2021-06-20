using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using FluentAssertions;

using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Domain;

using Xunit;

namespace Tweetbook.IntegrationTests
{
	public class PostsControllerTests : IntegrationTests
	{
		[Fact]
		public async Task GetAll_WithoutAnyPosts_ReturnsEmptyResponse()
		{
			// Arrange
			await AuthenticateAsync();

			// Act
			var response = await TestClient.GetAsync("api/posts");

			// Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			(await response.Content.ReadAsAsync<List<Post>>()).Should().BeEmpty();
		}

		[Fact]
		public async Task Get_ReturnsPost_WhenPostExistsInTheDatabase()
		{
			// Arrange
			await AuthenticateAsync();
			var postName = "Test post";
			var createdPost = await CreatePostAsync(new CreatePostRequest
			{
				Name = postName
			});

			// Act
			var response = await TestClient.GetAsync($"api/posts/{createdPost.Id}");

			// Assert
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var returnedPost = await response.Content.ReadAsAsync<PostResponse>();
			returnedPost.Id.Should().Be(createdPost.Id);
			returnedPost.Name.Should().Be(postName);
		}
	}
}
