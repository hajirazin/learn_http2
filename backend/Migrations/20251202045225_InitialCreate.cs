using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Http2Streaming.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Records_Id",
                table: "Records",
                column: "Id");

            // Seed 100,000 records using efficient batch inserts
            SeedRecords(migrationBuilder);
        }

        private void SeedRecords(MigrationBuilder migrationBuilder)
        {
            const int totalRecords = 100_000;
            const int batchSize = 1_000;
            var random = new Random(42); // Fixed seed for reproducibility
            var baseDateTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (int batch = 0; batch < totalRecords / batchSize; batch++)
            {
                var values = new System.Text.StringBuilder();
                
                for (int i = 0; i < batchSize; i++)
                {
                    int id = batch * batchSize + i + 1;
                    string name = $"Item {id}";
                    decimal value = Math.Round((decimal)(random.NextDouble() * 1000), 2);
                    DateTime createdAt = baseDateTime.AddSeconds(id);

                    if (i > 0)
                        values.Append(",");
                    
                    values.Append($"({id}, '{name}', {value}, '{createdAt:yyyy-MM-dd HH:mm:ss}')");
                }

                var sql = $@"
                    INSERT INTO ""Records"" (""Id"", ""Name"", ""Value"", ""CreatedAt"")
                    VALUES {values};";

                migrationBuilder.Sql(sql);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Records");
        }
    }
}
