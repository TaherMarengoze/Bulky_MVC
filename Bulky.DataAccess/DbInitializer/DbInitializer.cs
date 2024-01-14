﻿using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.DbInitializer;

public class DbInitializer : IDbInitializer
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public DbInitializer(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public void Initialize()
    {
        //migration if they are not applied
        try
        {
            if (_db.Database.GetPendingMigrations().Count() > 0)
            {
                _db.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            throw;
        }

        //create roles if they are not created
        if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
        {
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();

            //if roles are not created, then create admin user as well
            _userManager.CreateAsync(
                new ApplicationUser
                {
                    UserName = "firstClassAdmin@gmail.com",
                    Email = "firstClassAdmin@gmail.com",
                    Name = "First Class Admin",
                    PhoneNumber = "01213141516",
                    StreetAddress = "45 Liberty Street",
                    State = "VR",
                    PostalCode = "85202",
                    City = "Alexandria"
                },
                "Admin_1").GetAwaiter().GetResult();

            ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "firstClassAdmin@gmail.com");
            _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
        }

        return;
    }
}