using System.Data;

namespace SQLLLM.Services
{
    /// <summary>
    /// Interface for SQL service that executes queries and retrieves database schema
    /// </summary>
    public interface ISqlService
    {
        /// <summary>
        /// Executes a SQL query and returns the results
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <returns>A tuple containing the result DataTable and any error message</returns>
        Task<(DataTable? ResultTable, string? ErrorMessage)> ExecuteSqlQueryAsync(string query);

        /// <summary>
        /// Retrieves the database schema information
        /// </summary>
        /// <returns>A string representation of the database schema</returns>
        string GetDatabaseSchema();
    }
}