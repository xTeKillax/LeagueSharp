using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;



//Credits: Esk0r, princer007, xQx, jackisback, WorstPing, InjectionDev

namespace AkaliShadow
{
    internal class Program
    {
        #region Variables
        public const string ChampionName = "Akali";
        private static readonly Obj_AI_Hero MyHero = ObjectManager.Player;

        private static Spell _q, _w, _e, _r;
        private static List<Spell> _spellList;
        private static Items.Item _hex, _dfg, _bwC;


        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;
        public static Menu TargetSelectorMenu;
        public static LevelUpManager LevelUpManager;

        private const bool PacketCast = false;
        private static bool _qInAir = true;
        private static bool _wCountdown;
        private static bool _drawWspots;
        private static List<Vector3> _wardSpots;
        private const int SpotMagnetRadius = 125;
        private static int _wTick;
        private static Render.Text _wCountdownText;

        //private static System.IO.StreamWriter debug_output;
        #endregion

        #region GameFunction
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (MyHero.ChampionName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 600);
            _w = new Spell(SpellSlot.W, 700);
            _e = new Spell(SpellSlot.E, 325);
            _r = new Spell(SpellSlot.R, 800);

            _hex = new Items.Item(3146, 700);
            _dfg = new Items.Item(3128, 750);
            _bwC = new Items.Item(3144, 450);

            MyHero.GetSpellSlot("SummonerDot");

            _spellList = new List<Spell>() { _q, _w, _e, _r };

            _wCountdownText = new Render.Text("", new Vector2(0, 0), 25, SharpDX.Color.LightYellow, "Impact");

            InitializeLevelUpManager();

            (Config = new Menu("Akali Shadow", ChampionName, true)).AddToMainMenu();

            TargetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(TargetSelectorMenu);
            Config.AddSubMenu(TargetSelectorMenu);

            //[Orbwalker]
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //[Combo]
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Rdelay", "Delay Between R").SetValue(new Slider(0, 0, 2000)));

