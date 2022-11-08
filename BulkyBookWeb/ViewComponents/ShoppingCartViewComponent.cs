using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyBookWeb.ViewComponents
{
    public class ShoppingCartViewComponent: ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)                                //if user is logged in
            {
                if(HttpContext.Session.GetInt32(SD.SessionCart)!= null)          //if session is already set return it
                {
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
                else                                                             //otherwise return the number of items in
                {                                                                //shopping cart from db
                    HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll    
                                 (u => u.ApplicationUserId == claim.Value).ToList().Count);    
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
            }
            else                                            //if user logs out or not logged in yet
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }


    }
}
