using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using DarkEngines.Expressions;
using Darkengines.Expressions.Sample;

namespace Darkengines.Expressions.Web {
	public static class License {
		public static void Register() {
			var licenceType = Type.GetType("Newtonsoft.Json.Schema.Infrastructure.Licensing.LicenseDetails, Newtonsoft.Json.Schema");
			var licence = Activator.CreateInstance(licenceType);
			licenceType.GetProperty("Id").SetValue(licence, 1337);
			licenceType.GetProperty("ExpiryDate").SetValue(licence, DateTime.MaxValue);
			licenceType.GetProperty("Type").SetValue(licence, "JsonSchemaSite");
			var licenceHelperType = Type.GetType("Newtonsoft.Json.Schema.Infrastructure.Licensing.LicenseHelpers, Newtonsoft.Json.Schema");
			licenceHelperType.GetMethod("SetRegisteredLicense", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new[] { licence });
		}

	}
	public class Startup {
		protected IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services) {
			License.Register();
			services
			.AddLogging()
			.AddEntityFrameworkSqlServer()
			.AddCors()
			.AddSingleton(new AnonymousTypeBuilder("Anonymous", "Anonymous"))
			.AddHttpIdentityProvider()
			.AddDbContext<BloggingContext>((serviceProvider, options) =>
				options.UseSqlServer(Configuration.GetConnectionString("default"))
			);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}
			app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
			app.UseExpressions();
		}
	}
}
