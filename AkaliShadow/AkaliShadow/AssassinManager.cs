using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace AkaliShadow
{
    internal class AssassinManager
    {
        public AssassinManager()
        {
            Load();
        }

        private static void Load()
        {
            Program.targetSelectorMenu.AddSubMenu(new Menu("Assassin Manager", "MenuAssassin"));
            Program.targetSelectorMenu.SubMenu("MenuAssassin").AddItem(new MenuItem("AssassinActive", "Active").SetValue(true));
            Program.targetSelectorMenu.SubMenu("MenuAssassin").AddItem(new MenuItem("Ax", ""));
            Program.targetSelectorMenu.SubMenu("MenuAssassin").AddItem(new MenuItem("AssassinSelectOption", "Set: ").SetValue(new StringList(new[] { "Single Select", "Multi Select" })));
            Program.targetSelectorMenu.SubMenu("MenuAssassin").AddItem(new MenuItem("Ax", ""));
            Program.targetSelectorMenu.SubMenu("MenuAssassin").AddItem(new MenuItem("AssassinSetClick", "Add/Remove with Right-Click").SetValue(true));
            Program.targetSelectorMenu.SubMenu("MenuAssassin").AddItem(new MenuItem("AssassinReset", "Reset List").SetValue(new KeyBind("L".ToCharArray()[0], KeyBindType.Press)));

            Program.targetSelectorMenu.SubMenu("MenuAssassin").AddSubMenu(new Menu("Draw:", "Draw"));

            Program.targetSelectorMenu.SubMenu("MenuAssassin").SubMenu("Draw").AddItem(new MenuItem("DrawSearch", "Search Range").SetValue(new Circle(false, Color.Gray)));
            Program.targetSelectorMenu.SubMenu("MenuAssassin").SubMenu("Draw").AddItem(new MenuItem("DrawActive", "Active Enemy").SetValue(new Circle(true, Color.GreenYellow)));
            Program.targetSelectorMenu.SubMenu("MenuAssassin").SubMenu("Draw").AddItem(new MenuItem("DrawNearest", "Nearest Enemy").SetValue(new Circle(false, Color.DarkSeaGreen)));


            Program.targetSelectorMenu.SubMenu("MenuAssassin").AddSubMenu(new Menu("Assassin List:", "AssassinMode"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                Program.targetSelectorMenu.SubMenu("MenuAssassin")
                    .SubMenu("AssassinMode")
                    .AddItem(
                        new MenuItem("Assassin" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(
                            TargetSelector.GetPriority(enemy) > 3));
            }
            Program.targetSelectorMenu.SubMenu("MenuAssassin")
                .AddItem(new MenuItem("AssassinSearchRange", "Search Range")).SetValue(new Slider(1000, 2000));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        static void ClearAssassinList()
        {
            foreach (
                var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {

                Program.targetSelectorMenu.Item("Assassin" + enemy.BaseSkinName).SetValue(false);

            }
        }
        private static void OnGameUpdate(EventArgs args)
        {
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {

            if (Program.targetSelectorMenu.Item("AssassinReset").GetValue<KeyBind>().Active && args.Msg == 257)
            {
                ClearAssassinList();
                Game.PrintChat(
                    "<font color='#FFFFFF'>Reset Assassin List is Complete! Click on the enemy for Add/Remove.</font>");
            }

            if (args.Msg != 0x201)
            {
                return;
            }

            if (Program.targetSelectorMenu.Item("AssassinSetClick").GetValue<bool>())
            {
                foreach (var objAiHero in from hero in ObjectManager.Get<Obj_AI_Hero>()
                                          where hero.IsValidTarget()
                                          select hero
                                              into h
                                              orderby h.Distance(Game.CursorPos) descending
                                              select h
                                                  into enemy
                                                  where enemy.Distance(Game.CursorPos) < 150f
                                                  select enemy)
                {
                    if (objAiHero != null && objAiHero.IsVisible && !objAiHero.IsDead)
                    {
                        var xSelect =
                            Program.targetSelectorMenu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex;

                        switch (xSelect)
                        {
                            case 0:
                                ClearAssassinList();
                                Program.targetSelectorMenu.Item("Assassin" + objAiHero.BaseSkinName).SetValue(true);
                                Game.PrintChat(
                                    string.Format(
                                        "<font color='FFFFFF'>Added to Assassin List</font> <font color='#09F000'>{0} ({1})</font>",
                                        objAiHero.Name, objAiHero.BaseSkinName));
                                break;
                            case 1:
                                var menuStatus = Program.targetSelectorMenu.Item("Assassin" + objAiHero.BaseSkinName).GetValue<bool>();
                                Program.targetSelectorMenu.Item("Assassin" + objAiHero.BaseSkinName).SetValue(!menuStatus);
                                Game.PrintChat(
                                    string.Format("<font color='{0}'>{1}</font> <font color='#09F000'>{2} ({3})</font>",
                                        !menuStatus ? "#FFFFFF" : "#FF8877",
                                        !menuStatus ? "Added to Assassin List:" : "Removed from Assassin List:",
                                        objAiHero.Name, objAiHero.BaseSkinName));
                                break;
                        }
                    }
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Program.targetSelectorMenu.Item("AssassinActive").GetValue<bool>())
                return;

            var drawSearch = Program.targetSelectorMenu.Item("DrawSearch").GetValue<Circle>();
            var drawActive = Program.targetSelectorMenu.Item("DrawActive").GetValue<Circle>();
            var drawNearest = Program.targetSelectorMenu.Item("DrawNearest").GetValue<Circle>();

            var drawSearchRange = Program.targetSelectorMenu.Item("AssassinSearchRange").GetValue<Slider>().Value;
            if (drawSearch.Active)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, drawSearchRange, drawSearch.Color);
            }

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(enemy => enemy.Team != ObjectManager.Player.Team)
                        .Where(
                            enemy =>
                                enemy.IsVisible &&
                                Program.targetSelectorMenu.Item("Assassin" + enemy.BaseSkinName) != null &&
                                !enemy.IsDead)
                        .Where(
                            enemy => Program.targetSelectorMenu.Item("Assassin" + enemy.BaseSkinName).GetValue<bool>()))
            {
                if (ObjectManager.Player.Distance(enemy.ServerPosition) < drawSearchRange)
                {
                    if (drawActive.Active)
                        Utility.DrawCircle(enemy.Position, 85f, drawActive.Color);
                }
                else if (ObjectManager.Player.Distance(enemy.ServerPosition) > drawSearchRange &&
                         ObjectManager.Player.Distance(enemy.ServerPosition) < drawSearchRange + 400)
                {
                    if (drawNearest.Active)
                        Utility.DrawCircle(enemy.Position, 85f, drawNearest.Color);
                }
            }
        }
    }
}