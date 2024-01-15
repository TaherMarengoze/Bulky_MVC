using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class UserController(ApplicationDbContext db) : Controller
{
    public IActionResult Index() => View();

    #region API Calls
    [HttpGet]
    public IActionResult GetAll()
    {

        List<ApplicationUser> objUserList =
            db.ApplicationUsers.Include(u => u.Company).ToList();

        var userRoles = db.UserRoles.ToList();
        var roles = db.Roles.ToList();

        foreach (var user in objUserList)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var roldId = userRoles.FirstOrDefault(u => u.UserId == user.Id).RoleId;
            user.Role = db.Roles.FirstOrDefault(u => u.Id == roldId).Name;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            user.Company ??= new() { Name = "" };
        }

        return base.Json(new { data = objUserList });
    }

    #endregion
}
