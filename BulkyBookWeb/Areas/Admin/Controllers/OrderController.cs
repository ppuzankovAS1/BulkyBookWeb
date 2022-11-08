using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Authorize]                  //only logged in user can see records
    public class OrderController : Controller
    {

        private readonly IUnitOfWork _unitOfwork;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public OrderVM orderVM { get; set; }
        public OrderVM orderHeaderFromDb { get; set; }
        public OrderController(IUnitOfWork unitOfwork, IEmailSender emailSender)
        {
            _unitOfwork = unitOfwork;
            _emailSender = emailSender; ;
        }


        public IActionResult Index()
        {

            return View();
        }


        public IActionResult Details(int? id)
        {

            orderVM = new OrderVM()
            {
                ListDetails = _unitOfwork.OrderDetail.GetAll(u => u.OrderId == id, includeProperties: "Product"),
                orderHeader = new()
            };

            orderVM.orderHeader = _unitOfwork.OrderHeader.GetFirstOrDefault(u => u.Id == id, includeProperties: "ApplicationUser");
            return View(orderVM);
        }



        [HttpPost]
        [ActionName("Details")]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrder(int? id)     //only emplyee or admin can update
        {
            orderHeaderFromDb = new OrderVM()
            {
                orderHeader = new()
            };


            orderHeaderFromDb.orderHeader = _unitOfwork.OrderHeader.GetFirstOrDefault(u => u.Id == id, tracked: false);
            orderHeaderFromDb.orderHeader.Name = orderVM.orderHeader.Name;
            orderHeaderFromDb.orderHeader.PhoneNumber = orderVM.orderHeader.PhoneNumber;
            orderHeaderFromDb.orderHeader.StreetAddress = orderVM.orderHeader.StreetAddress;
            orderHeaderFromDb.orderHeader.City = orderVM.orderHeader.City;
            orderHeaderFromDb.orderHeader.State = orderVM.orderHeader.State;
            orderHeaderFromDb.orderHeader.PostalCode = orderVM.orderHeader.PostalCode;

            if (orderVM.orderHeader.Carrier != null)
            {
                orderHeaderFromDb.orderHeader.Carrier = orderVM.orderHeader.Carrier;
            }
            if (orderVM.orderHeader.TrackingNumber != null)
            {
                orderHeaderFromDb.orderHeader.TrackingNumber = orderVM.orderHeader.TrackingNumber;
            }

            _unitOfwork.OrderHeader.Update(orderHeaderFromDb.orderHeader);
            _unitOfwork.Save();
            TempData["success"] = "Order Updated";
            return RedirectToAction("Details", "Order", new { id = orderVM.orderHeader.Id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing()
        {
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))  //only admin or employee
            {
                var orderHeaderFromDb = _unitOfwork.OrderHeader.GetFirstOrDefault(u => u.Id == orderVM.orderHeader.Id, tracked: false);
                _unitOfwork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusInProcess);
                _unitOfwork.Save();
                TempData["success"] = "Order in Process";
                return RedirectToAction("Details", "Order", new { id = orderVM.orderHeader.Id });
            }
            return View();
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]               //only admin or employee
        [ValidateAntiForgeryToken]
        public IActionResult Ship()
        {
            var orderHeaderFromDb = _unitOfwork.OrderHeader.GetFirstOrDefault(u => u.Id == orderVM.orderHeader.Id, tracked: false);

            orderHeaderFromDb.Carrier = orderVM.orderHeader.Carrier;
            orderHeaderFromDb.TrackingNumber = orderVM.orderHeader.TrackingNumber;
            orderHeaderFromDb.ShippingDate = System.DateTime.Now;
            orderHeaderFromDb.OrderStatus = SD.StatusShipped;
            if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeaderFromDb.PaymentDueDate = System.DateTime.Now.AddDays(30);
            }
            _unitOfwork.OrderHeader.Update(orderHeaderFromDb);

            _unitOfwork.Save();
            TempData["success"] = "Order Shipped";
            return RedirectToAction("Details", "Order", new { id = orderVM.orderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee + "," + SD.Role_User_Comp)]               //only admin or employee
        [ValidateAntiForgeryToken]
        public IActionResult CompanyPayment()
        {

            orderVM.ListDetails = _unitOfwork.OrderDetail.GetAll(u => u.OrderId == orderVM.orderHeader.Id, includeProperties: "Product");

            orderVM.orderHeader = _unitOfwork.OrderHeader.GetFirstOrDefault(u => u.Id == orderVM.orderHeader.Id,
                                                                                                    includeProperties: "ApplicationUser");

            var domain = "https://localhost:44347/";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?purchaseId={orderVM.orderHeader.Id}",
                //area/controller/action

                CancelUrl = domain + $"admin/order/details?id={orderVM.orderHeader.Id}",
            };

            foreach (var item in orderVM.ListDetails)
            {

                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),   //convert to double in cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title,
                        },

                    },
                    Quantity = item.Count, //it will autmatically calculate the grand total based on count
                };
                options.LineItems.Add(sessionLineItem);  //the line item will be created for every item in the 
            }                                             //shopping cart for the current user

            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfwork.OrderHeader.UpdateStripeSessionId(orderVM.orderHeader.Id, session.Id);
            _unitOfwork.Save();                                         // save sessionId and PaymentIntentId before redirect

            Response.Headers.Add("Location", session.Url); // redirects to stripe portal with status code 303
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int purchaseId)
        {

            OrderHeader orderHeader = _unitOfwork.OrderHeader.GetFirstOrDefault(u => u.Id == purchaseId, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                _unitOfwork.OrderHeader.UpdateStripePaymentId(purchaseId, session.PaymentIntentId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfwork.OrderHeader.UpdateStatus(purchaseId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                }
                _unitOfwork.Save();
                _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "Payment Confirmation - Bulky Book",
                                                                    "<p>Your payment has been received succssfully!</p>");
                return RedirectToAction("PaymentConfirmation", "Order", new { purchaseId = orderHeader.Id });
            }
            return View(purchaseId);

        }





        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]              //only admin or employee 
        [ValidateAntiForgeryToken]
        public IActionResult Cancel()
        {
            var orderHeaderFromDb = _unitOfwork.OrderHeader.GetFirstOrDefault(u => u.Id == orderVM.orderHeader.Id, tracked: false);
            if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDb.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfwork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfwork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
            }


            _unitOfwork.Save();
            TempData["success"] = "Order Canceled";
            return RedirectToAction("Details", "Order", new { id = orderVM.orderHeader.Id });
        }




        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string? status)
        {
            IEnumerable<OrderHeader> ListOrderHeaders;


            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                ListOrderHeaders = _unitOfwork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                ListOrderHeaders = _unitOfwork.OrderHeader.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "ApplicationUser");
            }



            if (status == "inprocess")
            {
                ListOrderHeaders = ListOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
            }
            else if (status == "pending")
            {
                ListOrderHeaders = ListOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
            }
            else if (status == "completed")
            {
                ListOrderHeaders = ListOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
            }
            else if (status == "approved")
            {
                ListOrderHeaders = ListOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
            }

            return Json(new { data = ListOrderHeaders });

            #endregion
        }



    }
}



