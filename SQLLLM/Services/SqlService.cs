using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SQLLLM.Services
{
    /// <summary>
    /// Implementation of ISqlService for SQL Server operations
    /// </summary>
    public class SqlService : ISqlService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlService> _logger;

        public SqlService(IConfiguration configuration, ILogger<SqlService> logger)
        {
            _connectionString = configuration.GetConnectionString("SqlDatabase") 
                ?? throw new ArgumentNullException("SqlDatabase connection string is not configured");
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<(DataTable? ResultTable, string? ErrorMessage)> ExecuteSqlQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return (null, "Query cannot be empty");
            }

            var dataTable = new DataTable();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = 60; // 1 minute timeout
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
                
                return (dataTable, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL query: {Query}", query);
                return (null, $"Error executing query: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public string GetDatabaseSchema()
        {
            var schema = new System.Text.StringBuilder();
            
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Get all tables
                    var tables = connection.GetSchema("Tables");
                    foreach (DataRow tableRow in tables.Rows)
                    {
                        if (tableRow["TABLE_TYPE"].ToString() == "BASE TABLE")
                        {
                            var tableName = tableRow["TABLE_NAME"].ToString();
                            schema.AppendLine($"Table: {tableName}");
                            
                            // Get columns for this table
                            using (var command = new SqlCommand(
                                $"SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE " +
                                $"FROM INFORMATION_SCHEMA.COLUMNS " +
                                $"WHERE TABLE_NAME = '{tableName}' " +
                                $"ORDER BY ORDINAL_POSITION", connection))
                            {
                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var columnName = reader["COLUMN_NAME"].ToString();
                                        var dataType = reader["DATA_TYPE"].ToString();
                                        var maxLength = reader["CHARACTER_MAXIMUM_LENGTH"];
                                        var isNullable = reader["IS_NULLABLE"].ToString() == "YES" ? "NULL" : "NOT NULL";
                                        
                                        var lengthInfo = maxLength != DBNull.Value ? $"({maxLength})" : "";
                                        schema.AppendLine($"  - {columnName} {dataType}{lengthInfo} {isNullable}");
                                    }
                                }
                            }
                            
                            schema.AppendLine();
                        }
                    }
                }
                
                return schema.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database schema");
                return $"Error retrieving database schema: {ex.Message}";
            }
        }
    }
}