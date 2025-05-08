using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;


namespace InitialAPP
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class AddinClass : IExternalCommand
    { 

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ExternalEventExample handler = new ExternalEventExample();

            // External Event for the dialog to use (to post requests)
            ExternalEvent exEvent = ExternalEvent.Create(handler);

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            UserUIForm UserUIForm = new UserUIForm(commandData , exEvent, handler);
            UserUIForm.ShowDialog();
            return Result.Succeeded;
        }
    }
}
