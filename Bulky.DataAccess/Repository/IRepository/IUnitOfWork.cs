﻿namespace Bulky.DataAccess.Repository.IRepository;

public interface IUnitOfWork
{
    ICategoryRepository Category { get; }
    IProductRepository Product { get; }
    ICompanyRepository Company { get; }
    IShoppingCartRepository ShoppingCart { get; }
    IOrderDetailRepository OrderDetail { get; }
    IOrderHeaderRepository OrderHeader { get; }

    IApplicationUserRepository ApplicationUser { get; }

    void Save();
}
