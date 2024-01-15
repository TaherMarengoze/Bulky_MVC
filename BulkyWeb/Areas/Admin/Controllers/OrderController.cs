using System.Security.Claims;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OrderController(IUnitOfWork unitOfWork) : Controller
{
    [BindProperty]
    public OrderVM OrderVM { get; set; }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int orderId)
    {
        OrderVM = new()
        {
            OrderHeader = unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
            OrderDetail = unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
        };

        return View(OrderVM);
    }

    [HttpPost]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

        orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderVM.OrderHeader.City;
        orderHeaderFromDb.State = OrderVM.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

        if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;

        if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;

        unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        unitOfWork.Save();

        TempData["Success"] = "Order Details Updated Successfully.";

        return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
    }

    [HttpPost]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public IActionResult StartProcessing()
    {
        unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
        unitOfWork.Save();

        TempData["Success"] = "Order Details Updated Successfully.";

        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public IActionResult ShipOrder()
    {
        var orderHeader = unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
        orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
        orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
        orderHeader.OrderStatus = SD.StatusShipped;
        orderHeader.ShippingDate = DateTime.Now;

        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
        }

        unitOfWork.OrderHeader.Update(orderHeader);
        unitOfWork.Save();

        TempData["Success"] = "Order Shipped Successfully.";

        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public IActionResult CancelOrder()
    {
        var orderHeader = unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

        if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeader.PaymentIntentId
            };

            var service = new RefundService();
            Refund refund = service.Create(options);

            unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
        }
        else
        {
            unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
        }
        unitOfWork.Save();

        TempData["Success"] = "Order Cancelled Successfully.";

        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [ActionName(nameof(Details))]
    public IActionResult Details_PayNow()
    {
        OrderVM.OrderHeader = unitOfWork.OrderHeader
            .Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");

        OrderVM.OrderDetail = unitOfWork.OrderDetail
            .GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

        //stripe logic
        var domain = $"{Request.Scheme}://{Request.Host.Value}/";
        var options = new SessionCreateOptions
        {
            SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
            CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
        };

        foreach (var cart in OrderVM.OrderDetail)
        {
            SessionLineItemOptions sessionLineItem = new()
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(cart.Price * 100),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions { Name = cart.Product.Title }
                },
                Quantity = cart.Count
            };
            options.LineItems.Add(sessionLineItem);
        }

        var service = new SessionService();
        Session session = service.Create(options);

        unitOfWork.OrderHeader.UpdateStripePaymentId(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
        unitOfWork.Save();

        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(StatusCodes.Status303SeeOther);
    }

    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        OrderHeader orderHeader = unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);

        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            //order made by a company
            SessionService service = new();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                unitOfWork.Save();
            }
        }

        return View(orderHeaderId);
    }

    #region API Calls
    [HttpGet]
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> objOrderHeaders;

        if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
        {
            objOrderHeaders = unitOfWork.OrderHeader
                .GetAll(includeProperties: "ApplicationUser").ToList();
        }
        else
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            objOrderHeaders = unitOfWork.OrderHeader
                .GetAll(u=>u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
        }

        switch (status)
        {
            case "pending":
            objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
            break;

            case "inprocess":
            objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
            break;

            case "completed":
            objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
            break;

            case "approved":
            objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
            break;

            default:
            break;
        }

        return Json(new
        {
            data = objOrderHeaders
        });
    }
    #endregion
}
