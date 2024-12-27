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
using System.Net;

namespace InitialAPP
{
    public partial class UserUIForm : System.Windows.Forms.Form
    {
        private UIApplication uiapp;
        private UIDocument uidoc;
        private Autodesk.Revit.ApplicationServices.Application app;
        private Autodesk.Revit.DB.Document doc;
        List<XYZ> InsertionPoints = new List<XYZ>();
        Dictionary<XYZ,XYZ> beamsPoints = new Dictionary<XYZ,XYZ>();
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
            CreateColumn(this.doc, new XYZ(0, 0, 0), 5000, "UC-Universal Columns-Column");
        }
        public void CreateColumn(Document doc, XYZ columnLocation, double columnHeight, string familyName)
        {
            //XYZ origin = new XYZ(0, 0, 0);

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
                firstColumn.Name = "UC356x368x129";

                foreach (var columnPoint in InsertionPoints)
                {
                    doc.Create.NewFamilyInstance(columnPoint, firstColumn, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
                }
                tx.Commit();
            }
        }

        public void CreateGrid()
        {

            var names = "ABCDEFJHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            using (Transaction tx = new Transaction(doc, "Create Grid"))
            {
                tx.Start();

                for (int i = 0; i < 61; i += 10)
                {

                    Line line = Line.CreateBound(new XYZ(i, -5, 0), new XYZ(i, 45, 0));

                    Grid grid = Grid.Create(doc, line);
                    grid.Name = $"{(i / 10) + 1}";
                }

                for (int i = 0; i < 41; i += 10)
                {
                    Line line = Line.CreateBound(new XYZ(-5, i, 0), new XYZ(65, i, 0));

                    Grid grid = Grid.Create(doc, line);
                    grid.Name = names[(i / 10)].ToString();

                }
                for (int i = 0; i < 61; i += 10)
                {
                    for (int j = 0; j < 41; j += 10)
                    {

                        XYZ point = new XYZ(i, j, 0);
                        if (InsertionPoints.Count() !=0 && point.X == InsertionPoints.LastOrDefault().X)
                        {
                            beamsPoints.Add(point, InsertionPoints.LastOrDefault());
                        }
                        InsertionPoints.Add(point);
                      
                    }
                }

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
            FilteredElementCollector beamsLevels = new FilteredElementCollector(doc)
             .WhereElementIsNotElementType()
             .OfCategory(BuiltInCategory.INVALID)
             .OfClass(typeof(Level));

            var allLevels = beamsLevels.Cast<Level>();

            var targetedLevel=allLevels.ToList().Where(l=>l.Elevation==3000).FirstOrDefault();


            // get a family symbol
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_StructuralFraming);

            List<FamilySymbol> Allbeams = collector.Cast<FamilySymbol>().ToList();

            StringBuilder stringBuilder = new StringBuilder();
            foreach (FamilySymbol familySymbol in Allbeams) { stringBuilder.Append(familySymbol.FamilyName); }
            MessageBox.Show(stringBuilder.ToString());

            var RequiredBeam = Allbeams.Where(e => e.FamilyName == "UB-Universal Beams").FirstOrDefault();
            if (RequiredBeam == null) { MessageBox.Show("the beam is null"); }

            // create new beam 10' long starting at origin
            //XYZ startPoint = new XYZ(0, 0, 0);
            //XYZ endPoint = new Autodesk.Revit.DB.XYZ(10, 0, 0);
            MessageBox.Show("the targeted beams count", this.beamsPoints.Count().ToString());
            using (Transaction tx = new Transaction(doc, "Create Beam"))
            {
                tx.Start();
                if (!RequiredBeam.IsActive)
                {
                    RequiredBeam.Activate();
                }
                RequiredBeam.Name = "UB305x165x40";

                //XYZ aa = new XYZ(beam.Key.X, beam.Key.Y, 3000);
                //XYZ bb = new XYZ(beam.Value.X, beam.Value.Y, 3000);
                foreach (var beam in beamsPoints)
                {
                    XYZ aa = new XYZ(beam.Key.X, beam.Key.Y, 3);
                    XYZ bb = new XYZ(beam.Value.X, beam.Value.Y, 3);
                    Autodesk.Revit.DB.Curve beamLine = Line.CreateBound(aa, bb);
                    // create a new beam
                    FamilyInstance instance = doc.Create.NewFamilyInstance(beamLine, RequiredBeam, targetedLevel, StructuralType.Beam);
                }

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
            // XYZ origin = new XYZ(0, 0, 0);

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
                foreach (var footingPoint in InsertionPoints)
                {
                    doc.Create.NewFamilyInstance(footingPoint, firstfooting, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Footing);
                }



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


            var floorTypeId = Floor.GetDefaultFloorType(doc, false);


            IList<CurveLoop> curves = new List<CurveLoop>();

            Line line1 = Line.CreateBound(new XYZ(-5, -5, firstLevel.Elevation), new XYZ(65, -5, firstLevel.Elevation));

            Line line2 = Line.CreateBound(new XYZ(65, -5, firstLevel.Elevation), new XYZ(65, 45, firstLevel.Elevation));

            Line line4 = Line.CreateBound(new XYZ(65, 45, firstLevel.Elevation), new XYZ(-5, 45, firstLevel.Elevation));

            Line line3 = Line.CreateBound(new XYZ(-5, 45, firstLevel.Elevation), new XYZ(-5, -5, firstLevel.Elevation));



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

        private void CreateRepars()
        {
            FilteredElementCollector rebars = new FilteredElementCollector(doc)
              .WhereElementIsNotElementType()
              .OfCategory(BuiltInCategory.OST_RebarShape)
              .OfClass(typeof(RebarShape));
            List<RebarShape> allRebars = rebars.Cast<RebarShape>().ToList();

            StringBuilder stringBuilder = new StringBuilder();

            foreach (RebarShape familySymbol in allRebars) 
            {
                stringBuilder.Append(familySymbol.Name);
            
            }
            MessageBox.Show(stringBuilder.ToString());


            // Start a transaction to modify the document
            using (Transaction trans = new Transaction(doc, "Create Rebar for Column"))
            {
                trans.Start();


                trans.Commit();
                
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            CreateRepars();
            
           
        }
        
    }
}
