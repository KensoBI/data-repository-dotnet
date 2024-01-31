using Microsoft.Extensions.Options;
using Npgsql;
using Kenso.Domain;

namespace Kenso.Data.Repository.Postgres
{
    public class AssetRepository : IAssetRepository
    {
        private readonly string _connectionString;

        public AssetRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            if (databaseOptions == null) throw new ArgumentNullException(nameof(databaseOptions));

            if (string.IsNullOrEmpty(databaseOptions.Value.ConnectionString))
            {
                throw new ArgumentException("Connection string not provided.");
            }

            _connectionString = databaseOptions.Value.ConnectionString;
        }

        public async Task<long> Insert(Asset asset, string source)
        {
            const string sql = @"
                WITH new_asset AS (
                    INSERT INTO asset (name, location, is_active, create_timestamp, update_timestamp, updated_by)
                    VALUES (@name, @location, @is_active, NOW(), NOW(), @source)
                    ON CONFLICT (name, location) DO NOTHING;
                    SELECT COALESCE(
                        (SELECT id FROM new_asset),
                        (SELECT id FROM asset WHERE location = @location AND name = @name)
                    ) AS assetId;";

            return await ExecuteInsertOrUpdate(asset, sql, source);
        }

        public async Task<long> Upsert(Asset asset, string source)
        {
            const string sql = @"
                WITH new_asset AS (
                    INSERT INTO asset (name, location, is_active, create_timestamp, update_timestamp, updated_by)
                    VALUES (@name, @location, @is_active, NOW(), NOW(), @source)
                    ON CONFLICT (name, location) DO UPDATE
                    SET
                        name = @name,
                        location = @location,
                        is_active = @is_active,
                        update_timestamp = NOW(),
                        updated_by = @source
                    RETURNING id
                )
                SELECT COALESCE(
                    (SELECT id FROM new_asset),
                    (SELECT id FROM asset WHERE location = @location AND name = @name)
                ) AS assetId;";

            return await ExecuteInsertOrUpdate(asset, sql, source);
        }

        public async Task<long> Update(Asset asset, string source)
        {
            const string sql = @"
                UPDATE asset
                SET
                    name = @name,
                    location = @location,
                    is_active = @is_active,
                    update_timestamp = NOW(),
                    updated_by = @source
                WHERE id = @asset_id
                RETURNING id;";
            return await ExecuteInsertOrUpdate(asset, sql, source);
        }

        private async Task<long> ExecuteInsertOrUpdate(Asset asset, string sql, string source)
        {
            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var cmd = dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@name", asset.Name);
            cmd.Parameters.AddWithValue("@location", asset.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("@is_active", asset.IsActive);
            cmd.Parameters.AddWithValue("@source", source);

            if (asset.Id != 0)
            {
                cmd.Parameters.AddWithValue("@asset_id", asset.Id);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();
            var assetId = reader.GetInt64(0);
            return assetId;
        }
    }
}
