using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduler.Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealershipId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Vehicle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ServiceTypeCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TechnicianId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceBayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dealerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OperatingHoursStart = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    OperatingHoursEnd = table.Column<TimeOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealerships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResourceKind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ResourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SlotStart = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentSlots_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Dealerships",
                columns: new[] { "Id", "Name", "OperatingHoursEnd", "OperatingHoursStart" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Downtown Dealership", new TimeOnly(17, 0, 0), new TimeOnly(8, 0, 0) });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlots_AppointmentId",
                table: "AppointmentSlots",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlots_ResourceKind_ResourceId_SlotStart",
                table: "AppointmentSlots",
                columns: new[] { "ResourceKind", "ResourceId", "SlotStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email_Phone",
                table: "Customers",
                columns: new[] { "Email", "Phone" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentSlots");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Dealerships");

            migrationBuilder.DropTable(
                name: "Appointments");
        }
    }
}
