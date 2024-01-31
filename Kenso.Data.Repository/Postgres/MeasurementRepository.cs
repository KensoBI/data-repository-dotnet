using Kenso.Domain;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Kenso.Data.Repository.Postgres
{
    public class MeasurementRepository : IMeasurementRepository
    {
        private readonly string _connectionString;

        public MeasurementRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            if (databaseOptions == null) throw new ArgumentNullException(nameof(databaseOptions));

            if (string.IsNullOrEmpty(databaseOptions.Value.ConnectionString))
            {
                throw new ArgumentException("Connection string not provided.");
            }

            _connectionString = databaseOptions.Value.ConnectionString;
        }

        public async Task Insert(Measurement measurement)
        {
            const string sql = "INSERT INTO measurement " +
                               "(characteristic_id, value, deviation, nominal, time, asset_id, serial, tag, created_by) " +
                               "VALUES (@characteristicId, @value, @deviation, @nominal, @time, @asset_id, @serial, @tag, @source) " +
                               "ON CONFLICT (characteristic_id, time) DO NOTHING;";

            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var cmd = dataSource.CreateCommand(sql);

            if (measurement.Characteristic == null || measurement.Characteristic.Id == 0)
            {
                throw new Exception("Characteristic ID is required to save a measurement.");
            }

            cmd.Parameters.AddWithValue("@characteristicId", measurement.Characteristic.Id);
            cmd.Parameters.AddWithValue("@value", measurement.Value);
            cmd.Parameters.AddWithValue("@deviation", measurement.Deviation.HasValue ? measurement.Deviation : DBNull.Value);
            cmd.Parameters.AddWithValue("@nominal", measurement.Nominal.HasValue ? measurement.Nominal : DBNull.Value);
            cmd.Parameters.AddWithValue("@time", measurement.DateTime);
            cmd.Parameters.AddWithValue("@asset_id", measurement.Asset == null ? DBNull.Value : measurement.Asset.Id);
            cmd.Parameters.AddWithValue("@serial", measurement.Serial == null ? DBNull.Value : measurement.Serial);
            cmd.Parameters.AddWithValue("@tag", NpgsqlDbType.Jsonb, measurement.Tag == null ? DBNull.Value : measurement.Tag);
            cmd.Parameters.AddWithValue("@source", measurement.CreatedBy == null ? DBNull.Value : measurement.CreatedBy);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
