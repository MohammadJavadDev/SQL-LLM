using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SQLLLM.Models;
using SQLLLM.Services;

namespace SQLLLM.Controllers
{
    public class QueryController : Controller
    {
        private readonly ILlmService _llmService;
        private readonly ISqlService _sqlService;
        private readonly ILogger<QueryController> _logger;

        public QueryController(
            ILlmService llmService,
            ISqlService sqlService,
            ILogger<QueryController> logger)
        {
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _sqlService = sqlService ?? throw new ArgumentNullException(nameof(sqlService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult Index()
        {
            return View(new SqlQueryViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteQuery(SqlQueryViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserQuery))
            {
                model.ErrorMessage = "Please enter a query";
                return View("Index", model);
            }

            try
            {
                // Get database schema
                var dbSchema = _sqlService.GetDatabaseSchema();
                
                // Generate SQL query using LLM
                model.SqlQuery = await _llmService.GenerateSqlQueryAsync(model.UserQuery, dbSchema);
                
                // Execute the generated SQL query
                var (data, error) = await _sqlService.ExecuteSqlQueryAsync(model.SqlQuery);
                
                if (!string.IsNullOrEmpty(error))
                {
                    model.ErrorMessage = error;
                }
                else
                {
                    model.QueryResults = data;
                }
                
                model.IsExecuted = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query: {UserQuery}", model.UserQuery);
                model.ErrorMessage = $"An error occurred: {ex.Message}";
            }

            return View("Index", model);
        }
    }
}