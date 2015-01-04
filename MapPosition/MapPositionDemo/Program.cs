using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;


namespace MapPositionDemo
{
    public class Program
    {
        private static bool InJungle = false;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            MapPosition.MapPosition.Initialize();

            Drawing.OnDraw += OnDraw;
            Game.OnGameUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (MapPosition.MapPosition.inJungle(ObjectManager.Player))
                InJungle = true;
            else if (InJungle)
                InJungle = false;
        }

        private static void OnDraw(EventArgs args)
        {
            MapPosition.MapPosition.DrawRegion("topLeftOuterJungle", System.Drawing.Color.Crimson);
            MapPosition.MapPosition.DrawRegion("topLeftInnerJungle", System.Drawing.Color.DarkOrange);
            MapPosition.MapPosition.DrawRegion("leftMidLane", System.Drawing.Color.DodgerBlue);
            MapPosition.MapPosition.DrawRegion("centerMidLane", System.Drawing.Color.DeepSkyBlue);
            MapPosition.MapPosition.DrawRegion("rightMidLane", System.Drawing.Color.DodgerBlue);
            MapPosition.MapPosition.DrawRegion("bottomOuterRiver", System.Drawing.Color.Fuchsia);
            MapPosition.MapPosition.DrawRegion("bottomInnerRiver", System.Drawing.Color.HotPink);

            if (InJungle)
            {
                Vector2 pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                Drawing.DrawText(pos.X, pos.Y, System.Drawing.Color.DeepPink, "In Jungle");
            }
        }
    }
}
