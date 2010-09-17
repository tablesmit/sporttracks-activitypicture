/*
Copyright (C) 2008 Dominik Laufer

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using QuartzTypeLib;
using ActivityPicturePlugin.UI.Activities;
using ZoneFiveSoftware.Common.Data.Fitness;
using System.Xml;
using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;

namespace ActivityPicturePlugin.Helper
{
    static class Functions
    {
        internal static DateTime GetFileTime(String filename)
        {
            try
            {
                string fileTime = com.SimpleRun.ShowOneFileOnlyTagOriginalDateTime(filename);
                IFormatProvider culture = new System.Globalization.CultureInfo("de-DE", true);
                DateTime dt = DateTime.ParseExact(fileTime, "yyyy:MM:dd HH:mm:ss", culture);
                return dt;
            }
            catch (Exception)
            {
                return new DateTime();
                //throw;
            }
        }
        internal static bool ValidVideoFile(string s)
        {
            try
            {
                FilgraphManagerClass FilGrMan = new FilgraphManagerClass();
                FilGrMan.RenderFile(s);
                FilGrMan = null;
                return true;

            }
            catch (Exception)
            {
                return false;
            }

        }

        internal static void PerformExportToGoogleEarth(List<ImageData> images, ZoneFiveSoftware.Common.Data.Fitness.IActivity act, string SavePath)
        {
            images.Sort(CompareByDate);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("    ");

            string KMZname = act.StartTime.ToLocalTime().ToString("dd. MMMM,yyyy") + " "
                        + Resources.Resources.ResourceManager.GetString("ImportControl_in") + " " + act.Location;
            string KMZstyle = "Photo";



            //using (XmlWriter writer = XmlWriter.Create(Console.Out, settings))
            string docFile = "";

            FileInfo kmzFile = new FileInfo(SavePath);

            String picDir = kmzFile.Directory.ToString() + "\\" + act.ReferenceId;
            if (kmzFile.Extension == ".kmz")
            {
                if (!System.IO.Directory.Exists(picDir)) System.IO.Directory.CreateDirectory(picDir);
                docFile = kmzFile.Directory.ToString() + "\\doc.kml";
            }
            else
            {
                docFile = SavePath;
            }

            //writing KML file
            using (XmlWriter writer = XmlWriter.Create(docFile, settings))
            {
                writer.WriteStartElement("kml", "http://earth.google.com/kml/2.2");
                writer.WriteStartElement("Document");

                writer.WriteElementString("name", "SportTracks Images exported with Activity Picture Plugin");
                writer.WriteElementString("open", "1");

                if (kmzFile.Extension == ".kml")
                {
                    writer.WriteStartElement("Style");
                    writer.WriteAttributeString("id", KMZstyle);
                    writer.WriteElementString("geomScale", "0.75");
                    writer.WriteStartElement("LabelStyle");
                    writer.WriteElementString("scale", "0");
                    writer.WriteEndElement(); //LabelStyle
                    writer.WriteStartElement("IconStyle");
                    writer.WriteElementString("color", "ffffffff");
                    writer.WriteStartElement("Icon");
                    writer.WriteElementString("href", "root://icons/palette-4.png");
                    writer.WriteElementString("x", "192");
                    writer.WriteElementString("y", "96");
                    writer.WriteElementString("w", "32");
                    writer.WriteElementString("h", "32");
                    writer.WriteEndElement(); //Icon
                    writer.WriteEndElement(); //IconStyle
                    writer.WriteEndElement(); //Style
                }
                else
                {
                    foreach (ImageData id in images)
                    {
                        if (id.EW.GPSLatitude != 0 & id.EW.GPSLongitude != 0)
                        {
                            //stylemaps
                            KMZstyle = id.ReferenceID;
                            WriteStyleMaps(act, KMZstyle, writer);
                        }
                    }
                }

                writer.WriteStartElement("Folder");
                writer.WriteElementString("name", KMZname);
                writer.WriteElementString("open", "1");

                WriteTrackXML(act, writer);

                foreach (ImageData id in images)
                {
                    if (id.EW.GPSLatitude != 0 & id.EW.GPSLongitude != 0)
                    {
                        string KMZfilesource; //source of image (which will be embedded in case of kmz)
                        string KMZLink;
                        if (kmzFile.Extension == ".kmz")
                        {
                            CreateKMZImages(picDir, id);
                            KMZfilesource = act.ReferenceId + "\\" + id.ReferenceID + ".jpg";
                            KMZLink = ">";
                            KMZstyle = id.ReferenceID;

                        }
                        else
                        {
                            KMZfilesource = id.ReferenceIDPath;
                            KMZstyle = "Photo";
                            KMZLink = " href='file://" + id.PhotoSource + "'>";
                        }

                        writer.WriteStartElement("Placemark");
                        writer.WriteElementString("name", id.PhotoSourceFileName);
                        writer.WriteElementString("Snippet", "");
                        writer.WriteStartElement("description");
                        int width = (int)(Math.Min(500, id.Ratio * 500));
                        int height = (int)((Single)(width) / id.Ratio);
                        string KMZpicdescription = "<P><FONT face=Verdana>"
                        + KMZname
                        + "</FONT></P><P><FONT face=Verdana size=2>"
                        + id.PhotoSourceFileName
                        + "</FONT></P><P><A"
                        + KMZLink
                        + "<IMG height="
                        + height
                        + " alt=" +
                        id.PhotoSourceFileName
                        + " hspace=0 src='"
                        + KMZfilesource
                        + "' width="
                        + width
                        + "></A></P><P><FONT face=Verdana size=2>"
                        + id.DateTimeOriginal
                        + "</FONT></P><P><FONT face=Verdana size=2>"
                        + id.ExifGPS
                        + "</FONT></P><P><FONT face=Verdana size=1>Created with ActivityPicturePlugin of SportTracks 2</FONT></P>";
                        writer.WriteCData(KMZpicdescription);
                        writer.WriteEndElement();//description

                        writer.WriteElementString("styleUrl", "#" + KMZstyle);
                        writer.WriteStartElement("Point");
                        writer.WriteElementString("altitudeMode", "absolute");
                        writer.WriteElementString("extrude", "1");
                        writer.WriteElementString("coordinates", id.KMLGPS);
                        writer.WriteEndElement(); //Point
                        writer.WriteEndElement(); //Placemark
                    }
                }



                writer.WriteEndElement(); //Folder
                writer.WriteEndElement(); //Document
                writer.WriteEndElement(); //kml

                // Write the XML to file and close the writer.
                writer.Flush();
                writer.Close();
            }


            if (kmzFile.Extension == ".kmz")
            {
                using (ZipOutputStream s = new ZipOutputStream(File.Create(SavePath)))
                {
                    s.SetLevel(6);
                    s.IsStreamOwner = true;
                    FileInfo fi;

                    string[] filenames = Directory.GetFiles(picDir);

                    foreach (string file in filenames)
                    {
                        ZipEntry entry = new ZipEntry(act.ReferenceId + "/" + Path.GetFileName(file));
                        fi = new FileInfo(file);
                        entry.DateTime = DateTime.Now;
                        entry.Size = fi.Length;
                        s.PutNextEntry(entry);
                        using (FileStream fs = File.OpenRead(file))
                        {
                            byte[] buffer = new byte[fs.Length];
                            fs.Read(buffer, 0, buffer.Length);
                            s.Write(buffer, 0, buffer.Length);
                        }

                    }
                    //add doc.kml
                    ZipEntry doc = new ZipEntry(Path.GetFileName(docFile));
                    fi = new FileInfo(docFile);
                    doc.DateTime = DateTime.Now;
                    doc.Size = fi.Length;
                    s.PutNextEntry(doc);
                    using (FileStream fs = File.OpenRead(docFile))
                    {
                        byte[] buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        s.Write(buffer, 0, buffer.Length);
                    }
                    s.Finish();
                    s.Close();
                }
                Directory.Delete(act.ReferenceId, true);
                File.Delete(Path.GetFileName(docFile));
            }
        }

        private static void WriteTrackXML(ZoneFiveSoftware.Common.Data.Fitness.IActivity act, XmlWriter writer)
        {
            //write track path of route
            writer.WriteStartElement("Placemark");
            writer.WriteElementString("name", "Route");
            writer.WriteElementString("visibility", "1");
            writer.WriteStartElement("Style");
            writer.WriteStartElement("IconStyle");
            writer.WriteStartElement("Icon");
            writer.WriteElementString("href", "http://www.zonefivesoftware.com/SportTracks/Images/SportTracksIcon48.png");
            writer.WriteElementString("w", "48");
            writer.WriteElementString("h", "48");
            writer.WriteEndElement(); //Icon
            writer.WriteEndElement(); //IconStyle
            writer.WriteElementString("geomScale", "6");
            writer.WriteElementString("geomColor", "660000ff");
            writer.WriteEndElement(); //Style
            writer.WriteStartElement("LineString");
            string strCoord = "";
            foreach (ZoneFiveSoftware.Common.Data.TimeValueEntry<ZoneFiveSoftware.Common.Data.GPS.IGPSPoint> gps in act.GPSRoute)
            {
                System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
                strCoord += gps.Value.LongitudeDegrees.ToString("0.00000000", ci) + "," + gps.Value.LatitudeDegrees.ToString("0.00000000", ci) + ",30" + Environment.NewLine;
            }
            writer.WriteElementString("coordinates", strCoord);
            writer.WriteEndElement(); //LineString
            writer.WriteEndElement(); //Placemark
        }

        internal static void PerformMultipleExportToGoogleEarth(IList<ZoneFiveSoftware.Common.Data.Fitness.IActivity> acts, string SavePath)
        {

            bool ImageFound = false; //if no images found, doc file will not be created

            string KMZstyle = "Photo";
            FileInfo kmzFile = new FileInfo(SavePath);

            string docFile = SavePath;
            if (kmzFile.Extension == ".kmz") docFile = kmzFile.Directory.ToString() + "\\doc.kml";

            //start writing xml file
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("    ");
            using (XmlWriter writer = XmlWriter.Create(docFile, settings))
            {
                writer.WriteStartElement("kml", "http://earth.google.com/kml/2.2");
                writer.WriteStartElement("Document");

                writer.WriteElementString("name", "SportTracks Images exported with Activity Picture Plugin");
                writer.WriteElementString("open", "1");

                if (kmzFile.Extension == ".kml")
                {
                    writer.WriteStartElement("Style");
                    writer.WriteAttributeString("id", KMZstyle);
                    writer.WriteElementString("geomScale", "0.75");
                    writer.WriteStartElement("LabelStyle");
                    writer.WriteElementString("scale", "0");
                    writer.WriteEndElement(); //LabelStyle
                    writer.WriteStartElement("IconStyle");
                    writer.WriteElementString("color", "ffffffff");
                    writer.WriteStartElement("Icon");
                    writer.WriteElementString("href", "root://icons/palette-4.png");
                    writer.WriteElementString("x", "192");
                    writer.WriteElementString("y", "96");
                    writer.WriteElementString("w", "32");
                    writer.WriteElementString("h", "32");
                    writer.WriteEndElement(); //Icon
                    writer.WriteEndElement(); //IconStyle
                    writer.WriteEndElement(); //Style
                }

                foreach (IActivity act in acts)
                {
                    //Get image data
                    List<ImageData> images = new List<ImageData>();
                    images.Sort(CompareByDate);
                    PluginData pd = ReadExtensionData(act);
                    if (pd.Images.Count != 0)
                    {
                        images = pd.LoadImageData(pd.Images);
                        ImageFound = true;
                    }
                    else continue; //if no images, continue to next activity

                    //Activity contains images
                    string KMZname = act.StartTime.ToLocalTime().ToString("dd. MMMM,yyyy") + " "
                    + Resources.Resources.ResourceManager.GetString("ImportControl_in") + " " + act.Location;

                    String picDir = kmzFile.Directory.ToString() + "\\" + act.ReferenceId;
                    if (kmzFile.Extension == ".kmz")
                    {
                        if (!System.IO.Directory.Exists(picDir)) System.IO.Directory.CreateDirectory(picDir);
                    }

                    //writing KML file for activity
                    if (kmzFile.Extension == ".kmz")
                    {
                        foreach (ImageData id in images)
                        {
                            if (id.EW.GPSLatitude != 0 & id.EW.GPSLongitude != 0)
                            {
                                //stylemaps
                                KMZstyle = id.ReferenceID;
                                WriteStyleMaps(act, KMZstyle, writer);
                            }
                        }
                    }

                    writer.WriteStartElement("Folder");
                    writer.WriteElementString("name", KMZname);
                    writer.WriteElementString("open", "1");

                    WriteTrackXML(act, writer);

                    foreach (ImageData id in images)
                    {
                        if (id.EW.GPSLatitude != 0 & id.EW.GPSLongitude != 0)
                        {
                            string KMZfilesource; //source of image (which will be embedded in case of kmz)
                            string KMZLink;
                            if (kmzFile.Extension == ".kmz")
                            {
                                CreateKMZImages(picDir, id);
                                KMZfilesource = act.ReferenceId + "\\" + id.ReferenceID + ".jpg";
                                KMZLink = ">";
                                KMZstyle = id.ReferenceID;

                            }
                            else
                            {
                                KMZfilesource = id.ReferenceIDPath;
                                KMZstyle = "Photo";
                                KMZLink = " href='file://" + id.PhotoSource + "'>";
                            }

                            writer.WriteStartElement("Placemark");
                            writer.WriteElementString("name", id.PhotoSourceFileName);
                            writer.WriteElementString("Snippet", "");
                            writer.WriteStartElement("description");
                            int width = (int)(Math.Min(500, id.Ratio * 500));
                            int height = (int)((Single)(width) / id.Ratio);
                            string KMZpicdescription = "<P><FONT face=Verdana>"
                            + KMZname
                            + "</FONT></P><P><FONT face=Verdana size=2>"
                            + id.PhotoSourceFileName
                            + "</FONT></P><P><A"
                            + KMZLink
                            + "<IMG height="
                            + height
                            + " alt=" +
                            id.PhotoSourceFileName
                            + " hspace=0 src='"
                            + KMZfilesource
                            + "' width="
                            + width
                            + "></A></P><P><FONT face=Verdana size=2>"
                            + id.DateTimeOriginal
                            + "</FONT></P><P><FONT face=Verdana size=2>"
                            + id.ExifGPS
                            + "</FONT></P><P><FONT face=Verdana size=1>Created with ActivityPicturePlugin of SportTracks 2</FONT></P>";
                            writer.WriteCData(KMZpicdescription);
                            writer.WriteEndElement();//description

                            writer.WriteElementString("styleUrl", "#" + KMZstyle);
                            writer.WriteStartElement("Point");
                            writer.WriteElementString("altitudeMode", "absolute");
                            writer.WriteElementString("extrude", "1");
                            writer.WriteElementString("coordinates", id.KMLGPS);
                            writer.WriteEndElement(); //Point
                            writer.WriteEndElement(); //Placemark
                        }
                    }
                    writer.WriteEndElement(); //Folder
                }

                writer.WriteEndElement(); //Document
                writer.WriteEndElement(); //kml

                // Write the XML to file and close the writer.
                writer.Flush();
                writer.Close();
            }


            if (!ImageFound) //no image at all found
            {
                File.Delete(Path.GetFileName(docFile));
                return;
            }


            // create zip file
            if (kmzFile.Extension == ".kmz")
            {
                using (ZipOutputStream s = new ZipOutputStream(File.Create(SavePath)))
                {
                    s.SetLevel(6);
                    s.IsStreamOwner = true;
                    FileInfo fi;

                    //create zip files for images
                    foreach (IActivity act in acts)
                    {
                        if (Directory.Exists(kmzFile.Directory.ToString() + "\\" + act.ReferenceId)) //exists only if act. contains images
                        {
                            string[] filenames = Directory.GetFiles(kmzFile.Directory.ToString() + "\\" + act.ReferenceId);
                            foreach (string file in filenames)
                            {
                                ZipEntry entry = new ZipEntry(act.ReferenceId + "/" + Path.GetFileName(file));
                                fi = new FileInfo(file);
                                entry.DateTime = DateTime.Now;
                                entry.Size = fi.Length;
                                s.PutNextEntry(entry);
                                using (FileStream fs = File.OpenRead(file))
                                {
                                    byte[] buffer = new byte[fs.Length];
                                    fs.Read(buffer, 0, buffer.Length);
                                    s.Write(buffer, 0, buffer.Length);
                                }
                            }
                            Directory.Delete(act.ReferenceId, true);
                        }
                    }

                    //add doc.kml
                    ZipEntry doc = new ZipEntry(Path.GetFileName(docFile));
                    fi = new FileInfo(docFile);
                    doc.DateTime = DateTime.Now;
                    doc.Size = fi.Length;
                    s.PutNextEntry(doc);
                    using (FileStream fs = File.OpenRead(docFile))
                    {
                        byte[] buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        s.Write(buffer, 0, buffer.Length);
                    }
                    s.Finish();
                    s.Close();
                }
                File.Delete(Path.GetFileName(docFile));
            }
        }

        private static void CreateKMZImages(String picDir, ImageData id)
        {


            //create small thumbnail
            Bitmap bmp = CreateSmallThumbnail(id, 108);
            Functions.SaveThumbnailImage(bmp, picDir + "\\" + id.ReferenceID + "_small.jpg", ActivityPicturePageControl.PluginSettingsData.data.Quality);

            if (!File.Exists(picDir + "\\" + id.ReferenceID + ".jpg"))
            //copy image from webfiles folder to the image folder of the zip archive (if not already exist)
            {
                bmp = CreateSmallThumbnail(id, (int)(ActivityPicturePageControl.PluginSettingsData.data.Size * 50 * 0.75));
                Functions.SaveThumbnailImage(bmp, picDir + "\\" + id.ReferenceID + ".jpg", ActivityPicturePageControl.PluginSettingsData.data.Quality);
                //System.IO.File.Copy(id.ReferenceIDPath, picDir + "\\" + id.ReferenceID + ".jpg");
            }
        }

        private static Bitmap CreateSmallThumbnail(ImageData id, int minSize)
        {
            Bitmap bmp = null;
            //if (minSize > 150)
            //{
            //    if (File.Exists(id.PhotoSource)) bmp = new Bitmap(id.PhotoSource);
            //}

            //minsize<=150 or original image not found
            if (bmp == null)
            {
                bmp = new Bitmap(id.ReferenceIDPath);
            }

            int Swidth, Sheight;
            double ratio = (double)(bmp.Width) / (double)(bmp.Height);
            if (ratio > 1)
            {
                Swidth = (int)(minSize * ratio);
                Sheight = minSize;
            }
            else
            {
                Swidth = minSize;
                Sheight = (int)(minSize / ratio);
            }
            Size size = new Size(Swidth, Sheight);
            Bitmap bmpNew = new Bitmap(bmp, size);

            //copying the metadata of the original file into the new image
            foreach (System.Drawing.Imaging.PropertyItem pItem in bmp.PropertyItems)
            {
                bmpNew.SetPropertyItem(pItem);
            }

            //quality too bad
            //bmp = (Bitmap)(bmp.GetThumbnailImage(Swidth, Sheight, null, new IntPtr()));
            return bmpNew;
        }

        private static void WriteStyleMaps(ZoneFiveSoftware.Common.Data.Fitness.IActivity act, string KMZstyle, XmlWriter writer)
        {
            writer.WriteStartElement("StyleMap");
            writer.WriteAttributeString("id", KMZstyle);
            writer.WriteStartElement("Pair");
            writer.WriteElementString("key", "normal");
            writer.WriteElementString("styleUrl", "#" + KMZstyle + "_norm");
            writer.WriteEndElement(); //Pair
            writer.WriteStartElement("Pair");
            writer.WriteElementString("key", "highlight");
            writer.WriteElementString("styleUrl", "#" + KMZstyle + "_high");
            writer.WriteEndElement(); //Pair
            writer.WriteEndElement(); //StyleMap

            writer.WriteStartElement("Style");
            writer.WriteAttributeString("id", KMZstyle + "_norm");
            writer.WriteStartElement("IconStyle");
            writer.WriteStartElement("Icon");
            writer.WriteElementString("href", act.ReferenceId + "\\" + KMZstyle + "_small.jpg");
            writer.WriteEndElement(); //Icon
            writer.WriteEndElement(); //IconStyle
            writer.WriteStartElement("LabelStyle");
            writer.WriteElementString("scale", "0");
            writer.WriteEndElement(); //LabelStyle
            writer.WriteStartElement("BalloonStyle");
            writer.WriteElementString("text", "$[description]");
            writer.WriteEndElement(); //BalloonStyle
            writer.WriteEndElement(); //Style


            writer.WriteStartElement("Style");
            writer.WriteAttributeString("id", KMZstyle + "_high");
            writer.WriteStartElement("IconStyle");
            writer.WriteElementString("scale", "2");
            writer.WriteStartElement("Icon");
            writer.WriteElementString("href", act.ReferenceId + "\\" + KMZstyle + "_small.jpg");
            writer.WriteEndElement(); //Icon
            writer.WriteEndElement(); //IconStyle
            writer.WriteStartElement("BalloonStyle");
            writer.WriteElementString("text", "$[description]");
            writer.WriteEndElement(); //BalloonStyle
            writer.WriteEndElement(); //Style
        }

        internal static byte[] GetGPSByteValue(double value)
        {
            byte[] val = new byte[24];
            Int32 d1 = (Int32)(value); //degree
            Int32 d1den = 1;
            Int32 d2 = (Int32)((value - d1) * 60); //minutes
            Int32 d2den = 1;
            Int32 d3 = (Int32)((value - d1 - (double)(d2) / 60) * 60 * 60 * 100); //seconds with 2 digits after comma
            Int32 d3den = 100;
            BitConverter.GetBytes(d1).CopyTo(val, 0);
            BitConverter.GetBytes(d1den).CopyTo(val, 4);
            BitConverter.GetBytes(d2).CopyTo(val, 8);
            BitConverter.GetBytes(d2den).CopyTo(val, 12);
            BitConverter.GetBytes(d3).CopyTo(val, 16);
            BitConverter.GetBytes(d3den).CopyTo(val, 20);
            return val;
        }

        internal static double GetGPSDoubleValue(byte[] val)
        {
            Int32 d1 = BitConverter.ToInt32(val, 0);
            Int32 d1den = BitConverter.ToInt32(val, 4);
            Int32 d2 = BitConverter.ToInt32(val, 8);
            Int32 d2den = BitConverter.ToInt32(val, 12);
            Int32 d3 = BitConverter.ToInt32(val, 16);
            Int32 d3den = BitConverter.ToInt32(val, 20);
            double d = (double)(d1) / (double)(d1den) + (double)(d2) / (double)(d2den) / 60 + (double)(d3) / (double)(d3den) / 60 / 60;
            return d;
        }
        //public static bool ValidImageFile(string p)
        //    {
        //    try
        //        {
        //        Image img = new Bitmap(p);
        //        img.Dispose();
        //        return true;
        //        }
        //    catch (Exception)
        //        {
        //        return false;
        //        }
        //    }

        internal static void DeleteThumbnails(List<string> referenceIDs)
        {
            try
            {
                foreach (string referenceID in referenceIDs)
                {
                    string ThumbnailPath = ActivityPicturePlugin.UI.Activities.ActivityPicturePageControl.ImageFilesFolder + referenceID + ".jpg";
                    if (System.IO.File.Exists(ThumbnailPath))
                    {
                        System.IO.File.Delete(ThumbnailPath);
                    }
                }
            }
            catch (Exception)
            {
                //throw;
            }
        }


        public static void OpenImage(string photoSource, string referenceID)
        {
            //try to open Photosource first
            try
            {
                if (System.IO.File.Exists(photoSource))
                {
                    OpenImageWithWindowsViewer(photoSource);
                }
                // if not found, try next to open image from ...\Web Files\Images folder
                else
                {
                    if (System.IO.File.Exists(referenceID))
                    {
                        OpenImageWithWindowsViewer(referenceID);
                    }
                    // if both locations are not found, nothing will happen
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static void OpenImageWithWindowsViewer(string ImageLocation)
        {
            try
            {
                //show picture with windows
                string sys = System.Environment.GetFolderPath(Environment.SpecialFolder.System);
                System.Diagnostics.ProcessStartInfo f = new System.Diagnostics.ProcessStartInfo
                (sys + "\\rundll32.exe",
                sys + "\\shimgvw.dll,ImageView_Fullscreen " +
                ImageLocation);
                System.Diagnostics.Process.Start(f);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
        public static void OpenVideoInExternalWindow(string p)
        {
            try
            {
                FilgraphManager graphManager = new FilgraphManager();
                // QueryInterface for the IMediaControl interface:
                IMediaControl mc = (IMediaControl)graphManager;
                // Call some methods on a COM interface 
                // Pass in file to RenderFile method on COM object. 
                mc.RenderFile(p);
                // Show file. 
                mc.Run();
            }
            catch (Exception)
            {
                //throw;
            }
            Console.WriteLine("Press Enter to continue.");
        }


        public static void WriteExtensionData(ZoneFiveSoftware.Common.Data.Fitness.IActivity act, PluginData pd)
        {
            try
            {
                //store data of images in the serializable wrapper class
                if (pd.Images.Count == 0)
                {
                    act.SetExtensionData(ActivityPicturePlugin.Plugin.GUID, null);
                    act.SetExtensionText(ActivityPicturePlugin.Plugin.GUID, "");
                }
                else
                {
                    System.IO.MemoryStream mem = new System.IO.MemoryStream();
                    System.Xml.Serialization.XmlSerializer xmlSer = new System.Xml.Serialization.XmlSerializer(typeof(PluginData));
                    xmlSer.Serialize(mem, pd);
                    act.SetExtensionData(ActivityPicturePlugin.Plugin.GUID, mem.ToArray());
                    act.SetExtensionText(ActivityPicturePlugin.Plugin.GUID, "Picture Plugin");
                    mem.Close();
                }

                ActivityPicturePlugin.Plugin.GetIApplication().Logbook.Modified = true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public static ActivityPicturePlugin.UI.Activities.PluginData ReadExtensionData(ZoneFiveSoftware.Common.Data.Fitness.IActivity act)
        {
            try
            {
                PluginData pd;
                byte[] b = act.GetExtensionData(ActivityPicturePlugin.Plugin.GUID);
                if (!(b.Length == 0))
                {
                    System.Xml.Serialization.XmlSerializer xmlSer = new System.Xml.Serialization.XmlSerializer(typeof(PluginData));
                    System.IO.MemoryStream mem = new System.IO.MemoryStream();
                    mem.Write(b, 0, b.Length);
                    mem.Position = 0;
                    //this.PluginExtensionData = (PluginData)xmlSer.Deserialize(mem);
                    pd = (PluginData)xmlSer.Deserialize(mem);
                    mem.Dispose();
                    xmlSer = null;
                    b = null;
                    return pd;
                }
                else
                {
                    //this.PluginExtensionData = new PluginData();
                    return new PluginData();
                }
            }
            catch (Exception)
            {
                return new PluginData();
                //throw;
            }

        }
        internal static void SaveThumbnailImage(Bitmap bmp, string defpath, Int64 quality)
        {
            try
            {
                System.Drawing.Imaging.ImageCodecInfo[] codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                System.Drawing.Imaging.ImageCodecInfo codec = null;
                for (int i = 0; i < codecs.Length; i++)
                {
                    if (codecs[i].MimeType.Equals("image/jpeg"))
                    {
                        codec = codecs[i];
                        break;
                    }
                }
                if (codec != null)
                {
                    //save with the changed settings (quality, color depth)
                    System.Drawing.Imaging.Encoder encoderInstance = System.Drawing.Imaging.Encoder.Quality;
                    System.Drawing.Imaging.EncoderParameters encoderParametersInstance;
                    encoderParametersInstance = new System.Drawing.Imaging.EncoderParameters(2);
                    //100% quality
                    if (quality < 1 | quality > 10) quality = 10; //check for wrong input;
                    System.Drawing.Imaging.EncoderParameter encoderParameterInstance = new System.Drawing.Imaging.EncoderParameter(encoderInstance, (long)(quality * 10));
                    encoderParametersInstance.Param[0] = encoderParameterInstance;
                    encoderInstance = System.Drawing.Imaging.Encoder.ColorDepth;
                    //24bit color depth
                    encoderParameterInstance = new System.Drawing.Imaging.EncoderParameter(encoderInstance, 24L);
                    encoderParametersInstance.Param[1] = encoderParameterInstance;
                    bmp.Save(defpath, codec, encoderParametersInstance);
                }
                else
                //save with the default settings
                {
                    bmp.Save(defpath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
            }
            catch (Exception)
            {
                //throw;
            }

        }
        internal static void GeoTagWithActivity(string filepath, ZoneFiveSoftware.Common.Data.Fitness.IActivity act)
        {
            try
            {
                ExifWorks EW = new ExifWorks(filepath);
                EW.GPSLatitude = act.GPSRoute.GetInterpolatedValue(EW.DateTimeOriginal.ToUniversalTime()).Value.LatitudeDegrees;
                EW.GPSLongitude = act.GPSRoute.GetInterpolatedValue(EW.DateTimeOriginal.ToUniversalTime()).Value.LongitudeDegrees;
                EW.GPSAltitude = act.GPSRoute.GetInterpolatedValue(EW.DateTimeOriginal.ToUniversalTime()).Value.ElevationMeters;
                // Save Image with new Exif data

                EW.GetBitmap().Save(filepath);
            }
            catch (Exception)
            {

                //throw;
            }

        }
        internal static Image getThumbnailWithBorder(int width, Image img)
        {
            try
            {
                Image thumb = new Bitmap(width, width);
                Image tmp = null;
                //If the original image is small than the Thumbnail size, just draw in the center
                if (img.Width < width && img.Height < width)
                {
                    using (Graphics g = Graphics.FromImage(thumb))
                    {
                        int xoffset = (int)((width - img.Width) / 2);
                        int yoffset = (int)((width - img.Height) / 2);
                        g.DrawImage(img, xoffset, yoffset, img.Width, img.Height);
                    }
                }
                else //Otherwise we have to get the thumbnail for drawing
                {
                    Image.GetThumbnailImageAbort myCallback = new
                        Image.GetThumbnailImageAbort(ThumbnailCallback);
                    if (img.Width == img.Height)
                    {
                        thumb = img.GetThumbnailImage(
                                 width, width,
                                 myCallback, IntPtr.Zero);
                    }
                    else
                    {
                        int k = 0;
                        int xoffset = 0;
                        int yoffset = 0;
                        if (img.Width < img.Height)
                        {
                            k = (int)(width * img.Width / img.Height);
                            tmp = img.GetThumbnailImage(k, width, myCallback, IntPtr.Zero);
                            xoffset = (int)((width - k) / 2);
                        }
                        if (img.Width > img.Height)
                        {
                            k = (int)(width * img.Height / img.Width);
                            tmp = img.GetThumbnailImage(width, k, myCallback, IntPtr.Zero);
                            yoffset = (int)((width - k) / 2);
                        }
                        using (Graphics g = Graphics.FromImage(thumb))
                        {
                            g.DrawImage(tmp, xoffset, yoffset, tmp.Width, tmp.Height);
                        }
                    }
                }
                using (Graphics g = Graphics.FromImage(thumb))
                {
                    g.DrawRectangle(Pens.Black, 0, 0, thumb.Width - 1, thumb.Height - 1);
                }
                return thumb;
            }
            catch (Exception)
            {
                return null;
            }

        }
        internal static bool ThumbnailCallback()
        {
            return true;
        }

        internal static ImageData.DataTypes GetMediaType(string p)
        {
            if (p.Length > 4) p = p.Substring(p.Length - 4);
            string[] extimg = { ".jpg", ".png", ".tif", ".gif", ".bmp" };
            string[] extvid = { ".avi", ".wmv", ".mgp", ".mpeg" };
            foreach (string str in extimg)
            {
                if (str == p.ToLower()) return ImageData.DataTypes.Image;
            }
            foreach (string str in extvid)
            {
                if (str == p.ToLower()) return ImageData.DataTypes.Video;
            }
            return ImageData.DataTypes.Nothing;
        }

        internal static ImageData AddImage(String ImageLocation, Boolean CreateThumbnail)
        {
            try
            {
                ImageData ID = new ImageData();
                ID.PhotoSource = ImageLocation;
                ID.ReferenceID = Guid.NewGuid().ToString();
                //Bitmap bmp = new Bitmap(ImageLocation);
                //ID.Ratio = (Single)(bmp.Width) / (Single)(bmp.Height);
                //bmp.Dispose();


                ID.Type = ImageData.DataTypes.Image;
                if (CreateThumbnail)
                {
                    ID.SetThumbnail();
                    ID.EW = new ExifWorks(ID.ReferenceIDPath);
                }
                else
                {
                    ID.EW = new ExifWorks(ID.PhotoSource);
                }

                ID.Ratio = (Single)(ID.EW.GetBitmap().Width) / (Single)(ID.EW.GetBitmap().Height);

                return ID;
            }
            catch (Exception)
            {
                return null;
            }

        }

        internal static void ClearImageList(PictureAlbum pa)
        {
            try
            {

                foreach (ImageData ID in pa.ImageList)
                {
                    if (ID.EW != null)
                    {
                        ID.EW.Dispose();
                    }
                    ID.Dispose();
                }

                pa.ImageList.Clear();
            }
            catch (Exception)
            {

                //throw;
            }
        }

        internal static int CompareByDate(ImageData x, ImageData y)
        {
            try
            {
                if (x == null)
                {
                    if (y == null) return 0; // If x is null and y is null, they're equal. 
                    else return -1; // If x is null and y is not null, y is greater. 
                }
                else
                {
                    // If x is not null...
                    if (y == null) return 1;// ...and y is null, x is greater.
                    else
                    {
                        // ...and y is not null, compare the dates
                        int retval = x.DateTimeOriginal.CompareTo(y.DateTimeOriginal);
                        if (retval != 0) return retval;// If they are not equal, the later date is greater.
                        else return 0;// If the dates are equal, 0 is returned
                    }
                }
            }
            catch (Exception)
            {
                return 0;
                //throw;
            }

        }
    }
}
