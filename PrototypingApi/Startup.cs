using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nap.PrototypingApi.Data;

namespace Nap.PrototypingApi
{
	public class Startup
	{
		public IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration) => Configuration = configuration;

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContext<PrototypingDbContext>(options => options.UseInMemoryDatabase("prototyping"));
			
			services.AddControllers().AddJsonOptions(options =>
			{
				options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
				options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
				options.JsonSerializerOptions.IgnoreNullValues = false;
				options.JsonSerializerOptions.IgnoreReadOnlyProperties = false;
				options.JsonSerializerOptions.WriteIndented = true;
			});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();
			app.UseAuthorization();
			app.UseEndpoints(endpoints => endpoints.MapControllers());

		}
	}
}
