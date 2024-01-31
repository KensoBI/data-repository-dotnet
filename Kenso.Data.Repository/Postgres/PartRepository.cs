using Kenso.Domain;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Kenso.Data.Repository.Postgres
{
    public class PartRepository : IPartRepository
    {
        private readonly string _connectionString;

        public PartRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            if (databaseOptions == null) throw new ArgumentNullException(nameof(databaseOptions));

            if (string.IsNullOrEmpty(databaseOptions.Value.ConnectionString))
            {
                throw new ArgumentException("Connection string not provided.");
            }

            _connectionString = databaseOptions.Value.ConnectionString;
        }
        public async Task<long> Upsert(Part part, long? modelId, string source)
        {
            const string sql = "WITH new_part AS (INSERT INTO part (number, name, description, create_timestamp, update_timestamp, updated_by) " +
                               "VALUES (@partNumber, @partName, @description, NOW(), NOW(), @source) " +
                               "ON CONFLICT (number) DO UPDATE " +
                               "SET name = @partName, description = @description, update_timestamp = 'NOW()', updated_by = @source  RETURNING id)" +
                               "SELECT id FROM new_part";

            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var cmd = dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@partNumber", part.Number);
            cmd.Parameters.AddWithValue("@partName",  string.IsNullOrEmpty(part.Name) ? DBNull.Value : part.Name);
            cmd.Parameters.AddWithValue("@description", string.IsNullOrEmpty(part.Description) ? DBNull.Value : part.Description);
            cmd.Parameters.AddWithValue("@modelId", modelId == null ? DBNull.Value : modelId);
            cmd.Parameters.AddWithValue("@source", source);

            await using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();
            var partId = reader.GetInt64(0);
            return partId;
        }

        public async Task AddModelPartMapping(long partId, long modelId)
        {
            const string sql = "INSERT INTO model_part (model_id, part_id) " +
                               "VALUES (@modelId, @partId) " +
                               "ON CONFLICT (model_id, part_id) DO NOTHING;";

            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var cmd = dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@modelId", modelId);
            cmd.Parameters.AddWithValue("@partId", partId);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
