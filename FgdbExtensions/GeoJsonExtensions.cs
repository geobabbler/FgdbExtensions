using System;
using System.Collections.Generic;
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

        public static string ToGeoJson(this Esri.FileGDB.Row row)
        {
            string retval = "{ \"type\": \"Feature\", ";
            var geom = row.GetGeometry();
            string geomstr = "\"geometry\": " + geom.ToGeoJson() + ", ";
            retval += geomstr;
            retval += processFields(row);
            retval += "}";
            return retval;
        }

        private static string processFields(Esri.FileGDB.Row row)
        {
            string retval = "\"properties\": {";
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
            retval += propsjoin;
            retval += "}";

            return retval;
        }
        
        public static string ToGeoJson(this Esri.FileGDB.ShapeBuffer geometry)
        {
            string retval = "";
            var shapeType = (ShapeType)geometry.shapeType;
            switch (shapeType)
            {
                case ShapeType.Multipoint:
                case ShapeType.MultipointZ:
                    MultiPointShapeBuffer mptbuff = geometry;
                    retval = processMultiPointBuffer(mptbuff);
                    break;
                case ShapeType.Point:
                    PointShapeBuffer pt = geometry;
                    retval = processPointBuffer(geometry);
                    break;
                case ShapeType.PointZ:
                    break;
                case ShapeType.Polyline:
                    MultiPartShapeBuffer lbuff = geometry;
                    retval = processMultiPartBuffer(lbuff, "MultiLineString");
                    break;
                case ShapeType.PolylineZ:
                    break;
                case ShapeType.Polygon:
                    MultiPartShapeBuffer pbuff = geometry;
                    retval = processMultiPartBuffer(pbuff, "MultiPolygon");
                    break;
                case ShapeType.PolygonZ:
                    break;
            }
            return retval;
        }

        private static string processPointBuffer(PointShapeBuffer buffer)
        {
            string retval = "{\"type\":\"Point\", \"coordinates\": ";
            retval += getCoordinate(buffer.point.x, buffer.point.y) + "}";
            return retval;
        }

        private static string processMultiPointBuffer(MultiPointShapeBuffer buffer)
        {
            string retval = "{\"type\":\"MultiPoint\", \"coordinates\": [";
            Point[] points = buffer.Points;
            List<string> coords = new List<string>();
            for (int i = 0; i < points.Length; i++)
            {
                coords.Add(getCoordinate(points[i].x, points[i].y));
            }
            string[] coordArray = coords.ToArray();
            string coordList = string.Join(",", coordArray);
            retval += coordList + "]}";
            return retval;
        }

        private static string processMultiPartBuffer(MultiPartShapeBuffer buffer, string geoJsonType)
        {
            List<string> delims = getMultipartDelimiter(geoJsonType);
            string retval = "{\"type\":\"" + geoJsonType + "\", \"coordinates\": [";
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
                        string [] coordArray = coords.ToArray();
                        string coordList = string.Join(",", coordArray);
                        polys.Add(delims[0] + coordList + delims[1]);
                    }
                    coords = new List<string>();
                    partCount++;
                }
                string coord = getCoordinate(points[i].x, points[i].y);
                coords.Add(getCoordinate(points[i].x, points[i].y));
            }
            if (coords.Count > 0)
            {
                string[] coordArray = coords.ToArray();
                string coordList = string.Join(",", coordArray);
                polys.Add(delims[0] + coordList + delims[1]);
            }
            string[] polyArray = polys.ToArray();
            string polyList = string.Join(",", polyArray);
            retval += polyList + "]}";
            return retval;
        }

        private static List<string> getMultipartDelimiter(string geoJsonType)
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

        private static string getCoordinate(double x, double y)
        {
            string retval = string.Format("[{0}, {1}]", x, y);
            return retval;
        }
    }
}
