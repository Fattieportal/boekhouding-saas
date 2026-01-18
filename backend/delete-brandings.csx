using Npgsql;

var connectionString = "Host=localhost;Database=boekhouding;Username=postgres;Password=postgres";

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

Console.WriteLine("Deleting all TenantBranding records...");

await using (var cmd = new NpgsqlCommand("DELETE FROM \"TenantBrandings\"", conn))
{
    var rows = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"✓ Deleted {rows} branding record(s)");
}

await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"TenantBrandings\"", conn))
{
    var count = await cmd.ExecuteScalarAsync();
    Console.WriteLine($"✓ Remaining brandings: {count}");
}

Console.WriteLine();
Console.WriteLine("Database cleaned! Now run the test script again - it will create fresh branding with TenantId.");
