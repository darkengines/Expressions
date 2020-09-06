using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Darkengines.Expressions.Sample.Migrations {
	public partial class Bloggingdatabase : Migration {
		protected override void Up(MigrationBuilder migrationBuilder) {
			migrationBuilder.CreateTable(
				name: "Users",
				columns: table => new {
					Id = table.Column<int>(nullable: false)
						.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
					DisplayName = table.Column<string>(nullable: true),
					HashedPassword = table.Column<string>(nullable: true)
				},
				constraints: table => {
					table.PrimaryKey("PK_Users", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Blogs",
				columns: table => new {
					Id = table.Column<int>(nullable: false)
						.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
					OwnerId = table.Column<int>(nullable: false)
				},
				constraints: table => {
					table.PrimaryKey("PK_Blogs", x => x.Id);
					table.ForeignKey(
						name: "FK_Blogs_Users_OwnerId",
						column: x => x.OwnerId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateTable(
				name: "Posts",
				columns: table => new {
					Id = table.Column<int>(nullable: false)
						.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
					OwnerId = table.Column<int>(nullable: false),
					BlogId = table.Column<int>(nullable: false),
					Content = table.Column<string>(nullable: true)
				},
				constraints: table => {
					table.PrimaryKey("PK_Posts", x => x.Id);
					table.ForeignKey(
						name: "FK_Posts_Blogs_BlogId",
						column: x => x.BlogId,
						principalTable: "Blogs",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_Posts_Users_OwnerId",
						column: x => x.OwnerId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateTable(
				name: "Comments",
				columns: table => new {
					Id = table.Column<int>(nullable: false)
						.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
					OwnerId = table.Column<int>(nullable: false),
					PostId = table.Column<int>(nullable: false),
					Content = table.Column<string>(nullable: true)
				},
				constraints: table => {
					table.PrimaryKey("PK_Comments", x => x.Id);
					table.ForeignKey(
						name: "FK_Comments_Users_OwnerId",
						column: x => x.OwnerId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_Comments_Posts_PostId",
						column: x => x.PostId,
						principalTable: "Posts",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Blogs_OwnerId",
				table: "Blogs",
				column: "OwnerId");

			migrationBuilder.CreateIndex(
				name: "IX_Comments_OwnerId",
				table: "Comments",
				column: "OwnerId");

			migrationBuilder.CreateIndex(
				name: "IX_Comments_PostId",
				table: "Comments",
				column: "PostId");

			migrationBuilder.CreateIndex(
				name: "IX_Posts_BlogId",
				table: "Posts",
				column: "BlogId");

			migrationBuilder.CreateIndex(
				name: "IX_Posts_OwnerId",
				table: "Posts",
				column: "OwnerId");
		}

		protected override void Down(MigrationBuilder migrationBuilder) {
			migrationBuilder.DropTable(
				name: "Comments");

			migrationBuilder.DropTable(
				name: "Posts");

			migrationBuilder.DropTable(
				name: "Blogs");

			migrationBuilder.DropTable(
				name: "Users");
		}
	}
}
