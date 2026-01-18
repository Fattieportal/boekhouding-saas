-- Migration: Add AuditLog table
-- Description: Adds audit logging capabilities voor tracking belangrijke acties

-- Create AuditLogs table
CREATE TABLE "AuditLogs" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "Action" character varying(50) NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "DiffJson" jsonb,
    "IpAddress" character varying(45),
    "UserAgent" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuditLogs_Tenants_TenantId" FOREIGN KEY ("TenantId") 
        REFERENCES "Tenants" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AuditLogs_Users_ActorUserId" FOREIGN KEY ("ActorUserId") 
        REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

-- Create indices for better query performance
CREATE INDEX "IX_AuditLogs_TenantId" ON "AuditLogs" ("TenantId");
CREATE INDEX "IX_AuditLogs_TenantId_Timestamp" ON "AuditLogs" ("TenantId", "Timestamp");
CREATE INDEX "IX_AuditLogs_TenantId_EntityType_EntityId" ON "AuditLogs" ("TenantId", "EntityType", "EntityId");
CREATE INDEX "IX_AuditLogs_ActorUserId" ON "AuditLogs" ("ActorUserId");

-- Add comments
COMMENT ON TABLE "AuditLogs" IS 'Audit trail voor belangrijke acties in het systeem';
COMMENT ON COLUMN "AuditLogs"."TenantId" IS 'Tenant waarvoor de actie werd uitgevoerd';
COMMENT ON COLUMN "AuditLogs"."ActorUserId" IS 'Gebruiker die de actie uitvoerde';
COMMENT ON COLUMN "AuditLogs"."Action" IS 'Actie die werd uitgevoerd (Create, Update, Delete, Post, Reverse, etc.)';
COMMENT ON COLUMN "AuditLogs"."EntityType" IS 'Type entiteit waarop de actie werd uitgevoerd';
COMMENT ON COLUMN "AuditLogs"."EntityId" IS 'ID van de entiteit waarop de actie werd uitgevoerd';
COMMENT ON COLUMN "AuditLogs"."Timestamp" IS 'Timestamp van de actie (UTC)';
COMMENT ON COLUMN "AuditLogs"."DiffJson" IS 'JSON met veranderingen (before/after voor updates)';
COMMENT ON COLUMN "AuditLogs"."IpAddress" IS 'IP adres van de gebruiker';
COMMENT ON COLUMN "AuditLogs"."UserAgent" IS 'User agent van de request';
