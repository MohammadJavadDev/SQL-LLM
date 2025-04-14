using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace SQLLLM.Services
{
    public interface ILlmService
    {
        Task<string> GenerateSqlQueryAsync(string prompt, string databaseSchema);
    }

    public class LlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LlmService> _logger;
        private readonly string _llmEndpoint = "http://localhost:11434/api/generate";

        public LlmService(HttpClient httpClient, ILogger<LlmService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GenerateSqlQueryAsync(string prompt, string databaseSchema)
        {
            try
            {
                var request = new
                {
                    model = "deepseek-coder", // Change this based on your local LLM model
                    prompt = CreateLlmPrompt(prompt, databaseSchema),
                    stream = false,
                    temperature = 0.1 // Lower temperature for more deterministic output
                };

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_llmEndpoint, content);

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<LlmResponse>(jsonResponse);

                if (result == null || string.IsNullOrEmpty(result.Response))
                {
                    throw new Exception("The LLM response was empty or invalid");
                }

                // Extract SQL query from the LLM response
                return ExtractSqlQuery(result.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SQL query with LLM");
                throw;
            }
        }

        private string CreateLlmPrompt(string userPrompt, string databaseSchema)
        {
            return $@"You are a SQL query generator for SQL Server. You must follow these rules exactly:

DATABASE SCHEMA:
{databaseSchema}

USER REQUEST:
{userPrompt}

RESPONSE RULES:
1. Generate ONLY a valid T-SQL query with NO explanations, NO comments, and NO markdown.
2. Do not include ```sql or ``` tags.
3. Do not explain your reasoning or choices.
4. Return ONLY the raw SQL query text.
5. If you cannot generate a valid query, respond with 'ERROR:' followed by a brief message.

Example good response:
SELECT * FROM Sales WHERE Region = 'North'

Example bad response:
```sql
-- This query gets sales from the North region
SELECT * FROM Sales WHERE Region = 'North'
```

Now generate the SQL for the user request:";
        }

        private string ExtractSqlQuery(string llmResponse)
        {
            if (string.IsNullOrEmpty(llmResponse))
            {
                throw new Exception("LLM response was empty");
            }

            // Log original response for debugging
            _logger.LogDebug("Original LLM response: {Response}", llmResponse);

            // Trim whitespace and check for error
            var response = llmResponse.Trim();
            
            if (response.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(response);
            }

            // Remove any explanatory text before the query (often starts with phrases like "Here is", "Sure,", etc.)
            var prefixPatterns = new[] {
                @"^Sure,.*?(?=SELECT|WITH|INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)",
                @"^Here(?:\s+is)?.*?(?=SELECT|WITH|INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)",
                @"^I(?:\s+can)?.*?(?=SELECT|WITH|INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)",
                @"^This.*?(?=SELECT|WITH|INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)",
                @"^The.*?(?=SELECT|WITH|INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)",
                @"^A.*?(?=SELECT|WITH|INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)",
                @"^Based.*?(?=SELECT|WITH|INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)",
                @"^For.*?(?=SELECT|WITH|INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)"
            };

            foreach (var pattern in prefixPatterns)
            {
                response = Regex.Replace(response, pattern, string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            // Remove any markdown code block formatting
            response = Regex.Replace(response, @"```sql\s*", string.Empty, RegexOptions.IgnoreCase);
            response = Regex.Replace(response, @"```\s*", string.Empty, RegexOptions.IgnoreCase);

            // Remove SQL comments
            response = Regex.Replace(response, @"--.*?$", string.Empty, RegexOptions.Multiline);
            response = Regex.Replace(response, @"/\*[\s\S]*?\*/", string.Empty);

            // Remove any other explanations or text that might appear after the query
            var sqlKeywords = @"SELECT|FROM|WHERE|GROUP BY|HAVING|ORDER BY|JOIN|INNER JOIN|LEFT JOIN|RIGHT JOIN|OUTER JOIN|CROSS JOIN|UNION|UNION ALL|INSERT|UPDATE|DELETE|MERGE|TOP|DISTINCT|INTO|VALUES|SET|ON|AND|OR|NOT|LIKE|IN|BETWEEN|IS NULL|IS NOT NULL|AS|WITH|OVER|PARTITION BY|ROW_NUMBER|RANK|DENSE_RANK|NTILE|LEAD|LAG|FIRST_VALUE|LAST_VALUE|AVG|COUNT|MAX|MIN|SUM|CAST|CONVERT|CASE|WHEN|THEN|ELSE|END|BEGIN|COMMIT|ROLLBACK|TRANSACTION|IF|ELSE|WHILE|DECLARE|EXEC|EXECUTE|RETURN|GO|USE|DATABASE|TABLE|VIEW|PROCEDURE|FUNCTION|TRIGGER|INDEX|CONSTRAINT|PRIMARY KEY|FOREIGN KEY|UNIQUE|CHECK|DEFAULT|NULL|NOT NULL|AUTO_INCREMENT|IDENTITY";
            var lastSqlKeywordMatch = Regex.Match(response, $@"({sqlKeywords})[^;]*?;", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
            
            if (lastSqlKeywordMatch.Success)
            {
                var endIndex = lastSqlKeywordMatch.Index + lastSqlKeywordMatch.Length;
                if (endIndex < response.Length)
                {
                    response = response.Substring(0, endIndex);
                }
            }

            // Trim any whitespace again
            response = response.Trim();
            
            // Log cleaned response for debugging
            _logger.LogDebug("Cleaned SQL query: {CleanedResponse}", response);

            return response;
        }

        private class LlmResponse
        {
            [JsonProperty("response")]
            public string? Response { get; set; }
        }
    }
}