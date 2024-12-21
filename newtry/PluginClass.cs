using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Attributes;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Media;


namespace InitialAPP
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class PluginClass : IExternalApplication
    {

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            application.CreateRibbonTab("Columns Plugin");
            RibbonPanel ribbon = application.CreateRibbonPanel("Columns Plugin", "Columns Plugin");
            string path = Assembly.GetExecutingAssembly().Location;
            PushButtonData button = new PushButtonData("button nnn", "Columns Plugin", path, "InitialAPP.AddinClass"); 
            PushButton pushButton = ribbon.AddItem(button) as PushButton;
            string newPath = $@"{path}App.jpg";
            newPath = newPath.Replace("InitialAPP.dll", "");
            Uri uripath = new Uri(newPath);
            pushButton.LargeImage = new BitmapImage(uripath);
            return Result.Succeeded;

        }
    }
}
