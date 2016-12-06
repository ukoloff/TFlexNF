using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TFlex;
using TFlex.Drawing;
using TFlex.Model;
using TFlex.Model.Model2D;

namespace TFlexNF
{
    class NFPolyline
    {
        public int count = 0;
        protected int length = 0;
        public double[] xr;
        public double[] yr;

        void Double()
        {
            length *= 2;
            Array.Resize<double>(ref xr, length);
            Array.Resize<double>(ref yr, length);
        }
        
        public void AddPoint(double x, double y)
        {
            count++;

            if (count >= length)
            {
                Double();
            }

            xr[count-1] = x;
            yr[count-1] = y;
        }

        public void Draw(Document Doc,Page p)
        {
            Array.Resize<double>(ref xr, count);
            Array.Resize<double>(ref yr, count);
            PolylineGeometry cpoly;

            unsafe
            {
                double* xs = stackalloc double[count+1];
                double* ys = stackalloc double[count+1];

                for (int i = 0; i < count; i++)
                {
                    xs[i] = xr[i];
                    ys[i] = yr[i];
                }

                cpoly = new PolylineGeometry(count, xs, ys);
                PolylineOutline nes = new PolylineOutline(Doc, cpoly);
                nes.Page = p;
            }


        }
            
            
                
        public NFPolyline()
        {
            xr = new double[2];
            yr = new double[2];
            length = 2;
        }
    }
    class NFResults
    {
        public static void Start()
        {
            StreamReader myStream;
            OpenFileDialog OpenDialog = new OpenFileDialog();
            Document Doc = TFlex.Application.ActiveDocument;

            OpenDialog.FilterIndex = 1;
            OpenDialog.Filter = "Nesting Factory results (*.nres)|*.nres";
            OpenDialog.RestoreDirectory = true;
            if (OpenDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = new StreamReader(OpenDialog.FileName)) != null)
                    {
                        using (myStream)
                        {
                            Doc.BeginChanges("Вывод геометрии");
                            Process(myStream);
                            myStream.Close();
                            Doc.EndChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Невозможно прочитать файл, подробнее: " + ex.Message);
                }
            }
        }

