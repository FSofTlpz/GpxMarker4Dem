using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace GpxMarker4Dem {
   class Program {
      static void Main(string[] args) {

         Options opt = new Options();
         try {

            Assembly a = Assembly.GetExecutingAssembly();
            Console.WriteLine(
               ((AssemblyProductAttribute)(Attribute.GetCustomAttribute(a, typeof(AssemblyProductAttribute)))).Product + ", Version vom " +
               ((AssemblyInformationalVersionAttribute)(Attribute.GetCustomAttribute(a, typeof(AssemblyInformationalVersionAttribute)))).InformationalVersion + ", " +
               ((AssemblyCopyrightAttribute)(Attribute.GetCustomAttribute(a, typeof(AssemblyCopyrightAttribute)))).Copyright
            );
            Console.WriteLine(GarminCore.Garmin.DllTitle());
            Console.WriteLine();

            opt.Evaluate(args);
            Run(opt.Symbolname, opt.Input, opt.OutPath, opt.Zoomlevel, opt.OutputOverwrite);

         } catch (Exception ex) {
            Console.Error.WriteLine(ex.Message);
         }

      }

      static void Run(string symbolname, string srcfile, string dstpath, IList<int> zoomlevel, bool overwrite) {
         Console.Error.WriteLine("Symbolname: " + symbolname);
         Console.Error.WriteLine("File:       " + srcfile);
         Console.Error.WriteLine("Outputpath: " + dstpath);

         if (!File.Exists(srcfile))
            throw new Exception("File '" + srcfile + "' not exist.");

         string extension = Path.GetExtension(srcfile).ToUpper();
         if (extension == ".DEM") {

            using (GarminCore.BinaryReaderWriter dembr = new GarminCore.BinaryReaderWriter(srcfile, true)) {
               CalculateDem(dembr, dstpath, Path.GetFileNameWithoutExtension(srcfile), overwrite, symbolname, zoomlevel);
            }

         } else if (extension == ".IMG") {

            GarminCore.DskImg.SimpleFilesystem sf = new GarminCore.DskImg.SimpleFilesystem();
            using (GarminCore.BinaryReaderWriter br = new GarminCore.BinaryReaderWriter(srcfile, true)) {
               sf.Read(br);

               for (int i = 0; i < sf.FileCount; i++) {
                  string filename = sf.Filename(i);
                  if (Path.GetExtension(filename).ToUpper() == ".DEM") {
                     using (GarminCore.BinaryReaderWriter dembr = sf.GetBinaryReaderWriter4File(filename)) {
                        CalculateDem(dembr, dstpath, Path.GetFileNameWithoutExtension(filename), overwrite, symbolname, zoomlevel);
                     }
                  }
               }
            }

         }
      }

      /// <summary>
      /// liefert einen <see cref="GarminCore.BinaryReaderWriter"/> für die DEM-Datei
      /// </summary>
      /// <param name="dembr"></param>
      /// <param name="outpath">Ausgabepfad</param>
      /// <param name="basefilename"></param>
      /// <param name="overwrite"></param>
      /// <param name="symbolname"></param>
      /// <param name="zoomlevel"></param>
      static void CalculateDem(GarminCore.BinaryReaderWriter dembr, string outpath, string basefilename, bool overwrite, string symbolname, IList<int> zoomlevel) {
         GarminCore.Files.StdFile_DEM dem = new GarminCore.Files.StdFile_DEM();
         dem.Read(dembr);
         Console.WriteLine("-> " + basefilename + ".DEM with " + dem.ZoomlevelCount.ToString() + " zoomlevels");

         if (!Directory.Exists(outpath))
            Directory.CreateDirectory(outpath);

         string protfile = Path.Combine(outpath, basefilename + ".txt");
         if (File.Exists(protfile))
            if (!overwrite)
               throw new Exception("Error: File '" + protfile + "' exists.");
            else
               File.Delete(protfile);

         using (StreamWriter file = new StreamWriter(protfile)) {
            file.WriteLine(dem.ZoomlevelCount.ToString() + " zoomlevel");

            List<double> lon = new List<double>();
            List<double> lat = new List<double>();

            for (int zl = 0; zl < dem.ZoomlevelCount; zl++) {

               bool used = false;
               for (int i = 0; i < zoomlevel.Count; i++)
                  if (zoomlevel[i] == zl) {
                     used = true;
                     break;
                  }
               if (used) {

                  GarminCore.Files.DEM.ZoomlevelTableitem record = dem.ZoomLevel[zl].ZoomlevelItem;

                  double west = record.West;
                  double north = record.North;
                  double ptdisth = record.PointDistanceHoriz;
                  double ptdistv = record.PointDistanceVert;

                  file.WriteLine("");
                  file.WriteLine("zoomlevel " + zl.ToString());
                  file.WriteLine("{0}x{1} tiles", record.MaxIdxHoriz + 1, record.MaxIdxVert + 1);
                  file.WriteLine("height {0} ... {1}", record.MinHeight, record.MaxHeight);

                  lon.Clear();
                  lat.Clear();
                  int subtileidx = 0;
                  for (int x = 0; x <= record.MaxIdxHoriz; x++)
                     for (int y = 0; y <= record.MaxIdxVert; y++) {
                        GarminCore.Files.DEM.SubtileTableitem subtile = dem.ZoomLevel[zl].Subtiles[subtileidx].Tableitem;
                        subtileidx++;

                        double tileleft = west + x * (record.PointsHoriz - 1) * ptdisth;
                        double tiletop = north - y * (record.PointsVert - 1) * ptdistv;

                        file.WriteLine("subtile: x={0}, y={1}, baseheight {2}, heightdiff {3}, west={4}, north={5}",
                                       x,
                                       y,
                                       subtile.Baseheight,
                                       subtile.Diff,
                                       tileleft.ToString(CultureInfo.InvariantCulture),
                                       tiletop.ToString(CultureInfo.InvariantCulture));

                        int maxx = x < record.MaxIdxHoriz ? record.PointsHoriz : record.LastColWidth + 1;
                        int maxy = y < record.MaxIdxVert ? record.PointsVert : record.LastRowHeight + 1;
                        for (int xt = 0; xt < maxx; xt++)
                           for (int yt = 0; yt < maxx; yt++) {
                              lat.Add(tileleft + xt * ptdisth);
                              lon.Add(tiletop - yt * ptdistv);
                           }

                        WriteGpxFile(outpath, basefilename, zl, x, y, overwrite, symbolname, lon, lat, maxx, maxy);
                     }

               }
            }
         }
      }

      /// <summary>
      /// schreibt eine einzelne GPX-Datei
      /// </summary>
      /// <param name="outpath">Ausgabepfad</param>
      /// <param name="basefilename">Basis-Dateiname</param>
      /// <param name="zoomlevel">Zoomlevel</param>
      /// <param name="xpos">waagerechte Pos. im DEM-Raster</param>
      /// <param name="ypos">senkrechte Pos. im DEM-Raster</param>
      /// <param name="overwrite"></param>
      /// <param name="symbolname">Garmin-Symbolname</param>
      /// <param name="lon">Liste der geogr. Längen</param>
      /// <param name="lat">Liste der geogr. Breiten</param>
      /// <param name="maxx">Anzahl waagerecht</param>
      /// <param name="maxy">Anzahl senkrecht</param>
      static void WriteGpxFile(string outpath,
                        string basefilename,
                        int zoomlevel,
                        int xpos,
                        int ypos,
                        bool overwrite,
                        string symbolname,
                        IList<double> lon,
                        IList<double> lat,
                        int maxx,
                        int maxy) {
         string filename = string.Format("{0}\\{1}_{2}_{3:D3}_{4:D3}.gpx",
                                         outpath,
                                         basefilename,
                                         zoomlevel,
                                         xpos,
                                         ypos);
         if (!Directory.Exists(outpath))
            Directory.CreateDirectory(outpath);
         /*
          <?xml version="1.0" encoding="UTF-8" standalone="no" ?>
         <gpx xmlns="http://www.topografix.com/GPX/1/1" creator="MapSource 6.16.3" version="1.1" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd">
         <wpt lat="49.9998132884502" lon="13.0001346766949"><name>0000</name><sym>Waypoint</sym></wpt>
         ...
         <wpt lat="49.9823239445686" lon="13.0176240205765"><name>6363</name><sym>Waypoint</sym></wpt>
         </gpx>
         */
         if (File.Exists(filename)) {
            if (!overwrite)
               throw new Exception("Error: File '" + filename + "' exists.");
            File.Delete(filename);
         }

         using (StreamWriter file = new StreamWriter(filename, false, Encoding.UTF8)) {
            file.WriteLine("<?xml version=\"1.0\" encoding=\"UTF - 8\" standalone=\"no\" ?>");
            file.WriteLine("<gpx xmlns=\"http://www.topografix.com/GPX/1/1\" creator=\"MapSource 6.16.3\" version=\"1.1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd\">");
            int i = 0;
            int maxi = Math.Min(lat.Count, lon.Count);
            for (int x = 0; x < maxx; x++)
               for (int y = 0; y < maxx; y++) {
                  if (i < maxi)
                     file.WriteLine("<wpt lat=\"{0}\" lon=\"{1}\"><name>{2:D2}{3:D2}</name><sym>{4}</sym></wpt>",
                                    lat[i].ToString(CultureInfo.InvariantCulture),
                                    lon[i].ToString(CultureInfo.InvariantCulture),
                                    x,
                                    y,
                                    symbolname);
                  else
                     x = y = int.MaxValue;
                  i++;
               }
            file.WriteLine("</gpx>");
         }
      }

   }
}
