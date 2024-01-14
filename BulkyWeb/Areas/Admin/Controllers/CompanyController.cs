using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class CompanyController(IUnitOfWork unitOfWork) : Controller
{
    public IActionResult Index() => View();

    public IActionResult Upsert(int? id) => View(id == null || id == 0 ? new Company() : unitOfWork.Company.Get(u => u.Id == id));

    [HttpPost]
    public IActionResult Upsert(Company company)
    {
        if (ModelState.IsValid)
        {
            bool createNew = company.Id == 0;

            if (createNew)
                unitOfWork.Company.Add(company);
            else
                unitOfWork.Company.Update(company);

            unitOfWork.Save();

            TempData["success"] = $"Company {(createNew ? "Created" : "Updated")} Successfully";
            return RedirectToAction("Index");
        }
        else
            return View(company);
    }

    #region API Calls
    [HttpGet]
    public IActionResult GetAll() => Json(new { data = unitOfWork.Company.GetAll().ToList() });

    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var companyToBeDeleted = unitOfWork.Company.Get(u => u.Id == id);

        if (companyToBeDeleted == null)
            return Json(new { success = false, message = "Error while deleting" });

        unitOfWork.Company.Remove(companyToBeDeleted);
        unitOfWork.Save();

        return Json(new { success = true, message = "Company Deleted Successfully" });
    }
    #endregion
}
