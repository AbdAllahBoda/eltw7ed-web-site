﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using twa7edWebsite.Models;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace twa7edWebsite.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private taw7ed_DBEntities db = new taw7ed_DBEntities();

        //
        // GET: /Categories/
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View(db.Categories.ToList());
        }

        //
        // GET: /Categories/Details/5
        [AllowAnonymous]
        public ActionResult Details(int id = 0)
        {
            Category category = db.Categories.Find(id);
            if (category == null)
            {

                //return HttpNotFound(); //you might want to make a view to handle HTTPNotFound
                return View("Error");
            }
            ViewBag.isItem = 0;
            return View(category);
        }

        //
        // GET: /Categories/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Categories/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Exclude = "ID")] Category category, HttpPostedFileBase file)
        {

            this.severSideVaildationsForCreate_Edit(category, file);
            if (ModelState.IsValid)
            {
                var fileName = Path.GetFileName(file.FileName);

                PutObjectResponse response = helperMethods.SaveImageToAmazon("tawyed.categories.images", file);
                
                category.imageName = fileName;

                db.Categories.Add(category);
                db.SaveChanges();

               TempData["AlertMessage"] = "تم حفظ الصنف، شكرًا :)";
                return RedirectToAction("Index");
            }

            return View(category);
        }

        //
        // GET: /Categories/Edit/5

        public ActionResult Edit(int id = 0)
        {
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return View("Error");
            }
            Session["_prvImageName"] = category.imageName;
            return View(category);
        }

        //
        // POST: /Categories/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Exclude = "ID")] Category category, HttpPostedFileBase file)
        {
            this.severSideVaildationsForCreate_Edit(category, file);
            if (ModelState.IsValid)
            {
                var fileName = Path.GetFileName(file.FileName);
                string prevImageName = ((string)Session["_prvImageName"]);
                if (prevImageName != fileName)
                {   
                    //delete the older image
                    DeleteObjectResponse response = helperMethods.DeleteImageFromAmazon("tawyed.categories.images", prevImageName);

                    //rename the imageName
                    category.imageName = fileName;

                    //save the new image
                    PutObjectResponse r2 = helperMethods.SaveImageToAmazon("tawyed.categories.images", file);
                }
                db.Entry(category).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        //
        // GET: /Categories/Delete/5

        public ActionResult Delete(int id = 0)
        {
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return View("Error");
            }
            return View(category);
        }

        //
        // POST: /Categories/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return View("Error");
            }
            //get the path of the older image
            var path = helperMethods.formImageNameToImagePath(category.imageName, "tawyed.categories.images");
            //delete the older image


            db.Categories.Remove(category);
            db.SaveChanges();
            DeleteObjectResponse r = helperMethods.DeleteImageFromAmazon("tawyed.categories.images", category.imageName);
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        #region sever-side vaildations
        private void vaildateStringAttribute(string attributeName, string attributeData)
        {
            if (String.IsNullOrEmpty(attributeData) ||
                String.IsNullOrWhiteSpace(attributeData)) ModelState.AddModelError(@attributeName, @attributeName + " can't be empty or spaces");
        }
        private void severSideVaildationsForCreate_Edit(Category category, HttpPostedFileBase file)
        {
            this.vaildateStringAttribute("name", category.name);

            if (file == null || file.ContentLength < 1) ModelState.AddModelError("imageName", "Image required");
            if (file != null &&  !file.IsImage()) ModelState.AddModelError("imageName", "You have to upload an Image");
        }
        #endregion
    }
}