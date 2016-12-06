using System;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections.Generic;

using TFlex;
using TFlex.Model;
using TFlex.Model.Model2D;
using TFlex.Command;
using TFlex.Drawing;


namespace TFlexNF
{
    class NFGetGeom
    {
        
        public static Document Doc;
        public static void Msg(string M)
        {
            if (Doc != null)
            {
                Doc.Diagnostics.Add(new DiagnosticsMessage(DiagnosticsMessageType.Information, M));
            }
        }

        //protected static void cArcToDoubles(CircleArcGeometry cgeom, ref Stack<double> VertStack)
        protected static void cArcToDoubles(CircleArcGeometry cgeom, ref NFContour cont,Rectangle b,bool ccw)
        {
            double sx = cgeom.StartX;
            double sy = cgeom.StartY;

            double ex = cgeom.EndX;
            double ey = cgeom.EndY;

            /*double R = cgeom.Radius;
            double L = 0.5 * Math.Sqrt((ex - sx) * (ex - sx) + (ey - sy) * (ey - sy));
            double bulge = 0;
            if (L > 0)
            {
                bulge = (R - Math.Sqrt(R * R - L * L)) / L​;
            }*/

            double[] angs = getArcAngle(cgeom, ccw);
            double bulge = Math.Tan(angs[0] / 4);

            if (ccw)
            {
                bulge = -bulge;
            }
            if (ccw)
            {
                cont.AddPoint(new NFPoint(sx - b.Left, b.Top - sy, bulge));
                cont.AddPoint(new NFPoint(ex - b.Left, b.Top - ey, bulge));
            }
            else
            {
                cont.AddPoint(new NFPoint(ex - b.Left, b.Top - ey, bulge));
                cont.AddPoint(new NFPoint(sx - b.Left, b.Top - sy, bulge));
            }

        }

        public static double[] getArcAngle(CircleArcGeometry arc, bool ccw)
        {
            double xb = 0, yb = 0, xm = 0, ym = 0, xe = 0, ye = 0;
            arc.GetThreePoints(ref xe, ref ye, ref xm, ref ym, ref xb, ref yb);
            
            

            double xc = arc.CenterX;
            double yc = arc.CenterY;
            double radius = arc.Radius;

            double dx1 = xb - xc;
            double dy1 = yb - yc;
            double ang1 = Math.Atan2(dx1, dy1);

            double ang2 = Math.Atan2(xe - xc, ye - yc);
            double sweep = ang2 - ang1;

            if (sweep < 0)
            {
                sweep = 2*Math.PI + sweep;
            }

            return new double[] { sweep, ang1 };
        }

        public static NFTask GetGeometry()
        {
            Msg("[Nesting Factory] Starting collect geometry...");

            ICollection<Area> EO = Doc.GetAreas();
            IEnumerator<Area> GeomEnum = EO.GetEnumerator();
            GeomEnum.MoveNext();

            NFTask task = new NFTask();

            for (int area_num = 0; area_num < EO.Count; area_num++)
            {
                Area area = GeomEnum.Current;
                GeomEnum.MoveNext();
                Rectangle BoundBox = area.BoundRect;
                double bound_x = BoundBox.Left;
                double bound_y = BoundBox.Top;


                NFItem item = new NFItem(area.ObjectId.ToString());

                for (int num_contour = 0; num_contour < area.ContourCount; num_contour++)
                {
                    Contour contour = area.GetContour(num_contour);
                    NFContour cont = new NFContour();

                    for (int num_segment = 0; num_segment < contour.SegmentCount; num_segment++)
                    {
                        ContourSegment csegment = contour.GetSegment(num_segment);

                        switch (csegment.GeometryType)
                        {
                            case ObjectGeometryType.Line:
                                LineGeometry linegeom = csegment.Geometry as LineGeometry;
                                cont.AddPoint(new NFPoint(linegeom.X1 - bound_x, bound_y - linegeom.Y1, 0));
                                cont.AddPoint(new NFPoint(linegeom.X2 - bound_x, bound_y - linegeom.Y2, 0));
                                break;
                            /*case ObjectGeometryType.Polyline:

                                PolylineGeometry polygeom = csegment.Geometry as PolylineGeometry;
                                CircleArcGeometry[] cArcs = polygeom.GetCircleArcApproximation(2);

                                for (int i = 0; i < cArcs.GetLength(0); i++)
                                {
                                    cArcToDoubles(cArcs[i], ref cont, BoundBox);
                                }
                                break;*/
                            case ObjectGeometryType.CircleArc:

                                CircleArcGeometry cgeom = csegment.Geometry as CircleArcGeometry;
                                cArcToDoubles(cgeom, ref cont, BoundBox, csegment.IsCounterclockwise);

                                break;
                            case ObjectGeometryType.Circle:
                                CircleGeometry cirgeom = csegment.Geometry as CircleGeometry;
                                cont.AddPoint(new NFPoint(cirgeom.CenterX + cirgeom.Radius - bound_x, bound_y - cirgeom.CenterY, 1));
                                cont.AddPoint(new NFPoint(cirgeom.CenterX - cirgeom.Radius - bound_x, bound_y - cirgeom.CenterY, 1));
                                break;
                            default:
                                PolylineGeometry polygeom = csegment.Geometry as PolylineGeometry;
                                int v_count = polygeom.Count;
                                for (int i = 0; i < v_count; i++)
                                {
                                    if (v_count < 50 || i % (csegment.GeometryType == ObjectGeometryType.Ellipse ? 5 : 1) == 0 || i == v_count)
                                    {
                                        cont.AddPoint(new NFPoint(polygeom.GetX(i) - bound_x, bound_y - polygeom.GetY(i), 0));
                                    }
                                }
                                break;
                        }
                    }
                    item.AddContour(cont);
                }
                task.AddItem(item);


            }
            Msg("[Nesting Factory] Geometry collected");
            return task;

        }
    }
}
