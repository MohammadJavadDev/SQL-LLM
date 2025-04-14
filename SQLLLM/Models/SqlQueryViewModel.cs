using System.Data;

namespace SQLLLM.Models
{
    public class SqlQueryViewModel
    {
        // User's natural language query
        public string? UserQuery { get; set; }
        
        // Generated SQL query
        public string? SqlQuery { get; set; }
        
        // Results of the SQL query
        public DataTable? QueryResults { get; set; }
        
        // Any error messages
        public string? ErrorMessage { get; set; }
        
        // Whether the query has been executed
        public bool IsExecuted { get; set; }
    }
}