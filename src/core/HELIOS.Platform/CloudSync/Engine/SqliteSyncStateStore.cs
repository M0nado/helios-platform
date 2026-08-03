using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.CloudSync.Providers;

namespace HELIOS.Platform.CloudSync.Engine
{
    /// <summary>
    /// SQLite-based implementation of sync state store
    /// </summary>
    public class SqliteSyncStateStore : ISyncStateStore
    {
        private readonly string _connectionString;
        private const string TableName = "SyncState";

        public SqliteSyncStateStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                        CREATE TABLE IF NOT EXISTS {TableName} (
                            Path TEXT PRIMARY KEY,
                            Name TEXT NOT NULL,
                            Size INTEGER NOT NULL,
                            Modified TEXT NOT NULL,
                            ETag TEXT,
                            ContentHash TEXT,
                            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                            UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                        )";
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task<Dictionary<string, CloudFileMetadata>> GetLocalStateAsync(CancellationToken ct = default)
        {
            var state = new Dictionary<string, CloudFileMetadata>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT * FROM {TableName}";
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                var metadata = new CloudFileMetadata
                                {
                                    Path = reader["Path"].ToString(),
                                    Name = reader["Name"].ToString(),
                                    Size = Convert.ToInt64(reader["Size"]),
                                    Modified = DateTime.Parse(reader["Modified"].ToString()),
                                    ETag = reader["ETag"]?.ToString(),
                                    ContentHash = reader["ContentHash"]?.ToString()
                                };
                                state[metadata.Path] = metadata;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CloudSyncException("Failed to read sync state from database", ex);
            }

            return state;
        }

        public async Task UpdateLocalStateAsync(string path, CloudFileMetadata metadata, CancellationToken ct = default)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $@"
                            INSERT OR REPLACE INTO {TableName} 
                            (Path, Name, Size, Modified, ETag, ContentHash, UpdatedAt) 
                            VALUES (@Path, @Name, @Size, @Modified, @ETag, @ContentHash, CURRENT_TIMESTAMP)";
                        
                        command.Parameters.AddWithValue("@Path", metadata.Path);
                        command.Parameters.AddWithValue("@Name", metadata.Name);
                        command.Parameters.AddWithValue("@Size", metadata.Size);
                        command.Parameters.AddWithValue("@Modified", metadata.Modified.ToString("O"));
                        command.Parameters.AddWithValue("@ETag", metadata.ETag ?? "");
                        command.Parameters.AddWithValue("@ContentHash", metadata.ContentHash ?? "");
                        
                        await command.ExecuteNonQueryAsync(ct);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CloudSyncException($"Failed to update sync state for {path}", ex);
            }
        }

        public async Task RemoveFromLocalStateAsync(string path, CancellationToken ct = default)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"DELETE FROM {TableName} WHERE Path = @Path";
                        command.Parameters.AddWithValue("@Path", path);
                        await command.ExecuteNonQueryAsync(ct);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CloudSyncException($"Failed to remove sync state for {path}", ex);
            }
        }

        public async Task ClearLocalStateAsync(CancellationToken ct = default)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"DELETE FROM {TableName}";
                        await command.ExecuteNonQueryAsync(ct);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CloudSyncException("Failed to clear sync state", ex);
            }
        }
    }
}
