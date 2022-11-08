
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {               //HomeController implements the base class Controller


        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {                       //registering logger using dependency injection
            _logger = logger;
            _unitOfWork = unitOfWork;
        }




        //action methods
        public IActionResult Index()                    //default
        {
            //home
            IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");
            return View(objProductList);                              //home/index  
        }





        //action methods
        public IActionResult Details(int? productId)   //selected product id from the index.chshtml home screen                
        {

            ShoppingCart ShCartObject = new()  //creating a new shopping cart object
            {

                Count = 1,       //default starting Count
                ProductId = (int)productId,                    //      ▼- Index of the Product table
                Product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == productId, includeProperties: "Category,CoverType")
            };





            return View(ShCartObject);                              //home/index  

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart obj)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            obj.ApplicationUserId = claim.Value;
            // ▼-foreign key in ShoppingCart table  
            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.ApplicationUserId == claim.Value
                                                                                 && u.ProductId == obj.ProductId);
            //if the logged in userid is the same
            //as the one in the shopping cart
            //         AND
            //if Product Id in the table is the same as
            //Product ID in the form
            //         THEN
            //assign cartFromDb object to the retrieved record
            if (cartFromDb == null)                                     //add a shopping cart record
            {

                _unitOfWork.ShoppingCart.Add(obj);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll    //get # of shopping cart records 
                            (u => u.ApplicationUserId == claim.Value).ToList().Count);          //NOT count (number of items)   
            }
            else                                                      //don't add a shopping cart record just increment count
            {                                                                                                  //(# of items)
                //obj.Count = obj.Count + cartFromDb.Count   
                _unitOfWork.ShoppingCart.IncrementCount(cartFromDb, obj.Count);
            }
            _unitOfWork.Save();
            TempData["success"] = "Items Added to Shopping Cart";
            return RedirectToAction(nameof(Index));

        }










        public IActionResult Privacy()                   //home/privacy
        {
            return View();
        }





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}