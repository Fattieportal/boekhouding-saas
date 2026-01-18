-- Clean up TenantBranding records without a valid TenantId
DELETE FROM "TenantBrandings" 
WHERE "TenantId" = '00000000-0000-0000-0000-000000000000';

-- Show all remaining branding records
SELECT * FROM "TenantBrandings";
