using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FSpot.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exports",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ImageId = table.Column<Guid>(nullable: false),
                    ImageVersionId = table.Column<long>(nullable: false),
                    ExportType = table.Column<string>(nullable: true),
                    ExportToken = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    JobType = table.Column<string>(nullable: true),
                    JobOptions = table.Column<string>(nullable: true),
                    RunAt = table.Column<DateTime>(nullable: false),
                    JobPriority = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meta",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Data = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rolls",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UtcTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rolls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UtcTime = table.Column<DateTime>(nullable: false),
                    BaseUri = table.Column<string>(nullable: true),
                    Filename = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    RollId = table.Column<Guid>(nullable: false),
                    DefaultVersionId = table.Column<long>(nullable: false),
                    Rating = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_Rolls_RollId",
                        column: x => x.RollId,
                        principalTable: "Rolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotoVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PhotoId = table.Column<Guid>(nullable: false),
                    VersionId = table.Column<long>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    BaseUri = table.Column<string>(nullable: true),
                    Filename = table.Column<string>(nullable: true),
                    ImportMd5 = table.Column<string>(nullable: true),
                    Protected = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoVersions_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    CategoryId = table.Column<Guid>(nullable: false),
                    IsCategory = table.Column<bool>(nullable: false),
                    SortPriority = table.Column<long>(nullable: false),
                    Icon = table.Column<string>(nullable: true),
                    PhotoId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhotoTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PhotoId = table.Column<Guid>(nullable: false),
                    TagId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoTags_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Meta",
                columns: new[] { "Id", "Data", "Name" },
                values: new object[] { new Guid("9fbb58ab-1b6e-4f0a-a05e-dadd88228889"), "0.9.0", "F-Spot Version" });

            migrationBuilder.InsertData(
                table: "Meta",
                columns: new[] { "Id", "Data", "Name" },
                values: new object[] { new Guid("d1094b91-f73e-4a04-832f-d6952d11e314"), "8d8c0f4d-2fe4-4627-ac9a-7c273004b3b5", "Hidden Tag Id" });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "CategoryId", "Icon", "IsCategory", "Name", "PhotoId", "SortPriority" },
                values: new object[] { new Guid("920caad8-6480-4d8c-9e0a-468c94e777c1"), new Guid("00000000-0000-0000-0000-000000000000"), "emblem-favorite", true, "Favorites", null, -10L });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "CategoryId", "Icon", "IsCategory", "Name", "PhotoId", "SortPriority" },
                values: new object[] { new Guid("8d8c0f4d-2fe4-4627-ac9a-7c273004b3b5"), new Guid("00000000-0000-0000-0000-000000000000"), "emblem-readonly", false, "Hidden", null, -9L });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "CategoryId", "Icon", "IsCategory", "Name", "PhotoId", "SortPriority" },
                values: new object[] { new Guid("d67b6807-f635-4e6b-a98a-8a9f18966647"), new Guid("00000000-0000-0000-0000-000000000000"), "emblem-people", true, "People", null, -8L });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "CategoryId", "Icon", "IsCategory", "Name", "PhotoId", "SortPriority" },
                values: new object[] { new Guid("83128847-fd1e-4a5b-9294-3977a031f4f1"), new Guid("00000000-0000-0000-0000-000000000000"), "emblem-places", true, "Places", null, -7L });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "CategoryId", "Icon", "IsCategory", "Name", "PhotoId", "SortPriority" },
                values: new object[] { new Guid("da4b6643-50c4-4721-908d-ed2fe09feade"), new Guid("00000000-0000-0000-0000-000000000000"), "emblem-event", true, "Events", null, -6L });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_RollId",
                table: "Photos",
                column: "RollId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_PhotoId",
                table: "PhotoTags",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_TagId",
                table: "PhotoTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoVersions_PhotoId",
                table: "PhotoVersions",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_PhotoId",
                table: "Tags",
                column: "PhotoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Exports");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "Meta");

            migrationBuilder.DropTable(
                name: "PhotoTags");

            migrationBuilder.DropTable(
                name: "PhotoVersions");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "Rolls");
        }
    }
}
