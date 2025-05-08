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
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class PluginClass : IExternalApplication
    {
        internal static PluginClass thisApp = null;
        private UserUIForm m_MyForm;
        public Result OnShutdown(UIControlledApplication application)
        {


            if (m_MyForm != null && m_MyForm.Visible)
            {
                m_MyForm.Close();
            }

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            m_MyForm = null;   // no dialog needed yet; the command will bring it
            thisApp = this;
            application.CreateRibbonTab("My Plugin");
            RibbonPanel ribbon = application.CreateRibbonPanel("My Plugin", "My Plugin");
            string path = Assembly.GetExecutingAssembly().Location;
            PushButtonData button = new PushButtonData("button nnn", "My Plugin", path, "InitialAPP.AddinClass"); 
            PushButton pushButton = ribbon.AddItem(button) as PushButton;
            string newPath = $@"{path}App.jpg";
            newPath = newPath.Replace("InitialAPP.dll", "");
            Uri uripath = new Uri(newPath);
            pushButton.LargeImage = new BitmapImage(uripath);
            return Result.Succeeded;

        }
        public void ShowForm(UIApplication uiapp, ExternalCommandData commandData)
        {
            // If we do not have a dialog yet, create and show it
            if (m_MyForm == null || m_MyForm.IsDisposed)
            {
                // A new handler to handle request posting by the dialog
                ExternalEventExample handler = new ExternalEventExample();

                // External Event for the dialog to use (to post requests)
                ExternalEvent exEvent = ExternalEvent.Create(handler);
               
                // We give the objects to the new dialog;
                // The dialog becomes the owner responsible for disposing them, eventually.
              
               
                m_MyForm = new UserUIForm(commandData, exEvent, handler);
                m_MyForm.Show();
            }
        }

    }
}
