using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TFlex.Model;
using TFlex.Drawing;
using TFlex.Model.Model2D;

namespace TFlexNF
{
    public enum NFRotation
    {
        None = 0,
        Pi = 1,
        HalfPi = 2,
        Free = 3,

    }

    public class NFPoint
    {
        public double x, y, b;
        public NFPoint(double x, double y, double b)
        {
            this.x = Math.Round(x, 1);
            this.y = Math.Round(y, 1);
            this.b = Math.Round(b, 3);
        }
    }

    public class NFContour
    {
        NFPoint[] Points = new NFPoint[0];

        public void AddPoint(NFPoint Point)
        {
            int L = Points.GetLength(0) + 1;
            Array.Resize<NFPoint>(ref Points, L);
            Points[L-1] = Point;
        }

        public NFPoint GetPoint(int id)
        {
            return Points[id];
        }

        public int VertexCount()
        {
            return Points.GetLength(0);
        }
    }

    public class NFItem
    {
        public int Rotation = 0;
        public int Reflection = 0;
        NFContour[] Contours = new NFContour[0];
        public string Name = "";
        public int Count = 0;

        public NFItem(string Name)
        {
            this.Name = Name;
        }

        public void AddContour(NFContour Contour)
        {
            int L = Contours.GetLength(0) + 1;
            Array.Resize<NFContour>(ref Contours, L);
            Contours[L-1] = Contour;
        }

        public NFContour GetContour(int id)
        {
            return Contours[id];
        }

        public void SetContour(int id, NFContour contour)
        {
            Contours[id] = contour;
        }

        public int ContourCount()
        {
            return Contours.GetLength(0);
        }

    }

    public class NFTask
    {

        NFItem[] Items = new NFItem[0];
        public int DomainCount = 1;
        public int DomainX;
        public int DomainY;
        public int DefaultRotation;
        public int DefaultReflection;
        public int DefaultItemCount;
        public int p2p = 5;
        public int p2l = 5;

        public int ListX;
        public int ListY;

        public void AddItem(NFItem Item)
        {
            int L = Items.GetLength(0) + 1;
            Array.Resize<NFItem>(ref Items, L);
            Items[L-1] = Item;
        }

        public NFItem GetItem(int id)
        {
            return Items[id];
        }

        public void SetItem(int id, NFItem item)
        {
            Items[id] = item;
        }

        public void RemoveItem(int id)
        {
            for (int i = 0; i < Items.GetLength(0)-id-1; i++)
            {
                Items[id + i] = Items[id + i + 1];
            }
            Array.Resize(ref Items, Items.GetLength(0) - 1);
        }

        public int Count()
        {
            return Items.GetLength(0);
        }

        public void SaveToItems(string filePath, bool toCatAgent)
        {
            //string filePath = "F:/items/";
            string taskfile = "TASKNAME:\tnest\nTIMELIMIT:\t3600000\nTASKTYPE:\tSheet\n";
            if (toCatAgent)
            {
                taskfile += String.Format("DOMAINFILE:\t{0}.item\n", this.Count());
            } else
            {
                taskfile += String.Format("WIDTH:\t{0}\nLENGTH:\t{1}\n", this.ListY, this.ListX);
            }
            
            taskfile += String.Format("SHEETQUANT:\t{0}\n", this.DomainCount);
            taskfile += String.Format("ITEM2DOMAINDIST:\t{0}\n", this.p2l);
            taskfile += String.Format("ITEM2ITEMDIST:\t{0}\n", this.p2p);



            for (int item_id = 0; item_id < this.Count(); item_id++)
            {
                NFItem Item = Items[item_id];
                int CC = Item.ContourCount();
                string fileData = "ITEMNAME:\t" + Item.Name + "\n";

                int rot = (Item.Rotation == 0 ? this.DefaultRotation : Item.Rotation - 1);
                string rotstep = "";
                switch (rot)
                {
                    case 0:
                        rotstep = "NO";
                        break;
                    case 1:
                        rotstep = "PI";
                        break;
                    case 2:
                        rotstep = "PI/2";
                        break;
                    case 3:
                        rotstep = "FREE";
                        break;

                }
                int refl = (Item.Reflection == 0 ? this.DefaultReflection : Item.Reflection - 1);
                int count = (Item.Count == 0 ? this.DefaultItemCount : Item.Count);

                taskfile += String.Format("ITEMFILE:\t{0}.item\n", item_id);
                taskfile += String.Format("ITEMQUANT:\t{0}\n", count);
                taskfile += String.Format("ROTATE:\t{0}\n", (rot > 1 ? 1 : rot));
                taskfile += String.Format("ROTSTEP:\t{0}\n", rotstep);
                taskfile += String.Format("REFLECT:\t{0}\n", refl);


                for (int contour_id = 0; contour_id < CC; contour_id++)
                {
                    NFContour contour = Item.GetContour(contour_id);
                    int VC = contour.VertexCount();
                    fileData += "VERTQUANT:\t" + VC + "\n";

                    for (int v_id = 0; v_id < VC; v_id++)
                    {
                        NFPoint Point = contour.GetPoint(v_id);
                        fileData += "VERTEX:\t" + Point.x + "\t" + Point.y + "\t" + Point.b + "\n";
                    }


                }
                using (StreamWriter sw = File.CreateText(filePath + item_id + ".item"))
                {
                    fileData = fileData.Replace(",", ".");
                    sw.WriteLine(fileData);
                    sw.Close();
                }
            }

            if (toCatAgent)
            {

                string DomainData = "ITEMNAME:\tdomain\nVERTQUANT:\t4\nVERTEX:\t0\t0\t0\n";
                DomainData += string.Format("VERTEX:\t{0}\t0\t0\n", this.ListX);
                DomainData += string.Format("VERTEX:\t{0}\t{1}\t0\n", this.ListX, this.ListY);
                DomainData += string.Format("VERTEX:\t0\t{0}\t0\n", this.ListY);

                using (StreamWriter sw = File.CreateText(filePath + this.Count() + ".item"))
                {
                    DomainData = DomainData.Replace(",", ".");
                    sw.WriteLine(DomainData);
                    sw.Close();
                }
            }

            using (StreamWriter sw = File.CreateText(filePath + "nest.task"))
            {
                sw.WriteLine(taskfile);
                sw.Close();
            }
        }
    }
}