        protected static void Process(StreamReader filestream)
        {
            int i = 0;
            //filestream.ReadLine(); filestream.ReadLine();
            Document Doc = TFlex.Application.ActiveDocument;
            NFGetGeom.Doc = Doc;

            Page p = new Page(Doc);
            p.Name = "NFResult";
            p.FontStyle.Italic = true;

            int contour_count = 0;
            string line = "";
            string[] split;
            NFPolyline Poly = new NFPolyline();

            double y_offset = 0;
            


            do
            {
                line = filestream.ReadLine();
                split = line.Split(' ');
                bool processing = false;
                double first_x = -1.337;
                double first_y = -1.337;

                if (line != null && split.Length == 2)
                {
                    processing = true;
                    contour_count++;
                    NFGetGeom.Msg("Contour: " + contour_count);

                    do
                    {
                        line = filestream.ReadLine();
                        split = line.Split(' ');

                        if (split.Length > 1)
                        {
                            if (split.Length == 3)
                            {
                                int bulge = 0;
                                Int32.TryParse(split[2].ToString(), out bulge);
                                NFGetGeom.Msg(line);
                               
                                double x = 0, y = 0;
                                Double.TryParse(split[0].ToString().Replace('.', ','), out x);
                                Double.TryParse(split[1].ToString().Replace('.', ','), out y);

                                if (contour_count == 1)
                                {
                                    y_offset = Math.Max(y_offset, y);
                                } else
                                {
                                    y = y_offset - y;
                                }

                                if (first_x == -1.337)
                                {
                                    first_x = x;
                                    first_y = y;
                                }
                                Poly.AddPoint(x, y);
                                

                                
                            }

                            if (split.Length == 5)
                            {

                                NFGetGeom.Msg("ARC: " + line);
                                double x = 0, y = 0, radius = 0, ang1 = 0, ang2 = 0;
                                Double.TryParse(split[0].ToString().Replace('.', ','), out x);
                                Double.TryParse(split[1].ToString().Replace('.', ','), out y);
                                Double.TryParse(split[2].ToString().Replace('.', ','), out radius);
                                Double.TryParse(split[3].ToString().Replace('.', ','), out ang1);
                                Double.TryParse(split[4].ToString().Replace('.', ','), out ang2);
                                radius = radius / 2;

                                double x1 = Math.Cos(-ang1 / 180 * Math.PI) * radius + x + radius;
                                double y1 = Math.Sin(-ang1 / 180 * Math.PI) * radius + y + radius;

                                double x2 = Math.Cos((-ang1 - ang2/2) / 180 * Math.PI) * radius + x + radius;
                                double y2 = Math.Sin((-ang1 - ang2/2) / 180 * Math.PI) * radius + y + radius;

                                double x3 = Math.Cos((-ang1 - ang2) / 180 * Math.PI) * radius + x + radius;
                                double y3 = Math.Sin((-ang1 - ang2) / 180 * Math.PI) * radius + y + radius;

                                y1 = y_offset - y1;
                                y2 = y_offset - y2;
                                y3 = y_offset - y3;

                                FreeNode fn1 = new FreeNode(Doc, x1, y1);
                                FreeNode fn2 = new FreeNode(Doc, x2, y2);
                                FreeNode fn3 = new FreeNode(Doc, x3, y3);

                                if (first_x == -1.337)
                                {
                                    first_x = x1;
                                    first_y = y1;
                                }

                                if (Poly.count > 0)
                                {
                                    Poly.AddPoint(x1, y1);
                                    Poly.Draw(Doc, p);
                                    Poly = new NFPolyline();
                                    
                                }
                                Poly.AddPoint(x3, y3);
                                ThreePointArcOutline Arc = new ThreePointArcOutline(Doc, fn1, fn2, fn3);
                                Arc.Page = p;

                            }
                        }
                    } while (line != null & split.Length > 1) ;

                }

                if (processing & Poly.count > 0)
                {
                    NFGetGeom.Msg("INIT POLYLINE");
                    Poly.AddPoint(first_x, first_y);
                    Poly.Draw(Doc,p);
                    Poly = new NFPolyline();
                }
                if (filestream.EndOfStream)
                {
                    break;
                }
            } while (true);
        }
    }

    /*class NFResults
    {
        static Document Doc;
        static Page p;
        static double ListX = 300;
        static double ListY = 300;

        protected static void Rotate(double a,ref double x, ref double y, double axis_x, double axis_y)
        {
            a = a / 180 * Math.PI;
            double x1 = Math.Cos(a) * (x - axis_x) - Math.Sin(a) * (y - axis_y) + axis_x;
            double y1 = Math.Cos(a) * (y - axis_y) + Math.Sin(a) * (x - axis_x) + axis_y;

            x = x1;
            y = y1;
        }

        protected static void DrawLine(double x1, double y1, double x2, double y2)
        {
            FreeNode fn1 = new FreeNode(Doc, x1, y1);
            FreeNode fn2 = new FreeNode(Doc, x2, y2);
            ConstructionOutline conOutline = new ConstructionOutline(Doc, fn1, fn2);
            conOutline.Page = p;
        }

        protected static void DrawCircle(double cx, double cy, double radius)
        {
            FreeNode fn1 = new FreeNode(Doc, cx, cy);
            CircleOutline co = new CircleOutline(Doc, fn1, radius);
        }

        protected static void DrawCircleArc(double xb, double yb, double xc, double yc, double xe, double ye)
        {
            FreeNode fn1 = new FreeNode(Doc, xb, yb);
            FreeNode fn2 = new FreeNode(Doc, xc, yc);
            FreeNode fn3 = new FreeNode(Doc, xe, ye);

            ThreePointArcOutline threePointArcOutline = new ThreePointArcOutline(Doc, fn1, fn2, fn3);
            threePointArcOutline.Page = p;

        }
        protected static void DrawItem(Area example, double x0, double y0, bool reflected, double rotation)
        {
            Rectangle bound_box = example.BoundRect;

            double width = bound_box.Width;
            double height = bound_box.Height;
            double cx = 0;
            double cy = 0;
            double top = bound_box.Top;
            double left = bound_box.Left;

            new FreeNode(Doc, cx, cy);

            for (int num_contour = 0; num_contour < example.ContourCount; num_contour++)
            {
                Contour contour = example.GetContour(num_contour);
                for (int num_segment = 0; num_segment < contour.SegmentCount; num_segment++)
                {
                    ContourSegment csegment = contour.GetSegment(num_segment);

                    switch (csegment.GeometryType)
                    {
                        case ObjectGeometryType.Circle:
                            CircleGeometry ccirc = csegment.Geometry as CircleGeometry;
                            double center_x = ccirc.CenterX - left;
                            double center_y = top - ccirc.CenterY;

                            if (reflected)
                            {
                                center_x = width - center_x;
                            }

                            if (rotation != 0)
                            {
                                Rotate(rotation, ref center_x, ref center_y, cx, cy);
                            }

                            DrawCircle(center_x + x0, center_y + y0, ccirc.Radius);

                            break;

                        case ObjectGeometryType.Line:

                            LineGeometry cline = csegment.Geometry as LineGeometry;
                            double x1 = cline.X1 - left; double x2 = cline.X2 - left; double y1 = top - cline.Y1; double y2 = top - cline.Y2;

                            if (reflected)
                            {
                                x1 = width - x1;
                                x2 = width - x2;
                            }

                            if (rotation != 0)
                            {
                                Rotate(rotation, ref x1, ref y1, cx, cy);
                                Rotate(rotation, ref x2, ref y2, cx, cy);
                            }

                            DrawLine(x1+x0, y1 + y0, x2 + x0, y2 + y0);

                            break;
                        case ObjectGeometryType.CircleArc:

                            CircleArcGeometry carc = csegment.Geometry as CircleArcGeometry;
                            double xb = 0, yb = 0, xc = 0, yc = 0, xe = 0, ye = 0;
                            carc.GetThreePoints(ref xb, ref yb, ref xc, ref yc, ref xe, ref ye);

                            xb = xb - left;
                            xc = xc - left;
                            xe = xe - left;
                            yb = top - yb;
                            yc = top - yc;
                            ye = top - ye;

                            if (reflected)
                            {
                                xb = width - xb;
                                xe = width - xe;
                                xc = width - xc;
                            }

                            if (rotation != 0)
                            {
                                Rotate(rotation, ref xb, ref yb, cx, cy);
                                Rotate(rotation, ref xc, ref yc, cx, cy);
                                Rotate(rotation, ref xe, ref ye, cx, cy);
                            }

                            DrawCircleArc(xb + x0, yb + y0, xc + x0, yc + y0, xe + x0, ye + y0);

                            break;

                        default:

                            PolylineGeometry cpoly = csegment.Geometry as PolylineGeometry;
                            int num_p = cpoly.Count;

                            unsafe
                            {
                                double* xs = stackalloc double[num_p];
                                double* ys = stackalloc double[num_p];

                                for (int v = 0; v < num_p; v++)
                                {
                                    double xp = cpoly.GetX(v) - left;
                                    double yp = top - cpoly.GetY(v);
                                    

                                    if (reflected)
                                    {
                                        xp = width - xp;
                                    }

                                    if (rotation != 0)
                                    {
                                        Rotate(rotation, ref xp, ref yp, cx, cy);
                                    }
                                    xs[v] = xp + x0;
                                    ys[v] = yp + y0;
                                }
                            
                                cpoly = new PolylineGeometry(num_p, xs,ys);
                            }

                            PolylineOutline nes = new PolylineOutline(Doc, cpoly);
                            nes.Page = p;

                            break;
                    }
                }
            }
        }
        public static void ReadFile()
        {
            /*
            p = new Page(Doc);
            p.Name = "NFResult";
            p.FontStyle.Italic = true;

            ICollection<Area> EO = Doc.GetAreas();
            IEnumerator<Area> GeomEnum = EO.GetEnumerator();
            GeomEnum.MoveNext();
            GeomEnum.MoveNext();

            DrawItem(GeomEnum.Current as Area, 0, 0, true, 0);
            DrawItem(GeomEnum.Current as Area, 0, 0, true, 45);
            DrawItem(GeomEnum.Current as Area, 0, 0, true, 90);
            



            string fileName = Path.GetTempPath() + "result.nfres";

            Doc = TFlex.Application.ActiveDocument;
            Doc.BeginChanges("Отображение результата раскроя");

            p = new Page(Doc);
            p.Name = "NFResult";//название страницы
            p.FontStyle.Italic = true;//устновка стиля шрифта - наклонный

            StreamReader resultfile = new StreamReader(fileName);
            string line;
            int counter = 0;
            while ((line = resultfile.ReadLine()) != null)
            {
                line = line.Replace(".", ",");
                string[] param = line.Split(new Char[] { '=', ' ' });
                string name = param[1];
                bool refl = (param[3] == "1" ? true : false);
                double rotation = 0;
                Double.TryParse(param[5], out rotation);
                double dx = 0;
                Double.TryParse(param[7], out dx);
                double dy = 0;
                Double.TryParse(param[9], out dy);

                Area item = Doc.GetObjectByName(name) as Area;

                DrawItem(item, dx, dy, refl, rotation);
                counter++;
            }
            Doc.EndChanges();


        }
    }*/
}
