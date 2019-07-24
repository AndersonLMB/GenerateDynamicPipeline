using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using System.IO;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using System.Text;
using System.Threading.Tasks;

namespace GenerateDynamicPipeline
{
    class Program
    {
        static void Main(string[] args)
        {
            ESRI.ArcGIS.RuntimeManager.BindLicense(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            CommandLineApplication.Execute<Program>(args);
        }

        [Option(ShortName = "dst", LongName = "DataSourceType")]
        public DataSourceType DataSourceType { get; set; } = DataSourceType.Gdb;

        [Option(ShortName = "ds", LongName = "DataSource")]
        public string DataSource { get; set; } = @"C:\test\test.gdb";

        [Option(ShortName = "fcn", LongName = "FeatureClassName")]
        public string FeatureClassName { get; set; } = "test";

        [Option(ShortName = "odst", LongName = "OutDataSourceType")]
        public DataSourceType OutDataSourceType { get; set; } = DataSourceType.Gdb;

        [Option(ShortName = "ods", LongName = "OutDataSource")]
        public string OutDataSource { get; set; } = @"C:\test\test.gdb";

        [Option(ShortName = "ofcn", LongName = "OutFeatureClassName")]
        public string OutFeatureClassName { get; set; } = "out";


        [Option(ShortName = "pf", LongName = "ProjectionFile")]
        public string ProJectionFile { get; set; } = "proj3857.prj";

        [Option(ShortName = "sc", LongName = "ScalesFile")]
        public string ScalesFile { get; set; } = "Scales.txt";

        [Option(ShortName = "ll", LongName = "LineLength")]
        public double LineLength { get; set; } = 30;

        [Option(ShortName = "i", LongName = "Interval")]
        public double Interval { get; set; } = 60;

        [Option(ShortName = "fr", LongName = "FrameRate")]
        public int FrameRate { get; set; } = 5;

        [Option(ShortName = "ff", LongName = "FieldFrame")]
        public string FieldFrame { get; set; } = "FRAME";

        [Option(ShortName = "fs", LongName = "FieldScale")]
        public string FieldScale { get; set; } = "SCALE";

        private int _indexOfFieldScale = -1;
        private int _indexOfFieldFrame = -1;

        private void OnExecute()
        {
            Console.WriteLine(this.FeatureClassName);
            IWorkspaceFactory workspaceFactory = null;
            IWorkspace workspace = null;
            IFeatureClass featureClass = null;

            switch (DataSourceType)
            {
                case DataSourceType.Sde:
                    {
                        workspaceFactory = new SdeWorkspaceFactoryClass();
                    }
                    break;
                case DataSourceType.Gdb:
                    {
                        workspaceFactory = new FileGDBWorkspaceFactoryClass();
                    }
                    break;
                case DataSourceType.Shp:
                    {
                        workspaceFactory = new ShapefileWorkspaceFactoryClass();
                    }
                    break;
                default:
                    break;
            }
            workspace = workspaceFactory.OpenFromFile(DataSource, 0);
            featureClass = ((IFeatureWorkspace)workspace).OpenFeatureClass(FeatureClassName);

            IWorkspaceFactory outWorkspaceFactory = null;
            IWorkspace outWorkspace = null;
            IFeatureClass outFeatureClass = null;
            switch (OutDataSourceType)
            {
                case DataSourceType.Sde:
                    {
                        outWorkspaceFactory = new SdeWorkspaceFactoryClass();
                    }
                    break;
                case DataSourceType.Gdb:
                    {
                        outWorkspaceFactory = new FileGDBWorkspaceFactoryClass();
                    }
                    break;
                case DataSourceType.Shp:
                    {
                        outWorkspaceFactory = new ShapefileWorkspaceFactoryClass();
                    }
                    break;
                default:
                    break;
            }
            outWorkspace = outWorkspaceFactory.OpenFromFile(OutDataSource, 0);
            outFeatureClass = ((IFeatureWorkspace)outWorkspace).OpenFeatureClass(OutFeatureClassName);

            ISpatialReferenceFactory spatialReferenceFactory = new SpatialReferenceEnvironmentClass();
            FileInfo projectionFile = new FileInfo(ProJectionFile);
            ISpatialReference spatialReference = spatialReferenceFactory.CreateESRISpatialReferenceFromPRJFile(projectionFile.FullName);


            FileInfo scalesFile = new FileInfo(ScalesFile);
            System.IO.FileStream fs = scalesFile.OpenRead();
            StreamReader sr = new StreamReader(fs);
            string scalesString = sr.ReadToEnd();
            var scales = scalesString.Split('\r').Where(str => double.TryParse(str, out var d)).Select(str => double.Parse(str)).ToArray();
            sr.Close();

            ;
            try
            {
                _indexOfFieldScale = outFeatureClass.FindField(FieldScale);
            }
            catch (Exception)
            {
            }
            try
            {
                _indexOfFieldFrame = outFeatureClass.FindField(FieldFrame);
            }
            catch (Exception)
            {
            }

            GenerateAnimationLines(
              featureClass: featureClass,
              outFeatureClass: outFeatureClass,
              spatialReference: Get3857Sr(),
              frameRate: FrameRate,
              lineLengthInPixel: LineLength,
              intervalInPixel: Interval);
        }

        private ISpatialReference Get3857Sr()
        {
            ISpatialReferenceFactory spatialReferenceFactory = new SpatialReferenceEnvironmentClass();
            FileInfo projectionFile = new FileInfo("proj3857.prj");
            FileInfo scalesFile = new FileInfo(ScalesFile);
            ISpatialReference spatialReference = spatialReferenceFactory.CreateESRISpatialReferenceFromPRJFile(projectionFile.FullName);
            return spatialReference;
        }

        private void GenerateAnimationLines(IFeatureClass featureClass)
        {

        }

        private void GenerateAnimationLines(IFeatureClass featureClass, IFeatureClass outFeatureClass, ISpatialReference spatialReference = null, int frameRate = 60, double lineLengthInPixel = 5, double intervalInPixel = 10, string scaleFile = "Scales.txt")
        {
            FileInfo scalesFile = new FileInfo(ScalesFile);
            System.IO.FileStream fs = scalesFile.OpenRead();
            StreamReader sr = new StreamReader(fs);
            string scalesString = sr.ReadToEnd();
            var scales = scalesString.Split('\r').Where(str => int.TryParse(str, out var d)).Select(str => int.Parse(str)).ToArray();


            if (spatialReference == null)
            {
                spatialReference = Get3857Sr();
            }
            IFeatureCursor cursor = featureClass.Search(null, false);
            IFeature feature = null;
            while ((feature = cursor.NextFeature()) != null)
            {
                IGeometry geometry = feature.ShapeCopy;
                geometry.Project(spatialReference);

                foreach (int scale in scales)
                {
                    ProcessPolyline(geometry as IPolyline, outFeatureClass, scale, frameRate, lineLengthInPixel, intervalInPixel, spatialReference);
                }
                ;
            }
        }

        private void ProcessPolyline(IPolyline polyline, IFeatureClass outFeatureClass, int scale, int frameRate = 60, double lineLengthInPixel = 5, double intervalInPixel = 10, ISpatialReference spatialReference = null)
        {
            if (spatialReference == null) spatialReference = Get3857Sr();
            var pixelLength = PixelLengthAtScale(polyline.Length, scale, 96);
            double lineLength = LengthAtScaleOfPixelLength(lineLengthInPixel, scale);
            double intervalLength = LengthAtScaleOfPixelLength(intervalInPixel, scale);
            //too short
            if (pixelLength < intervalInPixel)
            {
            }
            else
            {

                for (int frame = 0; frame < frameRate; frame++)
                {

                    Console.WriteLine($"SCALE:{scale}, FRAME:{frame}");
                    double startLengthOfEachDrawLine = 0 + (((double)frame) / ((double)frameRate)) * intervalLength;
                    while (startLengthOfEachDrawLine < polyline.Length)
                    {
                        var endPointLength = startLengthOfEachDrawLine + lineLength;
                        if (endPointLength >= polyline.Length)
                        {
                            endPointLength = polyline.Length;
                        }
                        polyline.GetSubcurve(startLengthOfEachDrawLine, endPointLength, false, out ICurve curve);
                        IFeature createdFeature = outFeatureClass.CreateFeature();
                        createdFeature.Shape.SpatialReference = spatialReference;
                        createdFeature.Shape = (IPolyline)curve;
                        if (_indexOfFieldFrame >= 0)
                        {
                            try
                            {
                                createdFeature.Value[_indexOfFieldFrame] = frame;
                            }
                            catch (Exception)
                            {
                            }
                        }
                        if (_indexOfFieldScale >= 0)
                        {
                            try
                            {
                                createdFeature.Value[_indexOfFieldScale] = scale;
                            }
                            catch (Exception)
                            {
                            }
                        }
                        createdFeature.Store();
                        startLengthOfEachDrawLine += intervalLength;
                    }
                }



            }
            ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <param name="scale"></param>
        /// <param name="dpi"></param>
        /// <returns></returns>
        private double PixelLengthAtScale(double length, int scale, double dpi = 96)
        {
            return ((length / scale) * 1000) * (dpi / 25.6);
        }

        private double LengthAtScaleOfPixelLength(double pixelLength, int scale, double dpi = 96)
        {
            return ((pixelLength / (96 / 25.6)) / 1000) * scale;
        }




        private void Test()
        {
            FileInfo fi = new FileInfo("animation.mxd");
            IMapDocument mapDocument = new MapDocumentClass();
            mapDocument.Open(fi.FullName);

            var map = mapDocument.Map[0];
            ILayer layer = new FeatureLayerClass();

            //var map = mapDocument.Map[0];
        }


    }

    enum DataSourceType
    {
        Sde, Gdb, Shp
    }
}
