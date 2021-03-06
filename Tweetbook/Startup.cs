using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tweetbook.Data;
using Tweetbook.Options;
using Microsoft.OpenApi.Models;
using Tweetbook.Installers;

namespace Tweetbook
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) 
		{
			services.InstallServicesInAssembly(Configuration);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			} else {
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			var swaggerOptions = new SwaggerOptions();
			Configuration.GetSection(nameof(SwaggerOptions)).Bind(swaggerOptions);
			app.UseSwagger(options =>
				options.RouteTemplate = swaggerOptions.JsonRoute);
			app.UseSwaggerUI(options => 
				options.SwaggerEndpoint(swaggerOptions.UIEndpoint, swaggerOptions.Description));

			app.UseEndpoints(endpoints => {
				endpoints.MapControllers();
			});
		}
	}
}
