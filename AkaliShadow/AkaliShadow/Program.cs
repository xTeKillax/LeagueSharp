using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;


//add some R logic
//red part in damages
//Credits: Esk0r, princer007, 

namespace AkaliShadow
{
    internal class Program
    {
        public const string ChampionName = "Akali";
        public static Obj_AI_Hero myHero = ObjectManager.Player;

        public static Spell Q, W, E, R;
        public static SpellSlot IgniteSlot;
        public static List<Spell> SpellList;
        public static Items.Item Hex, Dfg, BwC;


        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;

        static bool packetCast = false;
        public static bool qInAir = true;


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

            (Config = new Menu("Best Akali Africa", ChampionName, true)).AddToMainMenu();

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));


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
            /*Config.SubMenu("Farm").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind(Config.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));*/


            Config.AddSubMenu(new Menu("Drawings", "Drawing"));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Qrange", "Q Range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Wrange", "W Range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Erange", "E Range").SetValue(new Circle(false, Color.FromArgb(150, Color.DarkRed))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Rrange", "R Range").SetValue(new Circle(false, Color.FromArgb(150, Color.DarkRed))));
            MenuItem fullComboDamageItem = Config.SubMenu("Drawing").AddItem(new MenuItem("FullComboDraw", "Draw fullCombo damage").SetValue(true));

            Utility.HpBarDamageIndicator.DamageToUnit = getComboDamage;
            Utility.HpBarDamageIndicator.Enabled = fullComboDamageItem.GetValue<bool>();
            fullComboDamageItem.ValueChanged +=
            delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            Orbwalker.SetAttack(true);
            Orbwalker.SetMovement(true);

            //Game event callback
            Game.OnGameUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            GameObject.OnCreate += OnCreateObj;

            Game.PrintChat("== BestAkaliAfrica Loaded ==");
        }

        static void OnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Farm();
                    break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm(true);
                    break;
            }

            if (Config.SubMenu("Harass").Item("HarassActiveT").GetValue<KeyBind>().Active
                || Config.SubMenu("Harass").Item("HarassActiveT").GetValue<bool>())
            {
                Harass();
            }
        }

        private static void OnCreateObj(GameObject sender, EventArgs args)
        {
            //Detect whenever our Q land on someone
            //TODO: check its not the Q of an enemy akali.
            if (sender.Name.Contains("akali_markOftheAssasin_marker_tar.troy"))
                qInAir = false;
        }

        /******************************************
         *  Graphics stuff
         ******************************************/

        private static void OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.SubMenu("Drawing").Item(spell.Slot + "range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(myHero.Position, spell.Range, menuItem.Color);
            }
        }


        /******************************************
         *  Mechanics stuff
         ******************************************/

        private static void Combo()
        {
            var Target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
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
                && (myHero.Distance(Target) > E.Range && (HasEnergyFor(false, true, false, true)) || (!Q.IsReady() && !E.IsReady()))
                && R.IsReady() && Config.SubMenu("Combo").Item("UseRCombo").GetValue<bool>())
            {
                R.Cast(Target, packetCast);
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
                    && (HasBuff(Target, "AkaliMota") || Damage.GetSpellDamage(myHero, Target, SpellSlot.E) <= Target.Health) && Config.SubMenu("Harass").Item("UseEHarass").GetValue<bool>())
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
                    if (!laneClear && (minion.Health <= Q.GetDamage(minion)  || (minion.Health <= (Q.GetDamage(minion) + Q.GetDamage(minion, 1)) && (minion.Health > Q.GetDamage(minion)) && myHero.Distance(minion) <= Orbwalking.GetRealAutoAttackRange(myHero)))
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

        /******************************************
         *  Utility stuff
         ******************************************/

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
    }
}
