﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CookBookWebRole.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "CookBook application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Recipes()
        {
            ViewBag.Message = "All Recipes in JsGrid.";

            return View();
        }
        
        public ActionResult RecipesKendo()
        {
            ViewBag.Message = "All Recipes in KendoGrid.";

            return View();
        }
    }
}