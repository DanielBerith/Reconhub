using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReconHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "recon");

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "recon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourceSystems",
                schema: "recon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ApiKeyHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceSystems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceSystems_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "recon",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FlowDefinitions",
                schema: "recon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TriggerSourceSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerEventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FollowUpSourceSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowUpEventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompletionWindowSeconds = table.Column<long>(type: "bigint", nullable: false),
                    MatchFields = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowDefinitions_SourceSystems_FollowUpSourceSystemId",
                        column: x => x.FollowUpSourceSystemId,
                        principalSchema: "recon",
                        principalTable: "SourceSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlowDefinitions_SourceSystems_TriggerSourceSystemId",
                        column: x => x.TriggerSourceSystemId,
                        principalSchema: "recon",
                        principalTable: "SourceSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlowDefinitions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "recon",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationEvents",
                schema: "recon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BusinessKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationEvents", x => x.Id);
                    table.CheckConstraint("CK_IntegrationEvents_Payload_IsJson", "ISJSON([Payload]) = 1");
                    table.ForeignKey(
                        name: "FK_IntegrationEvents_SourceSystems_SourceSystemId",
                        column: x => x.SourceSystemId,
                        principalSchema: "recon",
                        principalTable: "SourceSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "recon",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "recon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TriggerOccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FollowUpOccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EvaluationDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastEvaluatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_FlowDefinitions_FlowDefinitionId",
                        column: x => x.FlowDefinitionId,
                        principalSchema: "recon",
                        principalTable: "FlowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "recon",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReconExceptions",
                schema: "recon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconExceptions_IntegrationEvents_IntegrationEventId",
                        column: x => x.IntegrationEventId,
                        principalSchema: "recon",
                        principalTable: "IntegrationEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconExceptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "recon",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconExceptions_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalSchema: "recon",
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransactionEvents",
                schema: "recon",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionEvents", x => new { x.TransactionId, x.IntegrationEventId });
                    table.ForeignKey(
                        name: "FK_TransactionEvents_IntegrationEvents_IntegrationEventId",
                        column: x => x.IntegrationEventId,
                        principalSchema: "recon",
                        principalTable: "IntegrationEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionEvents_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalSchema: "recon",
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                schema: "recon",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReconExceptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntries_ReconExceptions_ReconExceptionId",
                        column: x => x.ReconExceptionId,
                        principalSchema: "recon",
                        principalTable: "ReconExceptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "recon",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ReconExceptionId",
                schema: "recon",
                table: "AuditEntries",
                column: "ReconExceptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_TenantId_ReconExceptionId_OccurredAt",
                schema: "recon",
                table: "AuditEntries",
                columns: new[] { "TenantId", "ReconExceptionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FlowDefinitions_FollowUpSourceSystemId",
                schema: "recon",
                table: "FlowDefinitions",
                column: "FollowUpSourceSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowDefinitions_TenantId_FollowUpSourceSystemId_FollowUpEventType",
                schema: "recon",
                table: "FlowDefinitions",
                columns: new[] { "TenantId", "FollowUpSourceSystemId", "FollowUpEventType" });

            migrationBuilder.CreateIndex(
                name: "IX_FlowDefinitions_TenantId_Name",
                schema: "recon",
                table: "FlowDefinitions",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowDefinitions_TenantId_TriggerSourceSystemId_TriggerEventType",
                schema: "recon",
                table: "FlowDefinitions",
                columns: new[] { "TenantId", "TriggerSourceSystemId", "TriggerEventType" });

            migrationBuilder.CreateIndex(
                name: "IX_FlowDefinitions_TriggerSourceSystemId",
                schema: "recon",
                table: "FlowDefinitions",
                column: "TriggerSourceSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEvents_SourceSystemId_IdempotencyKey",
                schema: "recon",
                table: "IntegrationEvents",
                columns: new[] { "SourceSystemId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEvents_TenantId_BusinessKey",
                schema: "recon",
                table: "IntegrationEvents",
                columns: new[] { "TenantId", "BusinessKey" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconExceptions_IntegrationEventId",
                schema: "recon",
                table: "ReconExceptions",
                column: "IntegrationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconExceptions_TenantId_Status_DetectedAt",
                schema: "recon",
                table: "ReconExceptions",
                columns: new[] { "TenantId", "Status", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconExceptions_TenantId_Type_DetectedAt",
                schema: "recon",
                table: "ReconExceptions",
                columns: new[] { "TenantId", "Type", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconExceptions_TransactionId_Type_IntegrationEventId",
                schema: "recon",
                table: "ReconExceptions",
                columns: new[] { "TransactionId", "Type", "IntegrationEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceSystems_ApiKeyHash",
                schema: "recon",
                table: "SourceSystems",
                column: "ApiKeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceSystems_TenantId_Name",
                schema: "recon",
                table: "SourceSystems",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionEvents_IntegrationEventId",
                schema: "recon",
                table: "TransactionEvents",
                column: "IntegrationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FlowDefinitionId",
                schema: "recon",
                table: "Transactions",
                column: "FlowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TenantId_EvaluationDueAt",
                schema: "recon",
                table: "Transactions",
                columns: new[] { "TenantId", "EvaluationDueAt" },
                filter: "[EvaluationDueAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TenantId_FlowDefinitionId_BusinessKey",
                schema: "recon",
                table: "Transactions",
                columns: new[] { "TenantId", "FlowDefinitionId", "BusinessKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TenantId_State",
                schema: "recon",
                table: "Transactions",
                columns: new[] { "TenantId", "State" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries",
                schema: "recon");

            migrationBuilder.DropTable(
                name: "TransactionEvents",
                schema: "recon");

            migrationBuilder.DropTable(
                name: "ReconExceptions",
                schema: "recon");

            migrationBuilder.DropTable(
                name: "IntegrationEvents",
                schema: "recon");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "recon");

            migrationBuilder.DropTable(
                name: "FlowDefinitions",
                schema: "recon");

            migrationBuilder.DropTable(
                name: "SourceSystems",
                schema: "recon");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "recon");
        }
    }
}
