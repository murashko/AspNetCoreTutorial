using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Tweetbook.Services;

namespace Tweetbook.Installers
{
	public class InfrastructureInstaller : IInstaller
	{
		public void InstallServices(IServiceCollection services, IConfiguration configuration)
		{
			services.AddScoped<IPostService, PostService>();
		}
	}
}