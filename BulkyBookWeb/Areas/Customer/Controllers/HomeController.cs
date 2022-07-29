
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;

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




        //2action methods
        public IActionResult Index()                    //default
        {
            //home
            IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");
            return View(objProductList);                              //home/index  
        }





        //2action methods
        public IActionResult Details(int? id)                    //default
        {

            ShoppingCart ShCartObject = new()
            {
  
                Count = 1,
                Product  = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id, includeProperties: "Category,CoverType")
        };

 
                return View(ShCartObject);                              //home/index  

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