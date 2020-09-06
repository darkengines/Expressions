using Darkengines.Expression.Sample;
using Darkengines.Expressions.Sample.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Darkengines.Expressions.Sample {
	public static class HttpIdentityProviderExtensions {
		public static IServiceCollection AddHttpIdentityProvider(this IServiceCollection serviceCollection) {
			return serviceCollection.AddScoped<IIdentityProvider, HttpIdentityProvider>();
		}
	}
	public class HttpIdentityProvider : IIdentityProvider {
		private RequestIdentity _identity = null;
		protected IHttpContextAccessor HttpContextAccessor { get; }
		protected JsonSerializer JsonSerializer { get; }

		public HttpIdentityProvider(IHttpContextAccessor httpContextAccessor, JsonSerializer jsonSerializer) {
			HttpContextAccessor = httpContextAccessor;
			JsonSerializer = jsonSerializer;
		}
		public RequestIdentity GetIdentity() {
			if (_identity != null) return _identity;
			var userIdString = HttpContextAccessor.HttpContext.User.Claims.SingleOrDefault(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
			var serializedUser = HttpContextAccessor.HttpContext.User.Claims.SingleOrDefault(claim => claim.Type == "user")?.Value;
			if (string.IsNullOrWhiteSpace(userIdString)) return null;
			var userId = Convert.ToInt32(userIdString);
			using (var textReader = new StringReader(serializedUser)) {
				using (var jsonReader = new JsonTextReader(textReader)) {
					return _identity = new RequestIdentity(JsonSerializer.Deserialize<User>(jsonReader));
				}
			}
		}
	}
}
