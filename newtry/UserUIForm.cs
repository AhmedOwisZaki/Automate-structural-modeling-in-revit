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
using Document = Autodesk.Revit.DB.Document;

using Transaction = Autodesk.Revit.DB.Transaction;
using Grid = Autodesk.Revit.DB.Grid;

namespace InitialAPP
{

    public partial class UserUIForm : System.Windows.Forms.Form
    {
        private ExternalEvent m_ExEvent;
        private ExternalEventExample m_Handler;
        private UIApplication uiapp;
        private UIDocument uidoc;
        private Autodesk.Revit.ApplicationServices.Application app;
        private Autodesk.Revit.DB.Document doc;
        List<XYZ> InsertionPoints = new List<XYZ>();
        Dictionary<XYZ,XYZ> beamsPoints = new Dictionary<XYZ,XYZ>();
        Dictionary<XYZ, XYZ> purlinsPoints = new Dictionary<XYZ, XYZ>();
        private Floor createdFloor ;
        XYZ mainBracingBottomPlateMiddlePoint = new XYZ();
        public UserUIForm(ExternalCommandData commandData, ExternalEvent exEvent, ExternalEventExample handler)
        {

            uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument;
            app = uiapp.Application;
            doc = uidoc.Document;
            InitializeComponent();
            m_ExEvent = exEvent;
            m_Handler = handler;
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
                    var refactoredPoint = GetFactoredPoint(columnPoint);
                    doc.Create.NewFamilyInstance(refactoredPoint, firstColumn, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
                }
                tx.Commit();
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // we own both the event and the handler
            // we should dispose it before we are closed
            m_ExEvent.Dispose();
            m_ExEvent = null;
            m_Handler = null;

            // do not forget to call the base class
            base.OnFormClosed(e);
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void showMessageButton_Click(object sender, EventArgs e)
        {
            m_ExEvent.Raise();
        }

        public void CreateGrid()
        {

            var names = "ABCDEFJHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            using (Transaction tx = new Transaction(doc, "Create Grid"))
            {
                tx.Start();

                for (int i = 0; i < 61; i += 10)
                {

                    Line line = Line.CreateBound(GetFactoredPoint(new XYZ(i, -5, 0)), GetFactoredPoint(new XYZ(i, 45, 0)));

                    Grid grid = Grid.Create(doc, line);
                    grid.Name = $"{(i / 10) + 1}";
                }

                for (int i = 0; i < 41; i += 10)
                {
                    Line line = Line.CreateBound(GetFactoredPoint(new XYZ(-5, i, 0)), GetFactoredPoint(new XYZ(65, i, 0)));

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
                        if (InsertionPoints.Count() > 4 && point.Y == InsertionPoints[InsertionPoints.Count()-5].Y)
                        {
                            purlinsPoints.Add(point, InsertionPoints[InsertionPoints.Count() - 5]);
                            if (j != 40)
                            {
                                purlinsPoints.Add(new XYZ(point.X, point.Y + 3, point.Z), new XYZ(InsertionPoints[InsertionPoints.Count() - 5].X, InsertionPoints[InsertionPoints.Count() - 5].Y + 3, InsertionPoints[InsertionPoints.Count() - 5].Z));
                                purlinsPoints.Add(new XYZ(point.X, point.Y + 7, point.Z), new XYZ(InsertionPoints[InsertionPoints.Count() - 5].X, InsertionPoints[InsertionPoints.Count() - 5].Y + 7, InsertionPoints[InsertionPoints.Count() - 5].Z));

                            }
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
           // MessageBox.Show(stringBuilder.ToString());

            var RequiredBeam = Allbeams.Where(e => e.FamilyName == "UB-Universal Beams").FirstOrDefault();
            if (RequiredBeam == null) { MessageBox.Show("the beam is null"); }

            // create new beam 10' long starting at origin
            //XYZ startPoint = new XYZ(0, 0, 0);
            //XYZ endPoint = new Autodesk.Revit.DB.XYZ(10, 0, 0);
           // MessageBox.Show("the targeted beams count", this.beamsPoints.Count().ToString());
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
                    XYZ aa = GetFactoredPoint(new XYZ(beam.Key.X, beam.Key.Y, 3));
                    XYZ bb = GetFactoredPoint(new XYZ(beam.Value.X, beam.Value.Y, 3));
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

        private void CreatePurlins() 
        {
            Autodesk.Revit.DB.View view = doc.ActiveView;
            FilteredElementCollector beamsLevels = new FilteredElementCollector(doc)
             .WhereElementIsNotElementType()
             .OfCategory(BuiltInCategory.INVALID)
             .OfClass(typeof(Level));

            var allLevels = beamsLevels.Cast<Level>();

            var targetedLevel = allLevels.ToList().Where(l => l.Elevation == 3000).FirstOrDefault();


            // get a family symbol
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_StructuralFraming);

            List<FamilySymbol> Allbeams = collector.Cast<FamilySymbol>().ToList();

            StringBuilder stringBuilder = new StringBuilder();
            foreach (FamilySymbol familySymbol in Allbeams) { stringBuilder.Append(familySymbol.FamilyName); }
            // MessageBox.Show(stringBuilder.ToString());

            var RequiredBeam = Allbeams.Where(e => e.FamilyName == "UB-Universal Beams").FirstOrDefault();
            if (RequiredBeam == null) { MessageBox.Show("the beam is null"); }

            // create new beam 10' long starting at origin
            //XYZ startPoint = new XYZ(0, 0, 0);
            //XYZ endPoint = new Autodesk.Revit.DB.XYZ(10, 0, 0);
            // MessageBox.Show("the targeted beams count", this.beamsPoints.Count().ToString());
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
                foreach (var beam in purlinsPoints)
                {
                    XYZ aa = GetFactoredPoint(new XYZ(beam.Key.X, beam.Key.Y, 3.305));
                    XYZ bb = GetFactoredPoint(new XYZ(beam.Value.X, beam.Value.Y, 3.305));
                    Autodesk.Revit.DB.Curve beamLine = Line.CreateBound(aa, bb);
                    // create a new beam
                    FamilyInstance instance = doc.Create.NewFamilyInstance(beamLine, RequiredBeam, targetedLevel, StructuralType.Beam);
                }

                tx.Commit();
            }

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
                    var refactoredPoint=GetFactoredPoint(footingPoint);
                    doc.Create.NewFamilyInstance(refactoredPoint, firstfooting, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Footing);
                }



                tx.Commit();
            }
        }

        private void CreateFloor(object sender, EventArgs e)
        {

            //FilteredElementCollector collLevels = new FilteredElementCollector(doc)
            //    .WhereElementIsNotElementType()
            //    .OfCategory(BuiltInCategory.INVALID)
            //    .OfClass(typeof(Level));

            
            FilteredElementCollector allFloors = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfClass(typeof(FloorType));


            StringBuilder stringBuilder = new StringBuilder();
            //foreach (FloorType familySymbol in allFloors) { stringBuilder.Append(familySymbol.FamilyName); }
            //MessageBox.Show(stringBuilder.ToString());


            var floorTypeId = Floor.GetDefaultFloorType(doc, false);


            IList<CurveLoop> curves = new List<CurveLoop>();

            Line line1 = Line.CreateBound(GetFactoredPoint(new XYZ(-1, -1, 3.305 * (10000 / 3048))), GetFactoredPoint(new XYZ(61, -1, 3.305*(10000/3048))));

            Line line2 = Line.CreateBound(GetFactoredPoint(new XYZ(61, -1, 3.305 * (10000 / 3048))), GetFactoredPoint(new XYZ(61, 41, 3.305 * (10000 / 3048))));

            Line line4 = Line.CreateBound(GetFactoredPoint(new XYZ(61, 41, 3.305 * (10000 / 3048))), GetFactoredPoint(new XYZ(-1, 41, 3.305 * (10000 / 3048))));

            Line line3 = Line.CreateBound(GetFactoredPoint(new XYZ(-1, 41, 3.305 * (10000 / 3048))), GetFactoredPoint(new XYZ(-1, -1, 3.305 * (10000 / 3048))));

          

            var a = new CurveLoop();
            a.Append(line1);
            a.Append(line2);
            a.Append(line4);
            a.Append(line3);



            var targetedFloor = allFloors.Cast<FloorType>().Where(ee => ee.FamilyName == "Floor").FirstOrDefault();

            using (Transaction tx = new Transaction(doc, "Create Floor"))
            {
                

                tx.Start();
                Level firstLevel = Level.Create(doc, 3.915d * (10000 / 3048)); 
                firstLevel.Name = "Level 3";
                targetedFloor.Name = "75mm Metal Roof Deck";
                this.createdFloor =Floor.Create(doc, new List<CurveLoop>() { a }, floorTypeId, firstLevel.Id);
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

        private void  CreateCornerConnection()
        {

            List<ElementId> selectedIds = uidoc.Selection.GetElementIds().ToList();
      

            if (selectedIds.Count() > 0)
            {
                // Start a new transaction.
                using (Transaction tran = new Transaction(doc, "Create generic structural connection"))
                {
                    tran.Start();

                    StructuralConnectionHandler conn = StructuralConnectionHandler.CreateGenericConnection(doc, selectedIds);

                    tran.Commit();
                
                }
            }

        }
        private void structuralFraming(object sender, EventArgs e)
        {

            CreateDimensions();

        }


        private XYZ GetFactoredPoint(XYZ point) 
        {
            XYZ refactoredPoint = new XYZ(point.X * (10000d / 3048d), point.Y * (10000d / 3048d), point.Z * (10000d / 3048d)); //from meter to feet
            return refactoredPoint;        }


        private void button7_Click(object sender, EventArgs e)
        {
            CreateCornerConnection();
       
         

        }

        private void button8_Click(object sender, EventArgs e)
        {
            CreatePurlins();
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("Ahmed Owis Zaki", "The developer");
        }

        private void CreateFloorOpen(object sender, EventArgs e)
        {

            Line line11 = Line.CreateBound(GetFactoredPoint(new XYZ(10, 10, 3.305)), GetFactoredPoint(new XYZ(20, 10, 3.305)));

            Line line22 = Line.CreateBound(GetFactoredPoint(new XYZ(20, 10, 3.305)), GetFactoredPoint(new XYZ(20, 20, 3.305)));

            Line line33 = Line.CreateBound(GetFactoredPoint(new XYZ(20, 20, 3.305)), GetFactoredPoint(new XYZ(10, 20, 3.305)));

            Line line44 = Line.CreateBound(GetFactoredPoint(new XYZ(10, 20, 3.305)), GetFactoredPoint(new XYZ(10, 10, 3.305)));

            CurveArray curveArray = new CurveArray();
            curveArray.Append(line11);
            curveArray.Append(line22);
            curveArray.Append(line33);
            curveArray.Append(line44);
            using (Transaction tx = new Transaction(doc, "Create Floor"))
            {
                //create the floor open
                tx.Start();
                doc.Create.NewOpening(createdFloor as Element, curveArray, false);
                tx.Commit();
            }
        }
   

        private void CreateModel(object sender, EventArgs e)
        {
            List<ElementId> selectedIds = uidoc.Selection.GetElementIds().ToList();
            //MessageBox.Show(JsonConvert.SerializeObject(selectedIds));
            List<Line> lines = new List<Line>();
            foreach (ElementId elementId in selectedIds)
            {
                XYZ insertionPoint;
                XYZ spanPoint;
                using (Transaction tx = new Transaction(doc, "Create model"))
                {
                    tx.Start();
                    FilteredElementCollector line = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Lines);
                    var selectedLine = line.Cast<ModelLine>();

                    //MessageBox.Show(JsonConvert.SerializeObject(selectedLine));
                    //textBox1.Text = JsonConvert.SerializeObject(selectedLine);
                    BoundingBoxXYZ boundingBox =selectedLine.Where(l=>l.Id == elementId).FirstOrDefault().get_BoundingBox(doc.ActiveView);

                   // MessageBox.Show(JsonConvert.SerializeObject(boundingBox.));

                    // Finally, we are able to PickPoint()
                    MessageBox.Show("Min point is "+JsonConvert.SerializeObject(boundingBox.Min));
                    MessageBox.Show("max  point is " + JsonConvert.SerializeObject(boundingBox.Max));
                    insertionPoint = boundingBox.Min; //uidoc.Selection.PickPoint();//new XYZ(0, 0, 0);//uidoc.Selection.PickPoint("Pick a first point!");
                    spanPoint = boundingBox.Max;//new XYZ(10.23, 0, 0);//uidoc.Selection.PickPoint("Pick a second point!");//10.57


                    tx.Commit();
                }
                double height = 11.486;//11.5
                double beamDepth = 1.0208333;

                CreateModelColumns(insertionPoint, spanPoint, height);
                CreateModelBeam(insertionPoint, spanPoint, height);
                CreateModelBracing(insertionPoint, spanPoint, height, beamDepth);
                //CreatMainBracingBottomPlate(insertionPoint, spanPoint, mainBracingBottomPlateMiddlePoint);
            }



        }
        private void CreatMainBracingBottomPlate(XYZ insertionPoint, XYZ spanPoint, XYZ mainBracingBottomPlateMiddlePoint) 
        {
            XYZ p1 = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(insertionPoint, spanPoint, .69270833);
            XYZ p2 = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(insertionPoint, spanPoint,(1d/6d));
            XYZ p3 = new XYZ(p2.X ,p2.Y, p2.Z +1.207956);
            XYZ p4 = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(p3 ,new XYZ(spanPoint.X,spanPoint.Y, spanPoint.Z+ 1.207956) , .4455538);
            var width = 2 * GetDistance(p4,mainBracingBottomPlateMiddlePoint);
            XYZ p5 = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(p4,mainBracingBottomPlateMiddlePoint,width);





        }
        private void CreateModelBracing(XYZ insertionPoint, XYZ spanPoint, double height , double beamDepth) 
        {

            FilteredElementCollector collLevels = new FilteredElementCollector(doc)
              .WhereElementIsNotElementType()
              .OfCategory(BuiltInCategory.INVALID)
              .OfClass(typeof(Level));

            Level firstLevel = collLevels.Cast<Level>().Where(l=>l.Name.Contains("Level 2")).FirstOrDefault();



            FilteredElementCollector allColumns = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilySymbol));

            var Columns = allColumns.Cast<FamilySymbol>();
            var firstColumn = Columns.Where(ee => ee.FamilyName == "HSS-Hollow Structural Section-Column").FirstOrDefault();

            //first bracing
            var newInsertionPoint = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(insertionPoint, new XYZ(spanPoint.X, spanPoint.Y, height -.5 * beamDepth),1.4068);
            //MessageBox.Show(JsonConvert.SerializeObject( newInsertionPoint));
            var targetPoint = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(new XYZ(spanPoint.X,spanPoint.Y, height - .5*beamDepth), insertionPoint,1.8569);
            //MessageBox.Show(JsonConvert.SerializeObject(targetPoint));
            var firstColumnLine = Line.CreateBound(newInsertionPoint, targetPoint);

            mainBracingBottomPlateMiddlePoint = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(newInsertionPoint, targetPoint ,-.1041666666666);

            //bottom first half bracing
            var newInsertionPoint1 = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(spanPoint, new XYZ(insertionPoint.X, insertionPoint.Y, height - .5 * beamDepth), 1.4068);
            //MessageBox.Show(JsonConvert.SerializeObject(newInsertionPoint1));
            var middlePoint = GetMidllePoint(spanPoint, new XYZ(insertionPoint.X, insertionPoint.Y, height- .5 * beamDepth));
            var targetPoint1 = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(middlePoint, newInsertionPoint1,.87675);
           // MessageBox.Show(JsonConvert.SerializeObject(targetPoint1));
            var firstHalfBracingLine = Line.CreateBound(newInsertionPoint1, targetPoint1);

            //top second half bracing
            var newInsertionPoint2 = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(middlePoint, targetPoint1, -.87675);
           // MessageBox.Show(JsonConvert.SerializeObject(newInsertionPoint2));
            var targetPoint2 = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint( new XYZ(insertionPoint.X, insertionPoint.Y, height- .5 * beamDepth),middlePoint, 1.8569);
           // MessageBox.Show(JsonConvert.SerializeObject(targetPoint2));
            var secondBracingHalfLine= Line.CreateBound(newInsertionPoint2, targetPoint2);

            using (Transaction tx = new Transaction(doc, "Create Column"))
            {
                tx.Start();
                if (!firstColumn.IsActive)
                {
                    firstColumn.Activate();
                }
                firstColumn.Name = "HSS4X4X.250";

                doc.Create.NewFamilyInstance(firstColumnLine, firstColumn, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
                doc.Create.NewFamilyInstance(firstHalfBracingLine, firstColumn, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
                doc.Create.NewFamilyInstance(secondBracingHalfLine, firstColumn, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
                tx.Commit();
            }
        }
        private void CreateModelColumns(XYZ insertionPoint , XYZ spanPoint , double height) 
        {
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
            var firstColumn = Columns.Where(ee => ee.FamilyName == "HSS-Hollow Structural Section-Column").FirstOrDefault();

            var firstColumnLine = Line.CreateBound(insertionPoint, new XYZ(insertionPoint.X, insertionPoint.Y, insertionPoint.Z + height));
            var secondColumnLine = Line.CreateBound(spanPoint, new XYZ(spanPoint.X, spanPoint.Y, spanPoint.Z + height));




            using (Transaction tx = new Transaction(doc, "Create Column"))
            {
                tx.Start();
                if (!firstColumn.IsActive)
                {
                    firstColumn.Activate();
                }
                firstColumn.Name = "HSS4X4X.250";

                doc.Create.NewFamilyInstance(firstColumnLine, firstColumn, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
                doc.Create.NewFamilyInstance(secondColumnLine, firstColumn, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
                tx.Commit();
            }


        }
        private void CreateModelBeam(XYZ insertionPoint, XYZ spanPoint, double height)
        {

            Autodesk.Revit.DB.View view = doc.ActiveView;
            FilteredElementCollector beamsLevels = new FilteredElementCollector(doc)
             .WhereElementIsNotElementType()
             .OfCategory(BuiltInCategory.INVALID)
             .OfClass(typeof(Level));

            var allLevels = beamsLevels.Cast<Level>();

            var targetedLevel = allLevels.ToList().Where(l => l.Elevation == 3000).FirstOrDefault();


            // get a family symbol
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_StructuralFraming);

            List<FamilySymbol> Allbeams = collector.Cast<FamilySymbol>().ToList();

            StringBuilder stringBuilder = new StringBuilder();
            foreach (FamilySymbol familySymbol in Allbeams) { stringBuilder.Append(familySymbol.FamilyName); }
            // MessageBox.Show(stringBuilder.ToString());

            var RequiredBeam = Allbeams.Where(e => e.FamilyName == "W-Wide Flange").FirstOrDefault();
            if (RequiredBeam == null) { MessageBox.Show("the beam is null"); }

            // create new beam 10' long starting at origin
            //XYZ startPoint = new XYZ(0, 0, 0);
            //XYZ endPoint = new Autodesk.Revit.DB.XYZ(10, 0, 0);
            // MessageBox.Show("the targeted beams count", this.beamsPoints.Count().ToString());
            using (Transaction tx = new Transaction(doc, "Create Beam"))
            {
                tx.Start();
                if (!RequiredBeam.IsActive)
                {
                    RequiredBeam.Activate();
                }
                RequiredBeam.Name = "W12X26";

                //XYZ aa = new XYZ(beam.Key.X, beam.Key.Y, 3000);
                //XYZ bb = new XYZ(beam.Value.X, beam.Value.Y, 3000);
                var newBeamInsertionPoint = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(insertionPoint,spanPoint,(1d/6d));
                    XYZ aa = new XYZ(newBeamInsertionPoint.X, newBeamInsertionPoint.Y, height);

                var newBeamEndPoint = GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(spanPoint,insertionPoint, (1d / 6d));
                    XYZ bb = new XYZ(newBeamEndPoint.X, newBeamEndPoint.Y, height);
                    Autodesk.Revit.DB.Line beamLine = Line.CreateBound(aa, bb);

               
                    // create a new beam
                    FamilyInstance instance = doc.Create.NewFamilyInstance(beamLine, RequiredBeam, targetedLevel, StructuralType.Beam);
                
                tx.Commit();
            }

        }
        private XYZ GetAPointAtASpecificDistanceFromAnotherPointWithADirectionPoint(XYZ origin , XYZ directionPoint , double distance)
        {
            var totalDistance = GetDistance(origin, directionPoint);

            var ratio = distance / totalDistance; 

            return new XYZ(origin.X+ratio*(directionPoint.X-origin.X) , origin.Y+ ratio * (directionPoint.Y - origin.Y), origin.Z + ratio * (directionPoint.Z - origin.Z));
        }
        private double GetDistance(XYZ p1, XYZ p2)
        {
           var square= ((p1.X - p2.X) * (p1.X - p2.X)) + ((p1.Y - p2.Y) *(p1.Y - p2.Y)) + ((p1.Z - p2.Z)* (p1.Z - p2.Z));
            return Math.Sqrt(square);
        }
        private XYZ GetMidllePoint(XYZ p1 , XYZ p2) 
        {
            return new XYZ(.5 * (p1.X + p2.X), .5 * (p1.Y + p2.Y), .5 * (p1.Z + p2.Z));
        }
        private void button10_Click(object sender, EventArgs e)
        {


            
        }
        private void CreateDimensions() 
        {
            try
            {
                // Prompt the user to select the first point
                XYZ firstPoint = uidoc.Selection.PickPoint("Please pick the first point");
                TaskDialog.Show("Point 1", $"First Point: {FormatXYZ(firstPoint)}");

                // Prompt the user to select the second point
                XYZ secondPoint = uidoc.Selection.PickPoint("Please pick the second point");
                TaskDialog.Show("Point 2", $"Second Point: {FormatXYZ(secondPoint)}");

                // Create a transaction to add a dimension
                using (Transaction tx = new Transaction(doc, "Add Dimension"))
                {
                    tx.Start();

                    // Get the active view
                    var view = doc.ActiveView;

                    // Create a reference plane between the points
                    Line dimensionLine = Line.CreateBound(firstPoint, secondPoint);

                    // Create references for dimension
                    ReferenceArray refArray = new ReferenceArray();

                    // Add points as references
                    Reference ref1 = new Reference(doc.ActiveView);
                    Reference ref2 = new Reference(doc.ActiveView);

                    // Note: These references are placeholders.
                    // You may need to use real element references if dimensioning objects.
                    refArray.Append(ref1);
                    refArray.Append(ref2);

                    // Create a dimension between the points
                    Dimension dimension = doc.Create.NewDimension(view, dimensionLine, refArray);

                    tx.Commit();
                }

                MessageBox.Show( Result.Succeeded.ToString());
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Handle the case where the user cancels the operation
                MessageBox.Show("Operation canceled by user.");
           
            }
            catch (Exception ex)
            {
                // Handle other unexpected exceptions
                var message = ex.Message;
                MessageBox.Show(message);
            }

        }
        private string FormatXYZ(XYZ point)
        {
            return $"X: {point.X:F3}, Y: {point.Y:F3}, Z: {point.Z:F3}";
        }
    }
    public class ExternalEventExample : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            TaskDialog.Show("External Event", "Click Close to close.");
        }

        public string GetName()
        {
            return "External Event Example";
        }
    }
}
