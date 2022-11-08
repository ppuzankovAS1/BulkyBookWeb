using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;


namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfwork;
        private readonly IEmailSender _emailSender;
        public ShoppingCartVM shoppingCartVM { get; set; }
        public int OrderTotal { get; set; }
        public CartController(IUnitOfWork unitOfwork,IEmailSender emailSender)
        {
            _unitOfwork = unitOfwork;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfwork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"),
                orderHeader = new()
            };
            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                shoppingCartVM.orderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return View(shoppingCartVM);
        }







        public IActionResult Summary()              //GET action method
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCartVM = new ShoppingCartVM()  //list all the selections made within this purchase
            {
                ListCart = _unitOfwork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"),
                orderHeader = new()
            };

            //copy logged in user record to shoppingCartVM.ApplicationUser object
            shoppingCartVM.orderHeader.ApplicationUser = _unitOfwork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            //copying from shoppingcart table to shoppingCartVM.orderHeader object

            shoppingCartVM.orderHeader.Name = shoppingCartVM.orderHeader.ApplicationUser.Name;
            shoppingCartVM.orderHeader.PhoneNumber = shoppingCartVM.orderHeader.ApplicationUser.PhoneNumber;
            shoppingCartVM.orderHeader.StreetAddress = shoppingCartVM.orderHeader.ApplicationUser.StreetAddress;
            shoppingCartVM.orderHeader.City = shoppingCartVM.orderHeader.ApplicationUser.City;
            shoppingCartVM.orderHeader.State = shoppingCartVM.orderHeader.ApplicationUser.State;
            shoppingCartVM.orderHeader.PostalCode = shoppingCartVM.orderHeader.ApplicationUser.PostalCode;

            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                shoppingCartVM.orderHeader.OrderTotal += cart.Price * cart.Count;

            }

            return View(shoppingCartVM);

            return View();
        }


        [HttpPost]
        [ActionName("Summary")]             //if I had named it SummaryPost I would not have needed this line
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(ShoppingCartVM shoppingCartVM)              //Post action method
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCartVM.ListCart = _unitOfwork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product");



            shoppingCartVM.orderHeader.OrderDate = System.DateTime.Now;
            shoppingCartVM.orderHeader.ApplicationUserId = claim.Value;



            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                shoppingCartVM.orderHeader.OrderTotal += cart.Price * cart.Count;

            }
            //save logged in user record                        
            ApplicationUser applicationUser = _unitOfwork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)  //if this is an individual customer
            {
                shoppingCartVM.orderHeader.PaymentStatus = SD.PaymentStatusPending;
                shoppingCartVM.orderHeader.OrderStatus = SD.StatusPending;
            }
            else                                                     //if this is a company customer
            {
                shoppingCartVM.orderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                shoppingCartVM.orderHeader.OrderStatus = SD.PaymentStatusApproved;
            }



            //save all the fields copied to the shoppingCartVM object in the GET method to a new record in the
            //OrderHeaders table.
            _unitOfwork.OrderHeader.Add(shoppingCartVM.orderHeader);
            _unitOfwork.Save();

            foreach (var cart in shoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new()       //OrderDetail is not a property of shoppingCartVM 
                {                                      //but I can still create a constructor for it
                    ProductId = cart.ProductId,           //cart is a list item object inside ListCart enumerable set
                    OrderId = shoppingCartVM.orderHeader.Id,        //shoppingCartVM is an object of ShoppingCartVM itself 
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfwork.OrderDetail.Add(orderDetail);     //for every item in the shopping cart
                _unitOfwork.Save();
            }



            if (applicationUser.CompanyId.GetValueOrDefault() == 0)  //if this is an individual customer process payment
            {


                //stripe settings

                var domain = "https://localhost:44347/";

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={shoppingCartVM.orderHeader.Id}",
                    //area/controller/action

                    CancelUrl = domain + $"customer/cart/index",
                };

                foreach (var item in shoppingCartVM.ListCart)
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
                _unitOfwork.OrderHeader.UpdateStripeSessionId(shoppingCartVM.orderHeader.Id, session.Id);
                _unitOfwork.Save();                                         // save sessionId and PaymentIntentId before redirect

                Response.Headers.Add("Location", session.Url); // redirects to stripe portal with status code 303
                return new StatusCodeResult(303);
            }
            else                                        //if this is a company customer redirect to Order Confirmation and pass the Order Id
            {
                return RedirectToAction("OrderConfirmation", "Cart", new { id = shoppingCartVM.orderHeader.Id });

            }

        }


        public IActionResult OrderConfirmation(int id)
        {



            OrderHeader orderHeader = _unitOfwork.OrderHeader.GetFirstOrDefault(u => u.Id == id, includeProperties: "ApplicationUser");

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)   // if this is not company user
            {                                                                                 //then payment must be approved
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);  //the Session was already created above
                _unitOfwork.OrderHeader.UpdateStripePaymentId(id, session.PaymentIntentId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfwork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfwork.Save();                     //static variables holding a string
                }

            }

            _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "Order Confirmation - Bulky Book",
                                                                "<p>Your order has been placed succssfully!</p>");
            List<ShoppingCart> shoppingCarts = _unitOfwork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            //retrieve the shopping cart list in order to remove below

            _unitOfwork.ShoppingCart.RemoveRange(shoppingCarts);
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            _unitOfwork.Save();
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfwork.ShoppingCart.GetAll
                        (u => u.ApplicationUserId == claim.Value).ToList().Count);
            return View(id);
        }

        private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if (quantity <= 50)
            {
                return price;
            }
            else
            {
                if (quantity <= 100)
                {
                    return price50;
                }
                else
                {
                    return price100;
                }
            }
        }


        public IActionResult Increment(int? id)
        {
            ShoppingCart cartFromDb = _unitOfwork.ShoppingCart.GetFirstOrDefault(u => u.Id == id);
            _unitOfwork.ShoppingCart.IncrementCount(cartFromDb, 1);
            _unitOfwork.Save();
            return RedirectToAction(nameof(Index));
        }


        public IActionResult Decrement(int? id)
        {

            ShoppingCart cartFromDb = _unitOfwork.ShoppingCart.GetFirstOrDefault(u => u.Id == id);
            if (cartFromDb.Count > 1)
            {
                _unitOfwork.ShoppingCart.DecrementCount(cartFromDb, 1);
                _unitOfwork.Save();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                _unitOfwork.ShoppingCart.Remove(cartFromDb);
                _unitOfwork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfwork.ShoppingCart.GetAll    
                            (u => u.ApplicationUserId == claim.Value).ToList().Count);
            }
            return RedirectToAction(nameof(Index));

        }

        public IActionResult Remove(int? id)
        {
            ShoppingCart cartFromDb = _unitOfwork.ShoppingCart.GetFirstOrDefault(u => u.Id == id);
            _unitOfwork.ShoppingCart.Remove(cartFromDb);
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            _unitOfwork.Save();
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfwork.ShoppingCart.GetAll
                        (u => u.ApplicationUserId == claim.Value).ToList().Count);
            return RedirectToAction(nameof(Index));
        }


    }
}
