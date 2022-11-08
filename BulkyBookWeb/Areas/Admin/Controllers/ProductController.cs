using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfwork;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfwork = unitOfWork;
            _hostEnvironment = hostEnvironment;
        }
        public IActionResult Index()
        {
            return View();
        }





        //GET
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()        //This looks like a constructor for the productVM class
            {
                Product = new(),
                CategoryList = _unitOfwork.Category.GetAll().Select(    //this looks like the drop down menu choice
                    i => new SelectListItem
                    {
                        Text = i.Name,
                        Value = i.Id.ToString()
                    }),
                CoverTypeList = _unitOfwork.CoverType.GetAll().Select(
                 i => new SelectListItem
                 {
                     Text = i.Name,
                     Value = i.Id.ToString()
                 }),


            };

            if (id == null || id == 0)
            {   //create product
                //ViewBag.CategoryList = CategoryList;  //pass SelectListItem object to the ViewBag object
                //ViewData["CoverTypeList"] = CoverTypeList; //pass SelectListItem object to the ViewData object
                return View(productVM);
            }
            else
            {
                //update product



                productVM.Product = _unitOfwork.Product.GetFirstOrDefault(u => u.Id == id);
                return View(productVM);

            }


            return View(productVM);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]                  //'?' makes it return null if there is nothing
        public IActionResult Upsert(ProductVM obj, IFormFile? file)   //If the file was opened in Upsert.cshtml IFormFile gets that and
                                                                      //puts it in the 'file' object

        {

            if (ModelState.IsValid)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;  //wwwroot directory
                if (file != null)       //file is the object name in upsert.cshtml that opens a file
                                        // ie-a0641560-c800-432b-bd3f-7e1598756005.png
                {                          //I guess this has something to do with filename.ext format
                    string fileName = Guid.NewGuid().ToString();       //gets the filename from the file object
                    var uploads = Path.Combine(wwwRootPath, @"images\products");  //user created variable that has the path
                    var extension = Path.GetExtension(file.FileName);             //gets file extension from the 'file' object


                    if (obj.Product.ImageUrl != null)   //is this checking the database or upsert page?
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);    //delete the previous image file
                        }
                    }


                    using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    {
                        file.CopyTo(fileStreams);                   //use all the above to copy file to the filestream location
                    }                                               //this is the /images/product/ location I created  
                    obj.Product.ImageUrl = @"\images\products\" + fileName + extension; //this filepath will be saved in the 
                }                                                                       //obj.imageURL property.

                if (obj.Product.Id == 0)
                {
                    _unitOfwork.Product.Add(obj.Product);
                }
                else
                {
                    _unitOfwork.Product.Update(obj.Product);
                }
                //The source file
                //path is not saved. 
                _unitOfwork.Save();                                                     //and finally to the db
                TempData["success"] = "Product Created";
                return RedirectToAction("Index");
            }
            return View(obj);



        }



        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _unitOfwork.Product.GetAll(includeProperties: "Category");
            return Json(new { data = productList }); //returns all the products as a JSON string
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfwork.Product.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            string wwwRootPath = _hostEnvironment.WebRootPath;
            var oldImagePath = Path.Combine(wwwRootPath, obj.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);    //delete the previous image file
            }



            _unitOfwork.Product.Remove(obj);
            _unitOfwork.Save();
            return Json(new { success = true, message = "Record Deleted" });
            return RedirectToAction("Index");


        }
        #endregion





    }








}


