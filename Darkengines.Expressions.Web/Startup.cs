using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Darkengines.Expressions.Factories;
using Darkengines.Expressions.ModelConverters;
using System.Linq.Expressions;
using System.Collections.Generic;
using Esprima;
using System.IO;
using System;
using Newtonsoft.Json;
using Darkengines.Expressions.Web.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Darkengines.Expressions.Web {
	public class Startup {
		protected IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services) {
			services
			.AddLogging()
			.AddEntityFrameworkSqlServer()
			.AddExpressionFactories()
			.AddEntityFrameworkLinqExtensions()
			.AddCors()
			.AddLinqMethodCallExpressionFactories()
			.AddModelConverters()
			.AddDbContext<BloggingContext>((serviceProvider, options) =>
				options.UseSqlServer(Configuration.GetConnectionString("default"))
			);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}
			app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().AllowCredentials());
			app.UseExpressions();
		}
	}
}
