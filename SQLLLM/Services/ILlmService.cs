namespace SQLLLM.Services
{
    /// <summary>
    /// Interface for language model service that converts natural language to SQL queries
    /// </summary>
    public interface ILlmService
    {
        /// <summary>
        /// Generates a SQL query from a natural language prompt
        /// </summary>
        /// <param name="prompt">The natural language prompt</param>
        /// <param name="databaseSchema">The database schema information</param>
        /// <returns>The generated SQL query as a string</returns>
        Task<string> GenerateSqlQueryAsync(string prompt, string databaseSchema);
    }
}