using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Data;

namespace Tweetbook.IntegrationTests
{
	public class IntegrationTests : IDisposable
	{
		protected readonly HttpClient TestClient;
		private readonly IServiceProvider serviceProvider;

		protected IntegrationTests()
		{
			var appFactory = new WebApplicationFactory<Startup>()
				.WithWebHostBuilder(builder =>
					builder.ConfigureServices(services =>
					{
						services.RemoveAll<DataContext>();
						services.AddDbContext<DataContext>(options =>
							options.UseInMemoryDatabase("TestDb")
						);
					})
				);
			serviceProvider = appFactory.Services;
			TestClient = appFactory.CreateClient();
		}

		protected async Task<PostResponse> CreatePostAsync(CreatePostRequest request)
		{
			var response = await TestClient.PostAsJsonAsync("api/posts", request);
			return await response.Content.ReadAsAsync<PostResponse>();
		}

		protected async Task AuthenticateAsync()
		{
			TestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", await GetJwtAsync());
		}

		private async Task<string> GetJwtAsync()
		{
			var response = await TestClient.PostAsJsonAsync("identity/register", new UserRegistrationRequest
			{
				Email = "test@integration.com",
				Password = "SomePassword1234!"
			});

			var retistrationResponse = await response.Content.ReadAsAsync<AuthSuccessResponse>();
			return retistrationResponse.Token;
		}

		public void Dispose()
		{
			using var serviceScope = serviceProvider.CreateScope();
			var context = serviceScope.ServiceProvider.GetService<DataContext>();
			context.Database.EnsureDeleted();
		}
	}
}
