-- Delete ALL branding records (including broken ones without TenantId)
DELETE FROM "TenantBrandings";

-- Verify they're all gone
SELECT COUNT(*) as "Remaining Brandings" FROM "TenantBrandings";

VACUUM ANALYZE "TenantBrandings";
