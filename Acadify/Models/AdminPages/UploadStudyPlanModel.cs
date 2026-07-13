using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Acadify.Models.AdminPages
{
    public class UploadStudyPlanModel
    {
        [Required(ErrorMessage = "Please upload Study Plan Excel file.")]
        public IFormFile? StudyPlanFile { get; set; }


        public string? SavedFileName { get; set; }

        public string? Message { get; set; }
    }
}