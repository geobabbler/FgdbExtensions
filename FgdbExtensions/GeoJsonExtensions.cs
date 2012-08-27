//The MIT License

//Copyright (c) 2012 Zekiah Technologies, Inc.

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Esri.FileGDB;

//TODO: throw in some exception handling throughout
namespace Zekiah.FGDB
{
    public static class GeoJsonExtensions
    {
        public static string ToGeoJson(this Esri.FileGDB.RowCollection rows)
        {
            try
            {
                string retval = "{ \"type\": \"FeatureCollection\", ";
                retval += "\"features\": [";
                List<string> feats = new List<string>();
                foreach (Row row in rows)
                {
                    feats.Add(row.ToGeoJson());
                }
                var featarray = feats.ToArray();
                var featjoin = string.Join(",", featarray);
                retval += featjoin;
                retval += "]}";
                return retval;
            }
            catch (Exception ex)
            {
                throw new Exception("Error processing row collection", ex);
            }
        }

        public static string ToGeoJson(this Esri.FileGDB.Row row)
        {
            try
            {
                StringBuilder sb = new StringBuilder("{ \"type\": \"Feature\", ");
                var geom = row.GetGeometry();
                sb.Append("\"geometry\": ");
                sb.Append(geom.ToGeoJson());
                sb.Append(", ");
                sb.Append(processFields(row));
                sb.Append("}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error processing row", ex);
            }
        }

        private static string processFields(Esri.FileGDB.Row row)
        {
            try
            {
                StringBuilder sb = new StringBuilder("\"properties\": {");
                List<string> props = new List<string>();
                string proptemplate = "\"{0}\": \"{1}\"";
                for (int fldnum = 0; fldnum < row.FieldInformation.Count; fldnum++)
                {
                    string fldname = row.FieldInformation.GetFieldName(fldnum);
                    string fldval = "";
                    if (row.IsNull(fldname))
                    {
                        fldval = "null";
                    }
                    else
                    {
                        switch (row.FieldInformation.GetFieldType(fldnum))
                        {
                            case FieldType.Geometry:
                                fldval = "geometry";
                                break;
                            case FieldType.Blob:
                                fldval = "blob";
                                break;
                            case FieldType.SmallInteger:
                                fldval = row.GetShort(fldname).ToString();
                                break;
                            case FieldType.Integer:
                                fldval = row.GetInteger(fldname).ToString();
                                break;
                            case FieldType.Single:
                                fldval = row.GetFloat(fldname).ToString();
                                break;
                            case FieldType.Double:
                                fldval = row.GetDouble(fldname).ToString();
                                break;
                            case FieldType.String:
                                fldval = row.GetString(fldname);
                                break;
                            case FieldType.Date:
                                fldval = row.GetDate(fldname).ToLongTimeString();
                                break;
                            case FieldType.OID:
                                fldval = row.GetOID().ToString();
                                break;
                            case FieldType.GUID:
                                fldval = row.GetGUID(fldname).ToString();
                                break;
                            case FieldType.GlobalID:
                                fldval = row.GetGlobalID().ToString();
                                break;
                            default:
                                break;
                        }
                    }
                    string propval = string.Format(proptemplate, fldname, fldval);
                    props.Add(propval);
                }
                var proparray = props.ToArray();
                string propsjoin = string.Join(",", proparray);
                sb.Append(propsjoin);
                sb.Append("}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error processing attributes", ex);
            }
        }
        
        public static string ToGeoJson(this Esri.FileGDB.ShapeBuffer geometry)
        {
            try
            {
                string retval = "";
                var shapeType = (ShapeType)geometry.shapeType;
                //note: "M" values are not really supported. Relevant geometries are handled to extract Z values, if present.
                switch (shapeType)
                {
                    case ShapeType.Multipoint:
                    case ShapeType.MultipointZ:
                    case ShapeType.MultipointZM:
                        MultiPointShapeBuffer mptbuff = geometry;
                        retval = processMultiPointBuffer(mptbuff);
                        break;
                    case ShapeType.Point:
                    case ShapeType.PointZ:
                    case ShapeType.PointZM:
                        PointShapeBuffer pt = geometry;
                        retval = processPointBuffer(geometry);
                        break;
                    case ShapeType.Polyline:
                    case ShapeType.PolylineZ:
                    case ShapeType.PolylineZM:
                        MultiPartShapeBuffer lbuff = geometry;
                        retval = processMultiPartBuffer(lbuff, "MultiLineString");
                        break;
                    case ShapeType.Polygon:
                    case ShapeType.PolygonZ:
                    case ShapeType.PolygonZM:
                        MultiPartShapeBuffer pbuff = geometry;
                        retval = processMultiPartBuffer(pbuff, "MultiPolygon");
                        break;
                }
                return retval;
            }
            catch (Exception ex)
            {
                throw new Exception("Error processing geometry", ex);
            }
        }

