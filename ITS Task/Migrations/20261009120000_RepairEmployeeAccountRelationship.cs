using ITS_Task.DataAccess;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace ITS_Task.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261009120000_RepairEmployeeAccountRelationship")]
public sealed partial class RepairEmployeeAccountRelationship : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "IsAnnual", table: "LeaveTypes", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.Sql("UPDATE LeaveTypes SET IsAnnual = 1 WHERE Id = (SELECT MIN(Id) FROM LeaveTypes WHERE Name = N'Annual Leave');");
        migrationBuilder.CreateIndex(name: "IX_LeaveTypes_IsAnnual", table: "LeaveTypes", column: "IsAnnual", unique: true, filter: "[IsAnnual] = 1");
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM Employees WHERE NULLIF(LTRIM(RTRIM(UserId)), '') IS NOT NULL AND LEN(UserId) > 450)
                THROW 51000, 'Employee UserId exceeds 450 characters. Resolve legacy links before upgrading.', 1;
            IF EXISTS (SELECT UserId FROM Employees WHERE NULLIF(LTRIM(RTRIM(UserId)), '') IS NOT NULL GROUP BY UserId HAVING COUNT(*) > 1)
                THROW 51001, 'Duplicate Employee UserId links exist. Resolve legacy links before upgrading.', 1;
            IF EXISTS (SELECT 1 FROM Employees e WHERE NULLIF(LTRIM(RTRIM(e.UserId)), '') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM AspNetUsers u WHERE u.Id = e.UserId))
                THROW 51002, 'An employee references a missing account. Resolve legacy links before upgrading.', 1;
            UPDATE Employees SET UserId = NULL WHERE NULLIF(LTRIM(RTRIM(UserId)), '') IS NULL;
            """);
        migrationBuilder.AlterColumn<string>(name: "UserId", table: "Employees", type: "nvarchar(450)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
        migrationBuilder.CreateIndex(name: "IX_Employees_UserId", table: "Employees", column: "UserId", unique: true, filter: "[UserId] IS NOT NULL");
        migrationBuilder.AddForeignKey(name: "FK_Employees_AspNetUsers_UserId", table: "Employees", column: "UserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.Sql("""
            INSERT INTO LeaveBalances (EmployeeId, LeaveTypeId, AllocatedDays, UsedDays, PendingDays)
            SELECT e.Id, t.Id, CASE WHEN t.AllowedDays < COALESCE(SUM(CASE WHEN r.Status IN (1, 2) THEN r.Days ELSE 0 END), 0) THEN COALESCE(SUM(CASE WHEN r.Status IN (1, 2) THEN r.Days ELSE 0 END), 0) ELSE t.AllowedDays END, COALESCE(SUM(CASE WHEN r.Status = 2 THEN r.Days ELSE 0 END), 0), COALESCE(SUM(CASE WHEN r.Status = 1 THEN r.Days ELSE 0 END), 0)
            FROM Employees e CROSS JOIN LeaveTypes t
            LEFT JOIN LeaveRequests r ON r.EmployeeId = e.Id AND r.LeaveTypeId = t.Id
            WHERE t.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM LeaveBalances b WHERE b.EmployeeId = e.Id AND b.LeaveTypeId = t.Id)
            GROUP BY e.Id, t.Id, t.AllowedDays;
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_LeaveTypes_IsAnnual", table: "LeaveTypes");
        migrationBuilder.DropColumn(name: "IsAnnual", table: "LeaveTypes");
        migrationBuilder.DropForeignKey(name: "FK_Employees_AspNetUsers_UserId", table: "Employees");
        migrationBuilder.DropIndex(name: "IX_Employees_UserId", table: "Employees");
        migrationBuilder.AlterColumn<string>(name: "UserId", table: "Employees", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(450)", oldNullable: true);
    }
}
