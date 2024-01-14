using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    public class CreateModel(ApplicationDbContext db) : PageModel
    {
        [BindProperty]
        public Category Category { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            db.Add(Category);
            db.SaveChanges();

            TempData["success"] = "Category created successfully";

            return RedirectToPage("Index");
        }
    }
}
