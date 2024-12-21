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
    [TransactionAttribute(TransactionMode.Manual)]
    public class AddinClass : IExternalCommand
    { 

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            UserUIForm UserUIForm = new UserUIForm(commandData);
            UserUIForm.ShowDialog();
            return Result.Succeeded;
        }
    }
}
