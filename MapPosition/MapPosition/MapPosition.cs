#region

using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace MapPosition
{
    public class MapPosition
    {
        internal static readonly Dictionary<String, Point[]> Regions = new Dictionary<string, Point[]>();

        public static void Initialize()
        {
            Point[] pts = new Point[] { new Point(1770, 5001), new Point(2084, 11596), new Point(3421, 9782), new Point(3841, 9305), new Point(4703, 8844), new Point(6345, 7451), new Point(3518, 4587) };
            Regions["topLeftOuterJungle"] = pts;

            pts = new Point[] { new Point(3274, 5106), new Point(2071, 5398), new Point(2088, 10702), new Point(2878, 10382), new Point(3289, 9293), new Point(5589, 7887) };
            Regions["topLeftInnerJungle"] = pts;

            pts = new Point[] { new Point(6427, 7629), new Point(4693, 8805), new Point(3427, 9600), new Point(2410, 11629), new Point(3006, 12325), new Point(7340, 8331) };
            Regions["topOuterRiver"] = pts;

            pts = new Point[] { new Point(6217, 8077), new Point(5287, 8507), new Point(4440, 8988), new Point(3408, 9699), new Point(2667, 11359), new Point(3227, 11953), new Point(6886, 8668) };
            Regions["topInnerRiver"] = pts;

            pts = new Point[] { new Point(7417, 8209), new Point(5629, 9663), new Point(5425, 11054), new Point(4078, 11153), new Point(3111, 12709), new Point(6631, 12986), new Point(9777, 12970), new Point(10290, 11155) };
            Regions["topRightOuterJungle"] = pts;

            pts = new Point[] { new Point(7129, 9365), new Point(6319, 10046), new Point(5794, 10160), new Point(5435, 11144), new Point(4507, 11371), new Point(3916, 12150), new Point(7202, 12168), new Point(9002, 12524), new Point(9122, 10553), new Point(8205, 9990), new Point(8021, 9111) };
            Regions["topRightInnerJungle"] = pts;

            pts = new Point[] { new Point(4485, 3800), new Point(7368, 6600), new Point(9245, 5131), new Point(9247, 3949), new Point(10707, 3730), new Point(11388, 1980), new Point(10492, 1801), new Point(4938, 1780) };
            Regions["bottomLeftOuterJungle"] = pts;

            pts = new Point[] { new Point(5132, 2358), new Point(4963, 3448), new Point(6850, 5663), new Point(7499, 5798), new Point(9151, 4810), new Point(9254, 4056), new Point(10663, 3012), new Point(10421, 2489) };
            Regions["bottomLeftInnerJungle"] = pts;

            pts = new Point[] { new Point(11752, 2728), new Point(9485, 3968), new Point(9072, 5126), new Point(8449, 5828), new Point(7462, 6567), new Point(8327, 7223), new Point(9692, 6463), new Point(10907, 5673), new Point(12552, 3442) };
            Regions["bottomOuterRiver"] = pts;

            pts = new Point[] { new Point(11236, 3200), new Point(10513, 4361), new Point(9961, 3480), new Point(9110, 4326), new Point(9455, 5250), new Point(7947, 6202), new Point(8742, 6731), new Point(10137, 6099), new Point(11429, 5293), new Point(12349, 3902) };
            Regions["bottomInnerRiver"] = pts;

            pts = new Point[] { new Point(13014, 4103), new Point(12029, 4416), new Point(11447, 5317), new Point(8192, 7207), new Point(11118, 10396), new Point(13061, 9911) };
            Regions["bottomRightOuterJungle"] = pts;

            pts = new Point[] { new Point(12491, 4049), new Point(11457, 5246), new Point(11553, 5671), new Point(10388, 6316), new Point(8881, 7164), new Point(11362, 9869), new Point(12550, 9567), new Point(12585, 6884), new Point(12956, 6405) };
            Regions["bottomRightInnerJungle"] = pts;

            pts = new Point[] { new Point(3297, 4261), new Point(5930, 6897), new Point(6895, 6141), new Point(4112, 3575) };
            Regions["leftMidLane"] = pts;

            pts = new Point[] { new Point(5930, 6897), new Point(7987, 8832), new Point(9112, 7958), new Point(6895, 6141) };
            Regions["centerMidLane"] = pts;

            pts = new Point[] { new Point(9112, 7958), new Point(7987, 8832), new Point(10631, 11341), new Point(11361, 10869) };
            Regions["rightMidLane"] = pts;

            pts = new Point[] { new Point(4502, 492), new Point(4486, 1784), new Point(11218, 1953), new Point(12183, 485) };
            Regions["leftBotLane"] = pts;

            pts = new Point[] { new Point(12183, 485), new Point(11218, 1953), new Point(12552, 3442), new Point(14283, 2620) };
            Regions["centerBotLane"] = pts;

            pts = new Point[] { new Point(14283, 2620), new Point(12552, 3442), new Point(12997, 3971), new Point(13048, 10432), new Point(14580, 10329) };
            Regions["rightBotLane"] = pts;

            pts = new Point[] { new Point(23, 4744), new Point(104, 12521), new Point(1967, 11326), new Point(1719, 4564) };
            Regions["leftTopLane"] = pts;

            pts = new Point[] { new Point(104, 12521), new Point(3332, 14683), new Point(3620, 12813), new Point(1967, 11326) };
            Regions["centerTopLane"] = pts;

            pts = new Point[] { new Point(3620, 12813), new Point(3332, 14683), new Point(10295, 14390), new Point(10261, 13162), new Point(4284, 13087) };
            Regions["rightTopLane"] = pts;
        }

        #region Utilities
        public static void DrawRegion(String region, System.Drawing.Color color, int width = 2)
        {
            Point[] polygon = Regions[region];
            for (var i = 0; i <= polygon.Length - 1; i++)
            {
                var nextIndex = (polygon.Length - 1 == i) ? 0 : (i + 1);
                Vector2 start = Drawing.WorldToScreen(new Vector3(polygon[i].X, polygon[i].Y, 50));
                Vector2 end = Drawing.WorldToScreen(new Vector3(polygon[nextIndex].X, polygon[nextIndex].Y, 50));
                Drawing.DrawLine(start, end, width, color);
            }
        }

        //credits to http://www.angusj.com/delphi/clipper.php
        //code taken from ClipperLib
        private static int PointInPolygon(Point[] path, Vector3 pt)
        {
            //returns 0 if false, +1 if true, -1 if pt ON polygon boundary
            //See "The Point in Polygon Problem for Arbitrary Polygons" by Hormann & Agathos
            //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.88.5498&rep=rep1&type=pdf
            int result = 0, cnt = path.Length;
            if (cnt < 3) return 0;
            Point ip = path[0];
            for (int i = 1; i <= cnt; ++i)
            {
                Point ipNext = (i == cnt ? path[0] : path[i]);
                if (ipNext.Y == pt.Y)
                {
                    if ((ipNext.X == pt.X) || (ip.Y == pt.Y &&
                      ((ipNext.X > pt.X) == (ip.X < pt.X)))) return -1;
                }
                if ((ip.Y < pt.Y) != (ipNext.Y < pt.Y))
                {
                    if (ip.X >= pt.X)
                    {
                        if (ipNext.X > pt.X) result = 1 - result;
                        else
                        {
                            double d = (double)(ip.X - pt.X) * (ipNext.Y - pt.Y) -
                              (double)(ipNext.X - pt.X) * (ip.Y - pt.Y);
                            if (d == 0) return -1;
                            else if ((d > 0) == (ipNext.Y > ip.Y)) result = 1 - result;
                        }
                    }
                    else
                    {
                        if (ipNext.X > pt.X)
                        {
                            double d = (double)(ip.X - pt.X) * (ipNext.Y - pt.Y) -
                              (double)(ipNext.X - pt.X) * (ip.Y - pt.Y);
                            if (d == 0) return -1;
                            else if ((d > 0) == (ipNext.Y > ip.Y)) result = 1 - result;
                        }
                    }
                }
                ip = ipNext;
            }
            return result;
        }

        private static bool IsPointInPolygon(Point[] path, Vector3 pt)
        {
            if (PointInPolygon(path, pt) == 0)
                return false;
            else
                return true;
        }
        #endregion

        #region River
        public static bool inRiver(Obj_AI_Base unit)
        {
            return (inTopRiver(unit) || inBottomRiver(unit));
        }

        public static bool inTopRiver(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["topOuterRiver"], unit.Position);
        }

        public static bool inTopInnerRiver(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["topInnerRiver"], unit.Position);
        }

        public static bool inTopOuterRiver(Obj_AI_Base unit)
        {
            return inTopRiver(unit) && !inTopInnerRiver(unit); ;
        }

        public static bool inBottomRiver(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["bottomOuterRiver"], unit.Position);
        }

        public static bool inBottomInnerRiver(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["bottomInnerRiver"], unit.Position);
        }

        public static bool inBottomOuterRiver(Obj_AI_Base unit)
        {
            return inBottomRiver(unit) && !inBottomInnerRiver(unit);
        }

        public static bool inOuterRiver(Obj_AI_Base unit)
        {
            return (inTopOuterRiver(unit) || inBottomOuterRiver(unit));
        }

        public static bool inInnerRiver(Obj_AI_Base unit)
        {
            return (inTopInnerRiver(unit) || inBottomInnerRiver(unit));
        }
        #endregion



        #region Base
        public static bool inBase(Obj_AI_Base unit)
        {
            return (!onLane(unit) && !inJungle(unit) && !inRiver(unit));
        }

        public static bool inLeftBase(Obj_AI_Base unit)
        {
            return (inBase(unit) && unit.Distance(new Vector3(50, 0, 285)) < 6000);
        }

        public static bool inRightBase(Obj_AI_Base unit)
        {
            return (inBase(unit) && unit.Distance(new Vector3(50, 0, 285)) > 6000);
        }
        #endregion



        #region Lane
        public static bool onLane(Obj_AI_Base unit)
        {
            return (onTopLane(unit) || onMidLane(unit) || onBotLane(unit));
        }

        public static bool onTopLane(Obj_AI_Base unit)
        {
            return (IsPointInPolygon(Regions["leftTopLane"], unit.Position) || IsPointInPolygon(Regions["centerTopLane"], unit.Position) || IsPointInPolygon(Regions["rightTopLane"], unit.Position));
        }

        public static bool onMidLane(Obj_AI_Base unit)
        {
            return (IsPointInPolygon(Regions["leftMidLane"], unit.Position) || IsPointInPolygon(Regions["centerMidLane"], unit.Position) || IsPointInPolygon(Regions["rightMidLane"], unit.Position));
        }

        public static bool onBotLane(Obj_AI_Base unit)
        {
            return (IsPointInPolygon(Regions["leftBotLane"], unit.Position) || IsPointInPolygon(Regions["centerBotLane"], unit.Position) || IsPointInPolygon(Regions["rightBotLane"], unit.Position));
        }
        #endregion



        #region Jungle
        public static bool inJungle(Obj_AI_Base unit)
        {
            return (inLeftJungle(unit) || inRightJungle(unit));
        }

        public static bool inOuterJungle(Obj_AI_Base unit)
        {
            return (inLeftOuterJungle(unit) || inRightOuterJungle(unit));
        }

        public static bool inInnerJungle(Obj_AI_Base unit)
        {
            return (inLeftInnerJungle(unit) || inRightInnerJungle(unit));
        }

        public static bool inLeftJungle(Obj_AI_Base unit)
        {
            return (inTopLeftJungle(unit) || inBottomLeftJungle(unit));
        }

        public static bool inLeftOuterJungle(Obj_AI_Base unit)
        {
            return (inTopLeftOuterJungle(unit) || inBottomLeftOuterJungle(unit));
        }

        public static bool inLeftInnerJungle(Obj_AI_Base unit)
        {
            return (inTopLeftInnerJungle(unit) || inBottomLeftInnerJungle(unit));
        }

        public static bool inTopLeftJungle(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["topLeftOuterJungle"], unit.Position);
        }

        public static bool inTopLeftOuterJungle(Obj_AI_Base unit)
        {
            return (inTopLeftJungle(unit) && !inTopLeftInnerJungle(unit));
        }

        public static bool inTopLeftInnerJungle(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["topLeftInnerJungle"], unit.Position);
        }

        public static bool inBottomLeftJungle(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["bottomLeftOuterJungle"], unit.Position);
        }

        public static bool inBottomLeftOuterJungle(Obj_AI_Base unit)
        {
            return (inBottomLeftJungle(unit) && !inBottomLeftInnerJungle(unit));
        }

        public static bool inBottomLeftInnerJungle(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["bottomLeftInnerJungle"], unit.Position);
        }

        public static bool inRightJungle(Obj_AI_Base unit)
        {
            return (inTopRightJungle(unit) || inBottomRightJungle(unit));
        }

        public static bool inRightOuterJungle(Obj_AI_Base unit)
        {
            return (inTopRightOuterJungle(unit) || inBottomRightOuterJungle(unit));
        }

        public static bool inRightInnerJungle(Obj_AI_Base unit)
        {
            return (inTopRightInnerJungle(unit) || inBottomRightInnerJungle(unit));
        }

        public static bool inTopRightJungle(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["topRightOuterJungle"], unit.Position);
        }

        public static bool inTopRightOuterJungle(Obj_AI_Base unit)
        {
            return (inTopRightJungle(unit) && !inTopRightInnerJungle(unit));
        }

        public static bool inTopRightInnerJungle(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["topRightInnerJungle"], unit.Position);
        }

        public static bool inBottomRightJungle(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["bottomRightOuterJungle"], unit.Position);
        }

        public static bool inBottomRightOuterJungle(Obj_AI_Base unit)
        {
            return (inBottomRightJungle(unit) && !inBottomRightInnerJungle(unit));
        }

        public static bool inBottomRightInnerJungle(Obj_AI_Base unit)
        {
            return IsPointInPolygon(Regions["bottomRightInnerJungle"], unit.Position);
        }

        public static bool inTopJungle(Obj_AI_Base unit)
        {
            return (inTopLeftJungle(unit) || inTopRightJungle(unit));
        }

        public static bool inTopOuterJungle(Obj_AI_Base unit)
        {
            return (inTopLeftOuterJungle(unit) || inTopRightOuterJungle(unit));
        }

        public static bool inTopInnerJungle(Obj_AI_Base unit)
        {
            return (inTopLeftInnerJungle(unit) || inTopRightInnerJungle(unit));
        }

        public static bool inBottomJungle(Obj_AI_Base unit)
        {
            return (inBottomLeftJungle(unit) || inBottomRightJungle(unit));
        }

        public static bool inBottomOuterJungle(Obj_AI_Base unit)
        {
            return (inBottomLeftOuterJungle(unit) || inBottomRightOuterJungle(unit));
        }

        public static bool inBottomInnerJungle(Obj_AI_Base unit)
        {
            return (inBottomLeftInnerJungle(unit) || inBottomRightInnerJungle(unit));
        }
        #endregion
    }
}
