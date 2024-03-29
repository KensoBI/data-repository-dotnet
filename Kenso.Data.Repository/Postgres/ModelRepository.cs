﻿using Microsoft.Extensions.Options;
using Npgsql;

namespace Kenso.Data.Repository.Postgres
{
    public class ModelRepository : IModelRepository
    {
        private readonly string _connectionString;

        public ModelRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            if (databaseOptions == null) throw new ArgumentNullException(nameof(databaseOptions));

            if (string.IsNullOrEmpty(databaseOptions.Value.ConnectionString))
            {
                throw new ArgumentException("Connection string not provided.");
            }

            _connectionString = databaseOptions.Value.ConnectionString;
        }

        public async Task<long> Insert(Domain.Model model, string source)
        {
            const string sql = "WITH new_model AS (INSERT INTO model (name, description, create_timestamp, update_timestamp, updated_by) " +
                               "VALUES (@modelName, @description, NOW(), NOW(), @source) " +
                               "ON CONFLICT (name) DO NOTHING " +
                               "RETURNING id) " +
                               "SELECT COALESCE(" +
                               "(SELECT id FROM new_model)," +
                               "(SELECT id FROM model WHERE name = @modelName)) AS id";

            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var cmd = dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("@modelName", string.IsNullOrEmpty(model.Name) ? DBNull.Value: model.Name);
            cmd.Parameters.AddWithValue("@description", string.IsNullOrEmpty(model.Description) ? DBNull.Value : model.Description);
            cmd.Parameters.AddWithValue("@source", source);

            await using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();
            var modelId = reader.GetInt64(0);
            return modelId;
        }
    }
}
