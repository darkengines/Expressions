using Darkengines.Expressions.Security;
using Darkengines.Expressions.Tests.Entities;
using Darkengines.Expressions.Tests.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Darkengines.Expressions.Tests {
	public class BloggingContext : DbContext {
		public BloggingContext() { }

		public DbSet<User> Users { get; set; }
		public DbSet<Blog> Blogs { get; set; }
		public DbSet<Post> Posts { get; set; }
		public DbSet<Comment> Comments { get; set; }

		public BloggingContext(DbContextOptions<BloggingContext> options)
			: base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<User>().HasKey(user => user.Id);
			modelBuilder.Entity<User>().HasMany(user => user.Blogs).WithOne(blog => blog.Owner).HasForeignKey(blog => blog.OwnerId);
			modelBuilder.Entity<User>().HasMany(user => user.Posts).WithOne(post => post.Owner).HasForeignKey(post => post.OwnerId);
			modelBuilder.Entity<User>().HasMany(user => user.Comments).WithOne(comment => comment.Owner).HasForeignKey(comment => comment.OwnerId);

			modelBuilder.Entity<Blog>().HasKey(blog => blog.Id);
			modelBuilder.Entity<Blog>().HasMany(blog => blog.Posts).WithOne(post => post.Blog).HasForeignKey(post => post.BlogId);

			modelBuilder.Entity<Post>().HasKey(post => post.Id);
			modelBuilder.Entity<Post>().HasMany(post => post.Comments).WithOne(comment => comment.Post).HasForeignKey(comment => comment.PostId);

			modelBuilder.Entity<Comment>().HasKey(comment => comment.Id);
			modelBuilder.AddSecurity();
		}
	}
}
