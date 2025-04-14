using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLLLM.Data;

namespace SQLLLM.Services
{
    public interface ISqlService
    {
        Task<(DataTable? Data, string? Error)> ExecuteSqlQueryAsync(string sqlQuery);
        string GetDatabaseSchema();
    }

    public class SqlService : ISqlService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<SqlService> _logger;

        public SqlService(ApplicationDbContext dbContext, ILogger<SqlService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(DataTable? Data, string? Error)> ExecuteSqlQueryAsync(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                return (null, "SQL query is empty or null");
            }

            // Validate the SQL query for safety
            var validationResult = ValidateSqlQuery(sqlQuery);
            if (!validationResult.IsValid)
            {
                return (null, validationResult.Error);
            }

            try
            {
                var dataTable = new DataTable();
                
                // Get connection from EF Core context
                var connection = _dbContext.Database.GetDbConnection();
                var wasClosed = connection.State == ConnectionState.Closed;
                
                if (wasClosed)
                {
                    await connection.OpenAsync();
                }

                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = sqlQuery;
                    command.CommandType = CommandType.Text;
                    
                    using var reader = await command.ExecuteReaderAsync();
                    dataTable.Load(reader);
                    
                    return (dataTable, null);
                }
                finally
                {
                    if (wasClosed)
                    {
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL query: {Query}", sqlQuery);
                return (null, $"Error executing query: {ex.Message}");
            }
        }

        public string GetDatabaseSchema()
        {
            // Return a string representation of the database schema
            // This is a simple version focused on the Sales table for the demo
            return @"
CREATE TABLE Sales (
    Id INT PRIMARY KEY IDENTITY,
    Region NVARCHAR(100),
    SaleDate DATE,
    SalesAmount DECIMAL(18,2)
);";
        }

        private (bool IsValid, string? Error) ValidateSqlQuery(string sqlQuery)
        {
            // Strip comments to prevent SQL injection through comments
            sqlQuery = StripSqlComments(sqlQuery);
            
            // Basic validation checks
            if (ContainsDisallowedKeywords(sqlQuery))
            {
                return (false, "Query contains disallowed keywords that might modify the database");
            }

            // Only allow SELECT statements for safety
            if (!IsSelectStatement(sqlQuery))
            {
                return (false, "Only SELECT statements are allowed");
            }

            return (true, null);
        }

        private string StripSqlComments(string sql)
        {
            // Remove -- style comments
            sql = Regex.Replace(sql, @"--(.*?)\r?\n", " ");
            
            // Remove /* */ style comments
            sql = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            
            return sql;
        }

        private bool ContainsDisallowedKeywords(string sql)
        {
            // Convert to uppercase for case-insensitive matching
            var upperSql = sql.ToUpperInvariant();
            
            // Disallow any data modification or schema change operations
            var disallowedKeywords = new[] 
            {
                "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE", 
                "EXEC", "EXECUTE", "SP_", "XP_", "SYSTEM_", "GRANT", "REVOKE", "DENY"
            };
            
            return disallowedKeywords.Any(keyword => upperSql.Contains(keyword));
        }

        private bool IsSelectStatement(string sql)
        {
            // Remove any leading whitespace
            sql = sql.TrimStart();
            
            // Check if it starts with SELECT
            return sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
        }
    }
}