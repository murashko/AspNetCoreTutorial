using System;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tweetbook.Installers
{
	public static class InstallerExtensions
	{
		public static void InstallServicesInAssembly(this IServiceCollection services, IConfiguration configuration)
		{
			var installers = typeof(Startup)
				.Assembly
				.ExportedTypes
				.Where(type => typeof(IInstaller).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
				.Select(Activator.CreateInstance)
				.Cast<IInstaller>()
				.ToList();

			installers.ForEach(installer => installer.InstallServices(services, configuration));
		}
	}
}