        private static string processPointBuffer(PointShapeBuffer buffer)
        {
            try
            {
                StringBuilder retval = new StringBuilder("{\"type\":\"Point\", \"coordinates\": ");
                bool hasZ = false;
                try
                {
                    hasZ = (buffer.Z != null); //this should always be true because a double is never null, but API throws and exception if no Z.
                }
                catch
                {
                    hasZ = false;
                }
                string coord = hasZ ? getCoordinate(buffer.point.x, buffer.point.y, buffer.Z) : getCoordinate(buffer.point.x, buffer.point.y);
                retval.Append(coord);
                retval.Append("}");
                return retval.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error processing point buffer", ex);
            }
        }

        private static string processMultiPointBuffer(MultiPointShapeBuffer buffer)
        {
            try
            {
                StringBuilder retval = new StringBuilder("{\"type\":\"MultiPoint\", \"coordinates\": [");
                bool hasZ = false;
                try
                {
                    hasZ = (buffer.Zs != null);
                }
                catch
                {
                    hasZ = false;
                }
                Point[] points = buffer.Points;
                List<string> coords = new List<string>();
                for (int i = 0; i < points.Length; i++)
                {
                    string coord = hasZ ? getCoordinate(points[i].x, points[i].y, buffer.Zs[i]) : getCoordinate(points[i].x, points[i].y);
                    coords.Add(coord);
                }
                string[] coordArray = coords.ToArray();
                string coordList = string.Join(",", coordArray);
                retval.Append(coordList);
                retval.Append("]}");
                return retval.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error processing multipoint buffer", ex);
            }
        }

        private static string processMultiPartBuffer(MultiPartShapeBuffer buffer, string geoJsonType)
        {
            try
            {
                List<string> delims = getMultipartDelimiter(geoJsonType);
                bool hasZ = false;
                try
                {
                    hasZ = (buffer.Zs != null);
                }
                catch
                {
                    hasZ = false;
                }
                StringBuilder retval = new StringBuilder("{\"type\":\"" + geoJsonType + "\", \"coordinates\": [");
                int numPts = buffer.NumPoints;
                int numParts = buffer.NumParts;
                int[] parts = buffer.Parts;

                Point[] points = buffer.Points;
                List<string> coords = new List<string>();
                List<string> polys = new List<string>();
                int partCount = 0;
                for (int i = 0; i < numPts; i++)
                {
                    if ((partCount < numParts) && (i == parts[partCount]))
                    {
                        if (coords.Count > 0)
                        {
                            string[] coordArray = coords.ToArray();
                            string coordList = string.Join(",", coordArray);
                            polys.Add(delims[0] + coordList + delims[1]);
                        }
                        coords = new List<string>();
                        partCount++;
                    }
                    string coord = hasZ ? getCoordinate(points[i].x, points[i].y, buffer.Zs[i]) : getCoordinate(points[i].x, points[i].y);
                    coords.Add(coord);
                }
                if (coords.Count > 0)
                {
                    string[] coordArray = coords.ToArray();
                    string coordList = string.Join(",", coordArray);
                    polys.Add(delims[0] + coordList + delims[1]);
                }
                string[] polyArray = polys.ToArray();
                string polyList = string.Join(",", polyArray);
                retval.Append(polyList);
                retval.Append("]}");
                return retval.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error processing multipart buffer", ex);
            }
        }

        private static List<string> getMultipartDelimiter(string geoJsonType)
        {
            try
            {
                List<string> retval = new List<string>();

                switch (geoJsonType.ToLower())
                {
                    case "multipoint":
                        retval.Add("");
                        retval.Add("");
                        break;
                    case "multilinestring":
                        retval.Add("[");
                        retval.Add("]");
                        break;
                    case "multipolygon":
                        retval.Add("[[");
                        retval.Add("]]");
                        break;
                }

                return retval;
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating delimiter", ex);
            }
        }

        private static string getCoordinate(double x, double y)
        {
            try
            {
                string retval = string.Format(CultureInfo.InvariantCulture, "[{0}, {1}]", x, y);
                return retval;
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating coordinate", ex);
            }
        }

        private static string getCoordinate(double x, double y, double z)
        {
            try
            {
                string retval = string.Format(CultureInfo.InvariantCulture, "[{0}, {1}, {2}]", x, y, z);
                return retval;
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating coordinate", ex);
            }
        }
    }
}
