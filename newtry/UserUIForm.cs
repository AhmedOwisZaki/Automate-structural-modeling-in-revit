using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Structure;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Text;
using Autodesk.Revit.Creation;
using Document = Autodesk.Revit.DB.Document;
using System.Xml.Linq;

namespace InitialAPP
{
    public partial class UserUIForm : System.Windows.Forms.Form
    {
        private UIApplication uiapp;
        private UIDocument uidoc;
        private Autodesk.Revit.ApplicationServices.Application app;
        private Autodesk.Revit.DB.Document doc;

        public UserUIForm(ExternalCommandData commandData)
        {

            uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument;
            app = uiapp.Application;
            doc = uidoc.Document;
            InitializeComponent();
        }

        private void CreateColumn(object sender, EventArgs e)
        {
            CreateColumn(this.doc, new XYZ(0, 0, 0), 5000, "M_Concrete-Rectangular-Column");
        }
        public void CreateColumn(Document doc, XYZ columnLocation, double columnHeight, string familyName)
        {
            XYZ origin = new XYZ(0, 0, 0);

            FilteredElementCollector collLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));

            Level firstLevel = collLevels.FirstElement() as Level;

            FilteredElementCollector allColumns = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilySymbol));

            var Columns = allColumns.Cast<FamilySymbol>();
            var firstColumn = Columns.Where(e => e.FamilyName == familyName).FirstOrDefault();


            using (Transaction tx = new Transaction(doc, "Create Column"))
            {
                tx.Start();
                if (!firstColumn.IsActive)
                {
                    firstColumn.Activate();
                }
                firstColumn.Name = "600 x 750mm";


                doc.Create.NewFamilyInstance(origin, firstColumn, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);

                tx.Commit();
            }
        }

        public void CreateGrid()
        {
            using (Transaction tx = new Transaction(doc, "Create Grid"))
            {
                tx.Start();
                Line line = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0));

                

               Grid grid= Grid.Create(doc, line);
                grid.Name = "a";
              
                tx.Commit();
            }
        }

        private void CreateGrid(object sender, EventArgs e)
        {
            CreateGrid();
        }

        private void CreateBeam() 
        {
            Autodesk.Revit.DB.View view = doc.ActiveView;
            FilteredElementCollector collLevels = new FilteredElementCollector(doc)
             .WhereElementIsNotElementType()
             .OfCategory(BuiltInCategory.INVALID)
             .OfClass(typeof(Level));

            Level firstLevel = collLevels.FirstElement() as Level;


            // get a family symbol
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_StructuralFraming);

            List<FamilySymbol> Allbeams = collector.Cast<FamilySymbol>().ToList();

            StringBuilder stringBuilder = new StringBuilder();
            foreach (FamilySymbol familySymbol in Allbeams) { stringBuilder.Append(familySymbol.FamilyName); }
                MessageBox.Show(stringBuilder.ToString());

            var RequiredBeam = Allbeams.Where(e => e.FamilyName == "M_Concrete-Rectangular Beam").FirstOrDefault();
            if (RequiredBeam == null) { MessageBox.Show("the beam is null"); }
            
            // create new beam 10' long starting at origin
            XYZ startPoint = new XYZ(0, 0, 0);
            XYZ endPoint = new Autodesk.Revit.DB.XYZ(10, 0, 0);
            using (Transaction tx = new Transaction(doc, "Create Beam"))
            {
                tx.Start();
                if (!RequiredBeam.IsActive)
                {
                    RequiredBeam.Activate();
                }
                RequiredBeam.Name = "400 x 800mm";
                Autodesk.Revit.DB.Curve beamLine = Line.CreateBound(startPoint, endPoint);

                // create a new beam
                FamilyInstance instance = doc.Create.NewFamilyInstance(beamLine, RequiredBeam, firstLevel, StructuralType.Beam);
                tx.Commit();
            }
            
        }

        private void CreateBeams(object sender, EventArgs e)
        {
            CreateBeam();
        }

        private void CreateFooting(object sender, EventArgs e)
        {
            CreateFootings("M_Footing-Rectangular");
        }
        private void CreateFootings(string familyName)
        {
            XYZ origin = new XYZ(0, 0, 0);

            FilteredElementCollector collLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));

            Level firstLevel = collLevels.FirstElement() as Level;

            FilteredElementCollector allfootings = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                .OfClass(typeof(FamilySymbol));

            var footings = allfootings.Cast<FamilySymbol>();
            var firstfooting = footings.Where(e => e.FamilyName == familyName).FirstOrDefault();


            using (Transaction tx = new Transaction(doc, "Create Footing"))
            {
                tx.Start();
                if (!firstfooting.IsActive)
                {
                    firstfooting.Activate();
                }
                firstfooting.Name = "1800 x 1200 x 450mm";


                doc.Create.NewFamilyInstance(origin, firstfooting, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Footing);

                tx.Commit();
            }
        }

        private void CreateFloor(object sender, EventArgs e)
        {

            FilteredElementCollector collLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));

            Level firstLevel = collLevels.ToList()[1] as Level;

            FilteredElementCollector allFloors = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfClass(typeof(FloorType));


            StringBuilder stringBuilder = new StringBuilder();
            foreach (FloorType familySymbol in allFloors) { stringBuilder.Append(familySymbol.FamilyName); }
            MessageBox.Show(stringBuilder.ToString());


            var floorTypeId=Floor.GetDefaultFloorType(doc, false);


            IList<CurveLoop> curves = new List<CurveLoop>();

            Line line1 = Line.CreateBound(new XYZ(-20, -20, firstLevel.Elevation), new XYZ(20, -20, firstLevel.Elevation));

            Line line2 = Line.CreateBound(new XYZ(20, -20, firstLevel.Elevation), new XYZ(20, 20, firstLevel.Elevation));

            Line line4 = Line.CreateBound(new XYZ(20, 20, firstLevel.Elevation), new XYZ(-20, 20, firstLevel.Elevation));

            Line line3 = Line.CreateBound(new XYZ(-20, 20, firstLevel.Elevation), new XYZ(-20, -20, firstLevel.Elevation));

           

            var a = new CurveLoop();
            a.Append(line1);
            a.Append(line2);
            a.Append(line4);
            a.Append(line3);
         

            var targetedFloor = allFloors.Cast<FloorType>().Where(ee => ee.FamilyName == "Floor").FirstOrDefault();
           
            using (Transaction tx = new Transaction(doc, "Create Floor"))
            {
              
               
                tx.Start();

                targetedFloor.Name = "Generic 500mm 2";
                Floor.Create(doc, new List<CurveLoop>() { a }, floorTypeId, firstLevel.Id);
                tx.Commit();
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {

        }
    }
}