using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Color = System.Drawing.Color;



//Credits: Esk0r, princer007, xQx, jackisback

namespace AkaliShadow
{
    internal class Program
    {
        #region Variables
        public const string ChampionName = "Akali";
        private static Obj_AI_Hero myHero = ObjectManager.Player;

        private static Spell Q, W, E, R;
        private static SpellSlot IgniteSlot;
        private static List<Spell> SpellList;
        private static Items.Item Hex, Dfg, BwC;


        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;
        public static Menu targetSelectorMenu;

        private static bool packetCast = false;
        private static bool qInAir = true;
        private static bool wCountdown = false;
        private static bool drawWspots = false;
        private static List<Vector3> _WardSpots;
        private const int SPOT_MAGNET_RADIUS = 125;
        private static int wTick = 0;
        private static Render.Text wCountdownText;

        //private static System.IO.StreamWriter debug_output;
        #endregion

        #region GameFunction
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (myHero.ChampionName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 800);

            Hex = new Items.Item(3146, 700);
            Dfg = new Items.Item(3128, 750);
            BwC = new Items.Item(3144, 450);

            IgniteSlot = myHero.GetSpellSlot("SummonerDot");

            SpellList = new List<Spell>() { Q, W, E, R };

            wCountdownText = new Render.Text("", new Vector2(0, 0), 25, SharpDX.Color.LightYellow, "Impact");

            (Config = new Menu("Akali Shadow", ChampionName, true)).AddToMainMenu();

            targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Rdelay", "Delay Between R").SetValue(new Slider(0, 0, 2000)));


            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(
            new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(
            new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 2)));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 1)));
            Config.SubMenu("Farm").AddItem(new MenuItem("hitCounter", "Use E if will hit min").SetValue(new Slider(3, 1, 6)));

            Config.AddSubMenu(new Menu("Drawings", "Drawing"));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Qrange", "Q Range").SetValue(new Circle(true, Color.FromArgb(255, Color.SkyBlue))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Wrange", "W Range").SetValue(new Circle(false, Color.FromArgb(150, Color.IndianRed))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Erange", "E Range").SetValue(new Circle(false, Color.FromArgb(150, Color.LimeGreen))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Rrange", "R Range").SetValue(new Circle(true, Color.FromArgb(255, Color.Black))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("spotColor", "W spot circle").SetValue(new Circle(false, Color.FromArgb(200, Color.Crimson))));
            MenuItem fullComboDamageItem = Config.SubMenu("Drawing").AddItem(new MenuItem("FullComboDraw", "Draw fullCombo damage").SetValue(true));
            Config.SubMenu("Drawing").AddItem(new MenuItem("wCountdown", "Draw W countdown").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("wSpotActive", "W perfect spot (press once and left click)").SetValue(
            new KeyBind("W".ToCharArray()[0], KeyBindType.Press)));

            Utility.HpBarDamageIndicator.DamageToUnit = getComboDamage;
            Utility.HpBarDamageIndicator.Enabled = fullComboDamageItem.GetValue<bool>();
            fullComboDamageItem.ValueChanged +=
            delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            new AssassinManager();
            Orbwalker.SetAttack(true);
            Orbwalker.SetMovement(true);

            InitializeWardSpots();

            //Game event callback
            Game.OnGameUpdate += OnUpdate;
            Game.OnWndProc += OnWndProc;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += OnCreateObj;
            Drawing.OnDraw += OnDraw;

            //debug_output = new System.IO.StreamWriter("c:\\AkaliShadow.log");
            Game.PrintChat("<font color = \"#6B9FE3\">Akali Shadow</font><font color = \"#E3AF6B\"> by BestAkaliAfrica</font>. You like ? Buy a coffee to Joduskame or me :p");
        }
        
        private static void OnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    var Target = GetEnemy;
                    if (Target != null)
                        Combo(Target);
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Farm();
                    break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm(true);
                    break;
            }

            if (Config.SubMenu("Harass").Item("HarassActive").GetValue<KeyBind>().Active
                || Config.SubMenu("Harass").Item("HarassActiveT").GetValue<bool>())
            {
                Harass();
            }
        }


        private static void OnWndProc(WndEventArgs args)
        {
            //a key is pressed
            if (args.Msg == (uint)WindowsMessages.WM_KEYDOWN)
            {
                //is the key for wSpotActive ?
                if (args.WParam == Config.SubMenu("Misc").Item("wSpotActive").GetValue<KeyBind>().Key)
                    drawWspots = true;


                //if (args.WParam == 0x60) //numpad 0
                //{
                //    string line = "_WardSpots.Add(new Vector3(" + Game.CursorPos.X + "f, " + Game.CursorPos.Y + "f, " + Game.CursorPos.Z + "f));";
                //    debug_output.WriteLine(line + "\r\n");
                //    Game.PrintChat(line);
                //}
                //if (args.WParam == 0x61) //numpad 1
                //    debug_output.Close();
            }
            else if (args.Msg == (uint)WindowsMessages.WM_LBUTTONDOWN && drawWspots)
            {
                drawWspots = false;

                foreach (Vector3 safeSpot in _WardSpots)
                    if (safeSpot.Distance(Game.CursorPos) <= SPOT_MAGNET_RADIUS)
                        W.Cast(safeSpot);
            }
            else if ((args.Msg == (uint)WindowsMessages.WM_LBUTTONUP || args.Msg == (uint)WindowsMessages.WM_RBUTTONDOWN) && drawWspots)
                drawWspots = false;
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Config.SubMenu("Drawing").Item("wCountdown").GetValue<bool>())
            {
                if (sender.Type == GameObjectType.obj_AI_Hero && sender.NetworkId == myHero.NetworkId)
                {
                    if (args.SData.Name.Equals("AkaliSmokeBomb"))
                    {
                        wCountdown = true;
                        wTick = Environment.TickCount;
                    }
                }
            }
        }

        private static void OnCreateObj(GameObject sender, EventArgs args)
        {
            //Detect whenever our Q land on someone
            if (sender.Name.Contains("akali_markOftheAssasin_marker_tar.troy") && !sender.IsEnemy )
                qInAir = false;
        }
        #endregion

        #region Graphics
        private static void OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.SubMenu("Drawing").Item(spell.Slot + "range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(myHero.Position, spell.Range, menuItem.Color);
            }

            if (drawWspots)
            {
                foreach (Vector3 safeSpot in _WardSpots)
                    if(Render.OnScreen(Drawing.WorldToScreen(safeSpot)))
                        Utility.DrawCircle(safeSpot, SPOT_MAGNET_RADIUS, Config.SubMenu("Drawing").Item("spotColor").GetValue<Circle>().Color);
            }

            if(wCountdown)
            {
                int remainingTime = 9 - ((Environment.TickCount - wTick) / 1000);
                if (remainingTime > 0)
                {
                    Vector2 drawPos = Drawing.WorldToScreen(myHero.Position);
                    wCountdownText.X = (int)drawPos.X;
                    wCountdownText.Y = (int)drawPos.Y - 20;
                    wCountdownText.text = remainingTime.ToString();
                    wCountdownText.OnEndScene();
                }
                else
                    wCountdown = false;
            }
        }
        #endregion

        #region Mechanics
        private static void Combo(Obj_AI_Hero Target)
        {
            //var Target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            double eDamage = Damage.GetSpellDamage(myHero, Target, SpellSlot.E);

            CastItems(Target);

            //Mark Q on enemy if not marked
            if (Q.IsReady() && Target != null && myHero.Distance(Target) <= Q.Range && !HasBuff(Target, "AkaliMota") && Config.SubMenu("Combo").Item("UseQCombo").GetValue<bool>())
            {
                Q.Cast(Target, packetCast);
                qInAir = true;
            }

            //Jump with R if dist > E.Range and have enough energy for R+E
            if (myHero.Distance(Target) <= R.Range
                && ((myHero.Distance(Target) > E.Range && (HasEnergyFor(false, true, false, true) || HasBuff(Target, "AkaliMota")))
                || (Damage.GetSpellDamage(myHero, Target, SpellSlot.R) + myHero.GetAutoAttackDamage(Target, true)) >= Target.Health)
                && R.IsReady() && Config.SubMenu("Combo").Item("UseRCombo").GetValue<bool>() && (R.LastCastAttemptT + Config.SubMenu("Combo").Item("Rdelay").GetValue<Slider>().Value) <= Environment.TickCount)
            {
                R.Cast(Target, packetCast);
                R.LastCastAttemptT = Environment.TickCount;
            }

            if (Config.SubMenu("Combo").Item("UseECombo").GetValue<bool>())
            {
                //Enemy got mark and we have energy to Q+E.
                if (myHero.Distance(Target) <= E.Range
                    && HasBuff(Target, "AkaliMota")
                    && HasEnergyFor(true, false, true, false)
                    && E.IsReady())
                {
                    E.Cast(packetCast);
                }

                //We can kill him with E, w/ or w/o mark/Qenergy
                if (myHero.Distance(Target) <= E.Range
                    && Target.Health <= eDamage
                    && E.IsReady())
                {
                    E.Cast(packetCast);
                }

                //We mark the proc with E and in 2-3 sec we will have enough energy to do Q again.
                if (myHero.Distance(Target) <= E.Range
                    && HasBuff(Target, "AkaliMota")
                    && !HasEnergyFor(true, false, false, false)
                    && E.IsReady())
                {
                    E.Cast(packetCast);
                }

                //No Q going to target, enough energy to do Q+E, we cast E
                if (myHero.Distance(Target) <= E.Range
                    && qInAir == false
                    && HasEnergyFor(true, false, true, false)
                    && E.IsReady())
                {
                    E.Cast(packetCast);
                }
            }
        }

        private static void Harass()
        {
            Obj_AI_Hero Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (Target != null)
            {
                if (Q.IsReady() && myHero.Distance(Target) <= Q.Range && Config.SubMenu("Harass").Item("UseQHarass").GetValue<bool>())
                {
                    Q.Cast(Target, packetCast);
                    qInAir = true;
                }

                if (E.IsReady() && myHero.Distance(Target) <= E.Range
                    && (HasBuff(Target, "AkaliMota") || Damage.GetSpellDamage(myHero, Target, SpellSlot.E) >= Target.Health) && Config.SubMenu("Harass").Item("UseEHarass").GetValue<bool>())
                {
                    E.Cast(packetCast);
                }
            }
        }

        private static void Farm(bool laneClear = false)
        {
            var useQi = Config.SubMenu("Farm").Item("UseQFarm").GetValue<StringList>().SelectedIndex;
            var useEi = Config.SubMenu("Farm").Item("UseEFarm").GetValue<StringList>().SelectedIndex;
            var useQ = (laneClear && (useQi == 1 || useQi == 2)) || (!laneClear && (useQi == 0 || useQi == 2));
            var useE = (laneClear && (useEi == 1 || useEi == 2)) || (!laneClear && (useEi == 0 || useEi == 2));

            foreach (Obj_AI_Base minion in MinionManager.GetMinions(myHero.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health))
            {
                if (useQ && Q.IsReady())
                {
                    //Q kill him or Q+Proc kill him.
                    if (!laneClear && (minion.Health <= Q.GetDamage(minion) || (minion.Health <= (Q.GetDamage(minion) + Q.GetDamage(minion, 1)) && (minion.Health > Q.GetDamage(minion)) && myHero.Distance(minion) <= Orbwalking.GetRealAutoAttackRange(myHero)))
                        || laneClear)
                    {
                        Q.Cast(minion, packetCast);
                    }

                    if (HasBuff(minion, "AkaliMota") && Orbwalking.GetRealAutoAttackRange(myHero) >= myHero.Distance(minion))
                        Orbwalker.ForceTarget(minion);
                }

                if (useE && E.IsReady())
                    if (myHero.Distance(minion) <= E.Range)
                        if ((!laneClear && minion.Health <= E.GetDamage(minion)) || (laneClear && MinionManager.GetMinions(myHero.Position, E.Range, MinionTypes.All, MinionTeam.Enemy).Count >= Config.SubMenu("Farm").Item("hitCounter").GetValue<Slider>().Value))
                            E.Cast(packetCast);
            }
        }
        #endregion

        #region Utilities
        private static double CalcItemsDmg(Obj_AI_Hero Target)
        {
            double result = 0d;
            foreach (var item in myHero.InventoryItems)
                switch ((int)item.Id)
                {
                    case 3100: // LichBane
                        if (myHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += myHero.BaseAttackDamage * 0.75 + myHero.FlatMagicDamageMod * 0.5;
                        break;
                    case 3057: //Sheen
                        if (myHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += myHero.BaseAttackDamage;
                        break;
                    case 3144: //BwC
                        if (myHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += myHero.GetItemDamage(Target, Damage.DamageItems.Bilgewater);
                        break;
                    case 3146:  //Hex
                        if (myHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += myHero.GetItemDamage(Target, Damage.DamageItems.Hexgun);
                        break;
                    case 3128:
                        if (myHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += myHero.GetItemDamage(Target, Damage.DamageItems.Dfg);
                        break;
                }

            return result;
        }

        private static float getComboDamage(Obj_AI_Hero Target)
        {
            double qDamage = Damage.GetSpellDamage(myHero, Target, SpellSlot.Q);
            double q2Damage = Damage.GetSpellDamage(myHero, Target, SpellSlot.Q, 1);
            double wDamage = Damage.GetSpellDamage(myHero, Target, SpellSlot.W);
            double eDamage = Damage.GetSpellDamage(myHero, Target, SpellSlot.E);
            double rDamage = Damage.GetSpellDamage(myHero, Target, SpellSlot.R);
            double hitDamage = Damage.GetAutoAttackDamage(myHero, Target, true);

            double totDmg = 0;

            if (Q.IsReady())
                totDmg += qDamage;

            if (HasBuff(Target, "AkaliMota"))
                totDmg += q2Damage + hitDamage;

            if (E.IsReady())
                totDmg += eDamage;

            if (R.IsReady())
                totDmg += rDamage;

            totDmg += CalcItemsDmg(Target);

            //Dfg damage
            foreach (var item in myHero.InventoryItems)
            {
                if ((int)item.Id == 3128)
                {
                    if (myHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                        totDmg *= 1.2;
                }
            }
            if (HasBuff(Target, "deathfiregraspspell"))
                totDmg *= 1.2;

            return (float)totDmg;
        }

        private static void CastItems(Obj_AI_Hero Target)
        {
            foreach (var item in myHero.InventoryItems)
            {
                switch ((int)item.Id)
                {
                    case 3128: //DFG
                        if (myHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready) Dfg.Cast(Target);
                        break;
                    case 3146: //HexTech
                        if (myHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready) Hex.Cast(Target);
                        break;
                    case 3144: //BwC
                        if (myHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready) BwC.Cast(Target);
                        break;
                }
            }
        }

        static bool HasBuff(Obj_AI_Base target, string buffName)
        {
            foreach (BuffInstance buff in target.Buffs)
                if (buff.Name == buffName) return true;
            return false;
        }

        static bool HasEnergyFor(bool Q, bool W, bool E, bool R)
        {
            float totalCost = 0;

            if (Q)
                totalCost += myHero.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
            if (W)
                totalCost += myHero.Spellbook.GetSpell(SpellSlot.W).ManaCost;
            if (E)
                totalCost += myHero.Spellbook.GetSpell(SpellSlot.E).ManaCost;
            if (R)
                totalCost += myHero.Spellbook.GetSpell(SpellSlot.R).ManaCost;

            if (myHero.Mana >= totalCost)
                return true;
            else
                return false;
        }

        static Obj_AI_Hero GetEnemy
        {
            get
            {
                var assassinRange = targetSelectorMenu.Item("AssassinSearchRange").GetValue<Slider>().Value;
                var vEnemy = ObjectManager.Get<Obj_AI_Hero>().Where(
                enemy => enemy.Team != ObjectManager.Player.Team
                      && !enemy.IsDead && enemy.IsVisible
                      && targetSelectorMenu.Item("Assassin" + enemy.ChampionName) != null
                      && targetSelectorMenu.Item("Assassin" + enemy.ChampionName).GetValue<bool>()
                      && ObjectManager.Player.Distance(enemy.ServerPosition) < assassinRange);

                if (targetSelectorMenu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
                {
                    vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
                }
                Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();
                Obj_AI_Hero t = !objAiHeroes.Any() ? TargetSelector.GetTarget(1400, TargetSelector.DamageType.Magical) : objAiHeroes[0];
                return t;
            }
        }

        private static void InitializeWardSpots()
        {
            _WardSpots = new List<Vector3>();
            _WardSpots.Add(new Vector3(7451.664f, 6538.447f, 33.74536f));
            _WardSpots.Add(new Vector3(8518.179f, 7240.318f, 40.60852f));
            _WardSpots.Add(new Vector3(8845.78f, 5213.672f, 33.61487f));
            _WardSpots.Add(new Vector3(11504.19f, 5433.52f, 30.58154f));
            _WardSpots.Add(new Vector3(11771.57f, 6313.124f, 51.80713f));
            _WardSpots.Add(new Vector3(12778.93f, 2197.325f, 51.68604f));
            _WardSpots.Add(new Vector3(7475.538f, 3373.856f, 52.57471f));
            _WardSpots.Add(new Vector3(3373.385f, 7560.835f, 50.81982f));
            _WardSpots.Add(new Vector3(1672.145f, 12495.21f, 52.83826f));
            _WardSpots.Add(new Vector3(2200.103f, 12940.9f, 52.83813f));
            _WardSpots.Add(new Vector3(4835.833f, 12076.87f, 56.44629f));
            _WardSpots.Add(new Vector3(7368.242f, 11604.35f, 51.2417f));
            _WardSpots.Add(new Vector3(7337.794f, 8310.077f, 14.54712f));
            _WardSpots.Add(new Vector3(6377.225f, 7541.464f, -28.97229f));
        }
        #endregion
    }
}
