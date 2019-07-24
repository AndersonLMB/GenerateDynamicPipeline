using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS;
using ESRI.ArcGIS.Geometry;
namespace SplitTest
{
    class Program
    {
        static void Main(string[] args)
        {

            RuntimeManager.BindLicense(ProductCode.EngineOrDesktop);


            IPolyline polyline = new PolylineClass();
            IPoint point0 = new PointClass() { X = 0, Y = 0 };
            IPoint point1 = new PointClass() { X = 1, Y = 1 };
            IPoint point2 = new PointClass() { X = 2, Y = -1 };

            ((IPointCollection)polyline).AddPoint(point0);
            ((IPointCollection)polyline).AddPoint(point1);
            ((IPointCollection)polyline).AddPoint(point2);
            polyline.GetSubcurve(1, 2, false, out ICurve outCurve);

            var outPolyline = ((IPolyline)outCurve);
            var outPointCollection = ((IPointCollection)outPolyline);
            for (int i = 0; i < outPointCollection.PointCount; i++)
            {
                Console.WriteLine($"{outPointCollection.Point[i].X} {outPointCollection.Point[i].Y}");
            }


            //Console.WriteLine(((IPointCollection)polyline).PointCount);
            //polyline.SplitAtDistance(1, false, false, out var splitHappened, out var newPartIndex, out var segmentIndex);
            //Console.WriteLine(((IPointCollection)polyline).PointCount);
            //double length = polyline.Length;
            ;



        }
    }
}
