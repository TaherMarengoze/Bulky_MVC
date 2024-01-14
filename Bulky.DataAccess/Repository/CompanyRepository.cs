using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class CompanyRepository(ApplicationDbContext db) : Repository<Company>(db), ICompanyRepository
{
    public void Update(Company obj)
    {
        //db.Companyies.Update(obj);
        var objFromDb = db.Companies.FirstOrDefault(u => u.Id == obj.Id);
        if (objFromDb != null)
        {
            objFromDb.Name = obj.Name;
            objFromDb.StreetAddress = obj.StreetAddress;
            objFromDb.City = obj.City;
            objFromDb.State = obj.State;
            objFromDb.PostalCode = obj.PostalCode;
            objFromDb.PhoneNumber = obj.PhoneNumber;
        }
    }
}
