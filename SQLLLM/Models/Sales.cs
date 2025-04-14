using System;
using System.ComponentModel.DataAnnotations;

namespace SQLLLM.Models
{
    public class Sales
    {
        [Key]
        public int Id { get; set; }
        
        public string? Region { get; set; }
        
        public DateTime SaleDate { get; set; }
        
        public decimal SalesAmount { get; set; }
    }
}