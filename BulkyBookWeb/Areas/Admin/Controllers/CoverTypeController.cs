
using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfwork;

        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfwork = unitOfWork;
        }
        public IActionResult Index()
        {
            IEnumerable<CoverType> objCoverTypeList = _unitOfwork.CoverType.GetAll();
            return View(objCoverTypeList);
        }


        //GET
        public IActionResult Create()
        {
            return View();
        }
        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfwork.CoverType.Add(obj);
                _unitOfwork.Save();
                TempData["success"] = "Cover Type Created";
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
            var covertypeFromDbFirst = _unitOfwork.CoverType.GetFirstOrDefault(u => u.Id == id);
            //name is a property of u here

            if (covertypeFromDbFirst == null)
            {
                return NotFound();
            }



            return View(covertypeFromDbFirst);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfwork.CoverType.Update(obj);
                _unitOfwork.Save();
                TempData["success"] = "Cover Type Updated";
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
            var covertypeFromDb = _unitOfwork.CoverType.GetFirstOrDefault(u => u.Id == id);

            if (covertypeFromDb == null)
            {
                return NotFound();  //I think this is a page not found message
            }


            return View(covertypeFromDb);
        }

        //POST - called by Delete.cshtml
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {

            var covertypeFromDb = _unitOfwork.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (covertypeFromDb == null)
            {
                return NotFound();  //I think this is a page not found message
            }
            _unitOfwork.CoverType.Remove(covertypeFromDb);
            _unitOfwork.Save();
            TempData["success"] = "Cover Type Deleted";
            return RedirectToAction("Index");
        }



    }










}


