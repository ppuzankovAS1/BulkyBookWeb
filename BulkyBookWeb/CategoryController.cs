
using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfwork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfwork = unitOfWork;
        }
        public IActionResult Index()
        {
            IEnumerable<Category> objCategoryList = _unitOfwork.Category.GetAll();
            return View(objCategoryList);
        }


        //GET
        public IActionResult Create()
        {
            return View();
        }
        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("DisplayOrder", "The DisplayOrder cannot match the Name.");
                //Error can have any unique name, Error message
            }
            if (ModelState.IsValid)
            {
                _unitOfwork.Category.Add(obj);
                _unitOfwork.Save();
                TempData["success"] = "Category Created";
                return RedirectToAction("Index");
            }
            return View(obj);
        }










        //GET
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            // var categoryFromDb = _db.Categories.Find(id);
            var categoryFromDbFirst = _unitOfwork.Category.GetFirstOrDefault(u => u.Id == id);
            //name is a property of u here

            if (categoryFromDbFirst == null)
            {
                return NotFound();
            }



            return View(categoryFromDbFirst);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("DisplayOrder", "The DisplayOrder cannot match the Name.");
                //Error can have any unique name, Error message
            }
            if (ModelState.IsValid)
            {
                _unitOfwork.Category.Update(obj);
                _unitOfwork.Save();
                TempData["success"] = "Category Updated";
                return RedirectToAction("Index");
            }
            return View(obj);
        }


        //GET- called by Index.cshtml 
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var categoryFromDb = _unitOfwork.Category.GetFirstOrDefault(u => u.Id == id);
            //           var categoryFromDbFirst = _db.Categories.FirstOrDefault(u => u.Id == id);
            //            var categoryFromDbSingle = _db.Categories.SingleOrDefault(u => u.Id == id);

            if (categoryFromDb == null)
            {
                return NotFound();  //I think this is a page not found message
            }


            return View(categoryFromDb);
        }

        //POST - called by Delete.cshtml
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {

            var CategoryFromDb = _unitOfwork.Category.GetFirstOrDefault(u => u.Id == id);
            if (CategoryFromDb == null)
            {
                return NotFound();  //I think this is a page not found message
            }
            _unitOfwork.Category.Remove(CategoryFromDb);
            _unitOfwork.Save();
            TempData["success"] = "Category Deleted";
            return RedirectToAction("Index");
        }



    }










}


