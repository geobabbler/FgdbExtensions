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

//No attempt has been made to handle WKT coordinate ordering silliness.
namespace Zekiah.FGDB
{
    public static class WktExtensions
    {
        public static string ToWKT(this Esri.FileGDB.ShapeBuffer geometry)
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
                    retval = processMultiPartBuffer(lbuff, "MULTILINESTRING");
                    break;
                case ShapeType.Polygon:
                case ShapeType.PolygonZ:
                case ShapeType.PolygonZM:
                    MultiPartShapeBuffer pbuff = geometry;
                    retval = processMultiPartBuffer(pbuff, "MULTIPOLYGON");
                    break;
            }
            return retval;
        }

        private static string processPointBuffer(PointShapeBuffer buffer)
        {
            string retval = "POINT ({0})";
            bool hasZ = false;
            try
            {
                hasZ = (buffer.Z != null);
            }
            catch
            {
                hasZ = false;
            }
            string coord = hasZ ? getCoordinate(buffer.point.x, buffer.point.y, buffer.Z) : getCoordinate(buffer.point.x, buffer.point.y);
            retval = string.Format(retval, coord);
            return retval;
        }

        private static string processMultiPointBuffer(MultiPointShapeBuffer buffer)
        {
            string retval = "MULTIPOINT ({0})";
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
            retval = string.Format(retval, coordList);
            return retval;
        }

        private static string processMultiPartBuffer(MultiPartShapeBuffer buffer, string wktType)
        {
            List<string> delims = getMultipartDelimiter(wktType);
            bool hasZ = false;
            try
            {
                hasZ = (buffer.Zs != null);
            }
            catch
            {
                hasZ = false;
            }
            string retval = wktType + "({0})";
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
            retval = string.Format(retval, polyList);
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
                    retval.Add("(");
                    retval.Add(")");
                    break;
                case "multipolygon":
                    retval.Add("((");
                    retval.Add("))");
                    break;
            }

            return retval;
        }

        private static string getCoordinate(double x, double y)
        {
            string retval = string.Format(CultureInfo.InvariantCulture, "{0} {1}", x, y);
            return retval;
        }

        private static string getCoordinate(double x, double y, double z)
        {
            string retval = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", x, y, z);
            return retval;
        }
    }
}
