using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Zekiah.FGDB;
using Esri.FileGDB;

namespace FgdbLeafletSample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "States that begin with the letter 'M'";
            ViewBag.Subtitle = "A simple application to demostrate GeoJSON extension methods for the Esri File Geodatabase API";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult States()
        {
     
            try
            {
                var path = Server.MapPath("/App_Data/mvc_samples.gdb");
                Geodatabase gdb = Geodatabase.Open(path);
                Table statesTable = gdb.OpenTable("\\us_states");
                RowCollection rows = statesTable.Search("*", "STATE_NAME LIKE 'M%'", RowInstance.Recycle);
                var rval = rows.ToGeoJson();
                gdb.Close();
                //gdb.Dispose();
                Response.ContentType = "application/json";
                object result = this.Content(rval);
                return result as ActionResult; ;
            }
            catch { return null; }
        }

    }
}