            //[Harass]
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(
            new KeyBind('T', KeyBindType.Press)));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(
            new KeyBind('Y', KeyBindType.Toggle)));

            //[Farm]
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 2)));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 1)));
            Config.SubMenu("Farm").AddItem(new MenuItem("hitCounter", "Use E if will hit min").SetValue(new Slider(3, 1, 6)));

            //[Drawings]
            Config.AddSubMenu(new Menu("Drawings", "Drawing"));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Qrange", "Q Range").SetValue(new Circle(true, Color.FromArgb(255, Color.SkyBlue))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Wrange", "W Range").SetValue(new Circle(false, Color.FromArgb(150, Color.IndianRed))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Erange", "E Range").SetValue(new Circle(false, Color.FromArgb(150, Color.LimeGreen))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Rrange", "R Range").SetValue(new Circle(true, Color.FromArgb(255, Color.Black))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("spotColor", "W spot circle").SetValue(new Circle(false, Color.FromArgb(200, Color.Crimson))));
            MenuItem fullComboDamageItem = Config.SubMenu("Drawing").AddItem(new MenuItem("FullComboDraw", "Draw fullCombo damage").SetValue(true));
            Config.SubMenu("Drawing").AddItem(new MenuItem("wCountdown", "Draw W countdown").SetValue(true));

            //[Misc]
            Menu menu_misc = Config.AddSubMenu(new Menu("Misc", "Misc"));
            LevelUpManager.AddToMenu(ref menu_misc);
            Config.SubMenu("Misc").AddItem(new MenuItem("wSpotActive", "W perfect spot (press once and left click)").SetValue(
            new KeyBind('W', KeyBindType.Press)));
            Config.SubMenu("Misc").AddItem(new MenuItem("antiGapCloser", "Use W on gapcloser")).SetValue(new StringList(new[] { "Targeted only", "Skillshot only", "Both", "Off" }, 1));
            Config.SubMenu("Misc").AddItem(new MenuItem("flee", "Auto flee")).SetValue(new KeyBind('H', KeyBindType.Press));

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = fullComboDamageItem.GetValue<bool>();
            fullComboDamageItem.ValueChanged +=
            delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            new AssassinManager();
            Orbwalker.SetAttack(true);
            Orbwalker.SetMovement(true);

            InitializeEvadeSpots();

            //Game event callback
            Game.OnGameUpdate += OnUpdate;
            Game.OnWndProc += OnWndProc;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += OnCreateObj;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            //debug_output = new System.IO.StreamWriter("c:\\AkaliShadow.log");
            Game.PrintChat("<font color = \"#6B9FE3\">Akali Shadow</font><font color = \"#E3AF6B\"> by BestAkaliAfrica</font>.");
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            //0=targeted 1=skillshot 2=both 4=no
            var gapcloserType = Config.SubMenu("Misc").Item("antiGapCloser").GetValue<StringList>().SelectedIndex;

            if (gapcloserType == 4)
                return;

            if (!_w.IsReady())
                return;

            if ((gapcloser.SkillType == GapcloserType.Targeted && gapcloserType == 0 || gapcloserType == 2) || ((gapcloser.SkillType == GapcloserType.Skillshot && gapcloserType == 1) || gapcloserType == 2 && (MyHero.Position.Distance(gapcloser.End)) <= 600))
                _w.Cast(MyHero, PacketCast);
        }


        
        private static void OnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    var target = GetEnemy;
                    if (target != null)
                        Combo(target);
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Farm();
                    break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm(true);
                    break;
            }
            
            if (Config.SubMenu("Harass").Item("HarassActive").GetValue<KeyBind>().Active || Config.SubMenu("Harass").Item("HarassActiveT").GetValue<KeyBind>().Active)
                Harass();

            if (Config.SubMenu("Misc").Item("flee").GetValue<KeyBind>().Active) 
                Flee();

            LevelUpManager.Update();
        }

        private static void OnWndProc(WndEventArgs args)
        {
            //a key is pressed
            if (args.Msg == (uint)WindowsMessages.WM_KEYDOWN)
            {
                //is the key for wSpotActive ?
                if (args.WParam == Config.SubMenu("Misc").Item("wSpotActive").GetValue<KeyBind>().Key)
                    _drawWspots = true;


                //if (args.WParam == 0x60) //numpad 0
                //{
                //    string line = "_WardSpots.Add(new Vector3(" + Game.CursorPos.X + "f, " + Game.CursorPos.Y + "f, " + Game.CursorPos.Z + "f));";
                //    debug_output.WriteLine(line + "\r\n");
                //    Game.PrintChat(line);
                //}
                //if (args.WParam == 0x61) //numpad 1
                //    debug_output.Close();
            }
            else if (args.Msg == (uint)WindowsMessages.WM_LBUTTONDOWN && _drawWspots)
            {
                _drawWspots = false;

                foreach (Vector3 safeSpot in _wardSpots)
                    if (safeSpot.Distance(Game.CursorPos) <= SpotMagnetRadius)
                        _w.Cast(safeSpot);
            }
            else if ((args.Msg == (uint)WindowsMessages.WM_LBUTTONUP || args.Msg == (uint)WindowsMessages.WM_RBUTTONDOWN) && _drawWspots)
                _drawWspots = false;
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Config.SubMenu("Drawing").Item("wCountdown").GetValue<bool>())
            {
                if (sender.Type == GameObjectType.obj_AI_Hero && sender.NetworkId == MyHero.NetworkId)
                {
                    if (args.SData.Name.Equals("AkaliSmokeBomb"))
                    {
                        _wCountdown = true;
                        _wTick = Environment.TickCount;
                    }
                }
            }
        }

        private static void OnCreateObj(GameObject sender, EventArgs args)
        {
            //Detect whenever our Q land on someone
            if (sender.Name.Contains("akali_markOftheAssasin_marker_tar.troy") && !sender.IsEnemy )
                _qInAir = false;
        }
        #endregion

        #region Graphics
        private static void OnDraw(EventArgs args)
        {
            foreach (var spell in _spellList)
            {
                var menuItem = Config.SubMenu("Drawing").Item(spell.Slot + "range").GetValue<Circle>();
                if (menuItem.Active)
                    Render.Circle.DrawCircle(MyHero.Position, spell.Range, menuItem.Color);
            }

            if (_drawWspots)
            {
                foreach (Vector3 safeSpot in _wardSpots)
                    if(Render.OnScreen(Drawing.WorldToScreen(safeSpot)))
                        Render.Circle.DrawCircle(safeSpot, SpotMagnetRadius, Config.SubMenu("Drawing").Item("spotColor").GetValue<Circle>().Color);
            }

            if(_wCountdown)
            {
                int remainingTime = 8 - ((Environment.TickCount - _wTick) / 1000);
                if (remainingTime > 0)
                {
                    Vector2 drawPos = Drawing.WorldToScreen(MyHero.Position);
                    _wCountdownText.X = (int)drawPos.X;
                    _wCountdownText.Y = (int)drawPos.Y - 20;
                    _wCountdownText.text = remainingTime.ToString();
                    _wCountdownText.OnEndScene();
                }
                else
                    _wCountdown = false;
            }
        }
        #endregion

        #region Mechanics
        private static void Combo(Obj_AI_Hero target)
        {
            double eDamage = MyHero.GetSpellDamage(target, SpellSlot.E);

            CastItems(target);

            //Mark Q on enemy if not marked
            if (_q.IsReady() && target != null && MyHero.Distance(target) <= _q.Range && !HasBuff(target, "AkaliMota") && Config.SubMenu("Combo").Item("UseQCombo").GetValue<bool>())
            {
                _q.Cast(target, PacketCast);
                _qInAir = true;
            }

            //Jump with R if dist > E.Range and have enough energy for R+E
            if (MyHero.Distance(target) <= _r.Range
                && ((MyHero.Distance(target) > _e.Range && (HasEnergyFor(false, true, false, true) || HasBuff(target, "AkaliMota")))
                || (MyHero.GetSpellDamage(target, SpellSlot.R) + MyHero.GetAutoAttackDamage(target, true)) >= target.Health)
                && _r.IsReady() && Config.SubMenu("Combo").Item("UseRCombo").GetValue<bool>() && (_r.LastCastAttemptT + Config.SubMenu("Combo").Item("Rdelay").GetValue<Slider>().Value) <= Environment.TickCount)
            {
                _r.Cast(target, PacketCast);
                _r.LastCastAttemptT = Environment.TickCount;
            }

            if (Config.SubMenu("Combo").Item("UseECombo").GetValue<bool>())
            {
                //Enemy got mark and we have energy to Q+E.
                if (MyHero.Distance(target) <= _e.Range
                    && HasBuff(target, "AkaliMota")
                    && HasEnergyFor(true, false, true, false)
                    && _e.IsReady())
                {
                    _e.Cast(PacketCast);
                }

                //We can kill him with E, w/ or w/o mark/Qenergy
                if (MyHero.Distance(target) <= _e.Range
                    && target.Health <= eDamage
                    && _e.IsReady())
                {
                    _e.Cast(PacketCast);
                }

                //We mark the proc with E and in 2-3 sec we will have enough energy to do Q again.
                if (MyHero.Distance(target) <= _e.Range
                    && HasBuff(target, "AkaliMota")
                    && !HasEnergyFor(true, false, false, false)
                    && _e.IsReady())
                {
                    _e.Cast(PacketCast);
                }

                //No Q going to target, enough energy to do Q+E, we cast E
                if (MyHero.Distance(target) <= _e.Range
                    && _qInAir == false
                    && HasEnergyFor(true, false, true, false)
                    && _e.IsReady())
                {
                    _e.Cast(PacketCast);
                }
            }
        }

        private static void Harass()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);

            if (target != null)
            {
                if (_q.IsReady() && MyHero.Distance(target) <= _q.Range && Config.SubMenu("Harass").Item("UseQHarass").GetValue<bool>())
                {
                    _q.Cast(target, PacketCast);
                    _qInAir = true;
                }

                if (_e.IsReady() && MyHero.Distance(target) <= _e.Range
                    && (HasBuff(target, "AkaliMota") || MyHero.GetSpellDamage(target, SpellSlot.E) >= target.Health) && Config.SubMenu("Harass").Item("UseEHarass").GetValue<bool>())
                {
                    _e.Cast(PacketCast);
                }
            }
        }

        private static void Farm(bool laneClear = false)
        {
            var useQi = Config.SubMenu("Farm").Item("UseQFarm").GetValue<StringList>().SelectedIndex;
            var useEi = Config.SubMenu("Farm").Item("UseEFarm").GetValue<StringList>().SelectedIndex;
            var useQ = (laneClear && (useQi == 1 || useQi == 2)) || (!laneClear && (useQi == 0 || useQi == 2));
            var useE = (laneClear && (useEi == 1 || useEi == 2)) || (!laneClear && (useEi == 0 || useEi == 2));

            foreach (Obj_AI_Base minion in MinionManager.GetMinions(MyHero.Position, _q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health))
            {
                if (useQ && _q.IsReady())
                {
                    //Q kill him or Q+Proc kill him.
                    if (!laneClear && (minion.Health <= _q.GetDamage(minion) || (minion.Health <= (_q.GetDamage(minion) + _q.GetDamage(minion, 1)) && (minion.Health > _q.GetDamage(minion)) && MyHero.Distance(minion) <= Orbwalking.GetRealAutoAttackRange(MyHero)))
                        || laneClear)
                    {
                        _q.Cast(minion, PacketCast);
                    }

                    if (HasBuff(minion, "AkaliMota") && Orbwalking.GetRealAutoAttackRange(MyHero) >= MyHero.Distance(minion))
                        Orbwalker.ForceTarget(minion);
                }

                if (useE && _e.IsReady())
                    if (MyHero.Distance(minion) <= _e.Range)
                        if ((!laneClear && minion.Health <= _e.GetDamage(minion)) || (laneClear && MinionManager.GetMinions(MyHero.Position, _e.Range, MinionTypes.All, MinionTeam.Enemy).Count >= Config.SubMenu("Farm").Item("hitCounter").GetValue<Slider>().Value))
                            _e.Cast(PacketCast);
            }
        }

        static void Flee()
        {
            Vector2 escape_pos = MyHero.Position.Extend(Game.CursorPos, _r.Range).To2D();

            MyHero.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            var creepNear = MinionManager.GetMinions(Game.CursorPos, 300, MinionTypes.All, MinionTeam.NotAlly);
            if (creepNear.Count < 1)
            {
                if (!IsWall(escape_pos) && IsWallBetween(MyHero.Position, escape_pos.To3D()) && MyHero.Distance(Game.CursorPos) < _r.Range)
                    if (_w.IsReady())
                        _w.Cast(MyHero.Position.Extend(Game.CursorPos, _w.Range), PacketCast);
            }
            else
            {
                _r.CastOnUnit(creepNear.FirstOrDefault(), PacketCast);
            }
        }
        #endregion

        #region Utilities
        private static double CalcItemsDmg(Obj_AI_Hero target)
        {
            double result = 0d;
            foreach (var item in MyHero.InventoryItems)
                switch ((int)item.Id)
                {
                    case 3100: // LichBane
                        if (MyHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += MyHero.BaseAttackDamage * 0.75 + MyHero.FlatMagicDamageMod * 0.5;
                        break;
                    case 3057: //Sheen
                        if (MyHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += MyHero.BaseAttackDamage;
                        break;
                    case 3144: //BwC
                        if (MyHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += MyHero.GetItemDamage(target, Damage.DamageItems.Bilgewater);
                        break;
                    case 3146:  //Hex
                        if (MyHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += MyHero.GetItemDamage(target, Damage.DamageItems.Hexgun);
                        break;
                    case 3128:
                        if (MyHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += MyHero.GetItemDamage(target, Damage.DamageItems.Dfg);
                        break;
                }

            return result;
        }

        private static float GetComboDamage(Obj_AI_Hero target)
        {
            double qDamage = MyHero.GetSpellDamage(target, SpellSlot.Q);
            double q2Damage = MyHero.GetSpellDamage(target, SpellSlot.Q, 1);
            double eDamage = MyHero.GetSpellDamage(target, SpellSlot.E);
            double rDamage = MyHero.GetSpellDamage(target, SpellSlot.R);
            double hitDamage = MyHero.GetAutoAttackDamage(target, true);

            double totDmg = 0;

            if (_q.IsReady())
                totDmg += qDamage;

            if (HasBuff(target, "AkaliMota"))
                totDmg += q2Damage + hitDamage;

            if (_e.IsReady())
                totDmg += eDamage;

            if (_r.IsReady())
                totDmg += rDamage;

            totDmg += CalcItemsDmg(target);

            //Dfg damage
            totDmg = MyHero.InventoryItems.
                Where(item => 
                    (int) item.Id == 3128).
                    Where(item => 
                        MyHero.Spellbook.CanUseSpell((SpellSlot) item.Slot) == SpellState.Ready).
                        Aggregate(totDmg, (current, item) => current * 1.2);

            if (HasBuff(target, "deathfiregraspspell"))
                totDmg *= 1.2;

            return (float)totDmg;
        }

        private static void CastItems(Obj_AI_Hero target)
        {
            foreach (var item in MyHero.InventoryItems)
            {
                switch ((int)item.Id)
                {
                    case 3128: //DFG
                        if (MyHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready) _dfg.Cast(target);
                        break;
                    case 3146: //HexTech
                        if (MyHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready) _hex.Cast(target);
                        break;
                    case 3144: //BwC
                        if (MyHero.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready) _bwC.Cast(target);
                        break;
                }
            }
        }

        static bool HasBuff(Obj_AI_Base target, string buffName)
        {
            return target.Buffs.Any(buff => buff.Name == buffName);
        }

        static bool HasEnergyFor(bool q, bool w, bool e, bool r)
        {
            float totalCost = 0;

            if (q)
                totalCost += MyHero.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
            if (w)
                totalCost += MyHero.Spellbook.GetSpell(SpellSlot.W).ManaCost;
            if (e)
                totalCost += MyHero.Spellbook.GetSpell(SpellSlot.E).ManaCost;
            if (r)
                totalCost += MyHero.Spellbook.GetSpell(SpellSlot.R).ManaCost;

            if (MyHero.Mana >= totalCost)
                return true;
            else
                return false;
        }

        static Obj_AI_Hero GetEnemy
        {
            get
            {
                var assassinRange = TargetSelectorMenu.Item("AssassinSearchRange").GetValue<Slider>().Value;
                var vEnemy = ObjectManager.Get<Obj_AI_Hero>().Where(
                enemy => enemy.Team != ObjectManager.Player.Team
                      && !enemy.IsDead && enemy.IsVisible
                      && TargetSelectorMenu.Item("Assassin" + enemy.ChampionName) != null
                      && TargetSelectorMenu.Item("Assassin" + enemy.ChampionName).GetValue<bool>()
                      && ObjectManager.Player.Distance(enemy.ServerPosition) < assassinRange);

                if (TargetSelectorMenu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
                {
                    vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
                }
                Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();
                Obj_AI_Hero t = !objAiHeroes.Any() ? TargetSelector.GetTarget(1400, TargetSelector.DamageType.Magical) : objAiHeroes[0];
                return t;
            }
        }

        private static void InitializeLevelUpManager()
        {

            var priority1 = new int[] { 1, 2, 1, 3, 1, 4, 1, 3, 1, 3, 4, 3, 3, 2, 2, 4, 2, 2 };
            LevelUpManager = new LevelUpManager();
            LevelUpManager.Add("R > Q > E > W ", priority1);
        }

        static bool IsWall(Vector2 pos)
        {
            return (NavMesh.GetCollisionFlags(pos.X, pos.Y) == CollisionFlags.Wall ||
            NavMesh.GetCollisionFlags(pos.X, pos.Y) == CollisionFlags.Building);
        }

        static bool IsWallBetween(Vector3 start, Vector3 end)
        {
            double count = Vector3.Distance(start, end);
            for (uint i = 0; i <= count; i += 10)
            {
                Vector2 pos = start.Extend(end, i).To2D();
                
                if (IsWall(pos)) 
                    return true;
            }

            return false;
        }

        private static void InitializeEvadeSpots()
        {
            _wardSpots = new List<Vector3>
            {
                new Vector3(7451.664f, 6538.447f, 33.74536f),
                new Vector3(8518.179f, 7240.318f, 40.60852f),
                new Vector3(8845.78f, 5213.672f, 33.61487f),
                new Vector3(11504.19f, 5433.52f, 30.58154f),
                new Vector3(11771.57f, 6313.124f, 51.80713f),
                new Vector3(12778.93f, 2197.325f, 51.68604f),
                new Vector3(7475.538f, 3373.856f, 52.57471f),
                new Vector3(3373.385f, 7560.835f, 50.81982f),
                new Vector3(1672.145f, 12495.21f, 52.83826f),
                new Vector3(2200.103f, 12940.9f, 52.83813f),
                new Vector3(4835.833f, 12076.87f, 56.44629f),
                new Vector3(7368.242f, 11604.35f, 51.2417f),
                new Vector3(7337.794f, 8310.077f, 14.54712f),
                new Vector3(6377.225f, 7541.464f, -28.97229f)
            };
        }
        #endregion
    }
}
