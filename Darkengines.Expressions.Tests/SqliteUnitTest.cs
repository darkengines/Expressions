using Darkengines.Expressions.Factories;
using Darkengines.Expressions.ModelConverters;
using Darkengines.Expressions.Tests.Entities;
using Esprima;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Darkengines.Expressions.Tests {
	[TestClass]
	public class SqliteUnitTest {
		[TestMethod]
		public void TestExpression() {
			var connection = new SqliteConnection("DataSource=:memory:");
			connection.Open();
			try {
				var loggerFactory = new LoggerFactory().AddDebug();
				var options = new DbContextOptionsBuilder<BloggingContext>()
					.UseSqlite(connection)
					.UseLoggerFactory(loggerFactory)
					.Options;

				// Create the schema in the database
				using (var context = new BloggingContext(options)) {
					context.Database.EnsureCreated();
				}

				// Insert seed data into the database using one instance of the context
				using (var context = new BloggingContext(options)) {
					var userIndex = 0;
					while (userIndex < 10) {
						var user = new User() {
							DisplayName = $"User{userIndex}",
							HashedPassword = new Guid().ToString()
						};
						context.Users.Add(user);

						var blogIndex = 0;
						while (blogIndex < 3) {
							var blog = new Blog() {
								Owner = user
							};
							context.Blogs.Add(blog);

							var postIndex = 0;
							while (postIndex < 10) {
								var post = new Post() {
									Blog = blog,
									Owner = user,
									Content = $"Post content #{postIndex}"
								};
								context.Posts.Add(post);
								postIndex++;
							}
							blogIndex++;
						}
						userIndex++;
					}
					context.SaveChanges();
				}

				// Use a clean instance of the context to run the test
				using (var dbContext = new BloggingContext(options)) {
					var serviceCollection = new ServiceCollection();
					serviceCollection.AddExpressionFactories()
					.AddLinqMethodCallExpressionFactories()
					.AddModelConverters();

					var serviceProvider = serviceCollection.BuildServiceProvider();

					// This line of code will be executed on the server side.
					var code = "Users.Join(Blogs, u => u.Id, b => b.OwnerId, (u, b) => b.Posts.Select(p => ({Content: p.Content})).ToList()).ToArray()";

					// Parsing Ecmascript
					var parser = new JavaScriptParser(code);
					var jsExpression = parser.ParseExpression();

					// Converting parsed Ecmascript expression to expression model
					var modelConverters = serviceProvider.GetServices<IModelConverter>();

					// The context olds the converters
					var context = new ModelConverterContext() { ModelConverters = modelConverters };
					var rootConverter = modelConverters.FindModelConverterFor(jsExpression, context);
					var model = rootConverter.Convert(jsExpression, context);

					// Building the expression
					var expressionFactories = serviceProvider.GetServices<IExpressionFactory>();

					// The context olds the factories
					var expressionFactoryContext = new ExpressionFactoryContext() {
						ExpressionFactories = expressionFactories
					};

					// The scope olds the target type, the generic parameter map and varibales set.
					// Note that the target type is null since we cannot predict it at this stage.
					// Note that the generic parameter map is null since we cannot predict it at this stage
					var expressionFactoryScope = new ExpressionFactoryScope(null, null) {
						Variables = new Dictionary<string, Expression>() {
							{ nameof(dbContext.Users), Expression.Constant(dbContext.Users) },
							{ nameof(dbContext.Blogs), Expression.Constant(dbContext.Blogs) },
							{ nameof(dbContext.Posts), Expression.Constant(dbContext.Posts) },
							{ nameof(dbContext.Comments), Expression.Constant(dbContext.Comments) },
						}
					};

					var rootExpressionFactory = expressionFactories.FindExpressionFactoryFor(model, expressionFactoryContext, expressionFactoryScope);
					var expression = rootExpressionFactory.BuildExpression(model, expressionFactoryContext, expressionFactoryScope);
					// Executing the resulting expression
					var function = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile();
					var result = function();
				}
			} finally {
				connection.Close();
			}
		}
	}
}
