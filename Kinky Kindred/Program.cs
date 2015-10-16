#region REFS
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using Collision = LeagueSharp.Common.Collision;
#endregion

namespace Kinky_Kindred {
    internal class Kinky_Kindred {

        #region GAME LOAD
        static readonly Obj_AI_Hero Player = ObjectManager.Player;
        static Orbwalking.Orbwalker Orbwalker;
        static Menu kinkykmenu;
        static Menu kinm { get { return Kinky_Kindred.kinkykmenu; } }
        static float Manapercent { get { return Player.ManaPercent; } }

        static Spell Q, W, E, R;
        static Items.Item botrk = new Items.Item(3153, 550);
        static Items.Item mercurial = new Items.Item(3139, 0f);
        static Items.Item dervish = new Items.Item(3137, 0f);
        static Items.Item qss = new Items.Item(3140, 0f);

        static void Game_OnGameLoad(EventArgs args) {
            if (Player.ChampionName != "Kindred") { return; }
            Q = new Spell(SpellSlot.Q, 500f);//340 is the jump range. 840f is the total
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 500f);
            R = new Spell(SpellSlot.R, 550f);

            menuload();
            Game.OnUpdate += Game_OnUpdate;
        }
        static void Main(string[] args) { CustomEvents.Game.OnGameLoad += Game_OnGameLoad; }
        #endregion

        #region MENU

        static void menuload() {
            kinkykmenu = new Menu("Kinky Kindred", Player.ChampionName, true);
            Menu OrbwalkerMenu = kinkykmenu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(OrbwalkerMenu);
            TargetSelector.AddToMenu(kinkykmenu.AddSubMenu(new Menu(Player.ChampionName + ": Target Selector", "Target Selector")));
            Menu haraM = kinkykmenu.AddSubMenu(new Menu("Harass", "Harass"));
            Menu LaneM = kinkykmenu.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            Menu JungM = kinkykmenu.AddSubMenu(new Menu("Jungle Clear", "Jungleclear"));
            Menu MiscM = kinkykmenu.AddSubMenu(new Menu("Misc", "Misc"));

            haraM.AddItem(new MenuItem("harassQ", "Use Q", true).SetValue(true));
            haraM.AddItem(new MenuItem("harassQwithin", "Only use Q when enemy is within X range", true).SetValue(new Slider(500, 1, 500)));
            haraM.AddItem(new MenuItem("harassmanaminQ", "Q requires % mana", true).SetValue(new Slider(40, 0, 100)));
            haraM.AddItem(new MenuItem("harassuseW", "Use W", true).SetValue(true));
            haraM.AddItem(new MenuItem("harassWwithin", "Only use W when enemy is within X range", true).SetValue(new Slider(500, 1, 800)));
            haraM.AddItem(new MenuItem("harassmanaminW", "W requires % mana", true).SetValue(new Slider(35, 0, 100)));
            haraM.AddItem(new MenuItem("harassuseE", "Use E", true).SetValue(true));
            haraM.AddItem(new MenuItem("harassmanaminE", "E requires % mana", true).SetValue(new Slider(45, 0, 100)));
            haraM.AddItem(new MenuItem("harassActive", "Active", true).SetValue(true));

            JungM.AddItem(new MenuItem("jungleclearQ", "Use Q", true).SetValue(true));
            LaneM.AddItem(new MenuItem("jungleclearmanaminQ", "Q requires % mana", true).SetValue(new Slider(25, 0, 100)));
            JungM.AddItem(new MenuItem("jungleclearE", "Use E", true).SetValue(true));
            LaneM.AddItem(new MenuItem("jungleclearmanaminE", "E requires % mana", true).SetValue(new Slider(35, 0, 100)));
            JungM.AddItem(new MenuItem("jungleclearW", "Use W", true).SetValue(true));
            LaneM.AddItem(new MenuItem("jungleclearmanaminW", "W requires % mana", true).SetValue(new Slider(40, 0, 100)));
            JungM.AddItem(new MenuItem("jungleActive", "Active", true).SetValue(true));

            LaneM.AddItem(new MenuItem("laneclearQ", "Use Q", true).SetValue(true));
            LaneM.AddItem(new MenuItem("laneclearQcast", "Q cast if it can kill X minions", true).SetValue(new Slider(2, 1, 3)));
            LaneM.AddItem(new MenuItem("laneclearQbigminions", "Use Q on siege/super minions", true).SetValue(true));
            LaneM.AddItem(new MenuItem("laneclearlasthit", "Q when non-killable by AA", true).SetValue(true));
            LaneM.AddItem(new MenuItem("laneclearlasthithealth", "Non-killable requires at least % health", true).SetValue(new Slider(7, 0, 100)));
            LaneM.AddItem(new MenuItem("laneclearmanaminQ", "Q requires % mana", true).SetValue(new Slider(65, 0, 100)));
            LaneM.AddItem(new MenuItem("laneclearEsuper", "Use E to kill siege/super if Q isnt avail", true).SetValue(true));
            LaneM.AddItem(new MenuItem("laneclearmanaminE", "E requires % mana", true).SetValue(new Slider(45, 0, 100)));
            LaneM.AddItem(new MenuItem("laneclearW", "Use W", true).SetValue(true));
            LaneM.AddItem(new MenuItem("laneclearWminminions", "W requires X minions", true).SetValue(new Slider(5, 3, 10)));
            LaneM.AddItem(new MenuItem("laneclearmanaminW", "W requires % mana", true).SetValue(new Slider(65, 0, 100)));


            MiscM.AddItem(new MenuItem("killsteal", "Kill Steal", true).SetValue(true));
            MiscM.AddItem(new MenuItem("saveallies", "Save Allies (With R)", true).SetValue(true));
            MiscM.AddItem(new MenuItem("saveallieswhen", "Save when health < %", true).SetValue(new Slider(25, 0, 100)));

            kinkykmenu.AddToMainMenu();
        }
        #endregion

        #region EVENT GAME ON UPDATE
        static void Game_OnUpdate(EventArgs args) {
            if (Player.IsRecalling()) { return; }
            if (Player.IsDead) { return; }

            smartW();

            if (kinm.Item("killsteal", true).GetValue<Boolean>()) {
                Killsteal();
            }
            if (Player.IsRecalling()) { return; }

            if (kinm.Item("harassActive", true).GetValue<Boolean>() || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) {
                harass();
            }
            if (kinm.Item("jungleActive", true).GetValue<Boolean>()) {
                JungleClear();
            }

            switch (Orbwalker.ActiveMode) {
                case Orbwalking.OrbwalkingMode.LaneClear:
                    laneclear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    break;
            }
        }
        #endregion

        #region LANECLEAR

        static void smartW() {
            var WisON = HeroManager.Allies.Find(a => a.Buffs.Any(b => b.Name.Contains("kindredwclonebuffvisible")));
            if (WisON != null) {
                var enemy = HeroManager.Enemies.FirstOrDefault(h => W.IsInRange(h));
                if (enemy != null) {
                    W.Cast(enemy);
                } else {
                    var Minions = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy);
                    if (Minions.Count > 0) {
                        var min = Minions.LastOrDefault();
                        W.Cast(min);
                    }
                }                
            }
        }

        static void JungleClear() {
            var Qmana = kinm.Item("jungleclearmanaminQ", true).GetValue<Slider>().Value;
            var Wmana = kinm.Item("jungleclearmanaminW", true).GetValue<Slider>().Value;
            var Emana = kinm.Item("jungleclearmanaminE", true).GetValue<Slider>().Value;
            var MINIONS = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.All, MinionOrderTypes.MaxHealth);
            var jungleinside = MINIONS.Find(X => X.Team == GameObjectTeam.Neutral && !X.CharData.BaseSkinName.ToLower().Contains("dragon") && !X.CharData.BaseSkinName.ToLower().Contains("baron"));
            if (jungleinside != null) {
                if (kinm.Item("jungleclearQ", true).GetValue<Boolean>() && Manapercent > Qmana) {
                    if (Q.IsReady()) { Q.Cast(Game.CursorPos); }
                }
                if (kinm.Item("jungleclearE", true).GetValue<Boolean>() && Manapercent > Emana) {
                    if (E.CanCast(jungleinside)) { E.Cast(jungleinside); }
                }
                if (kinm.Item("jungleclearW", true).GetValue<Boolean>() && Manapercent > Wmana) {
                    if (W.IsReady()) { W.Cast(); }
                }
            }

        }

        static void laneclear() {
            var Qmana = kinm.Item("laneclearmanaminQ", true).GetValue<Slider>().Value;
            var Wmana = kinm.Item("laneclearmanaminW", true).GetValue<Slider>().Value;
            var Emana = kinm.Item("laneclearmanaminE", true).GetValue<Slider>().Value;
            var Minions = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy);
            if (Minions.Count <= 0) { return; }

            if (kinm.Item("laneclearQ", true).GetValue<Boolean>() && Q.IsReady() && Manapercent >= Qmana) {
                //try to get as many minions as possible
                var qminions = Minions.FindAll(x => x.ServerPosition.Distance(Game.CursorPos) < 400f && x.Health < Q.GetDamage(x));
                var siege = qminions.Find(x => x.CharData.BaseSkinName.ToLower().Contains("siege") || x.CharData.BaseSkinName.ToLower().Contains("super"));
                if (qminions.Count >= kinm.Item("laneclearQcast", true).GetValue<Slider>().Value) { Q.Cast(qminions.FirstOrDefault().ServerPosition); }
                //try to get sieges if Q is still available
                if (Q.IsReady() && siege != null && Q.IsInRange(siege)) {
                    Q.Cast(siege);
                }
            }
            if (kinm.Item("laneclearEsuper", true).GetValue<Boolean>() && E.IsReady() && Manapercent >= Emana && !Q.IsReady()) {
                var siege = Minions.Find(x => x.CharData.BaseSkinName.ToLower().Contains("siege") || x.CharData.BaseSkinName.ToLower().Contains("super"));
                if (siege != null && E.CanCast(siege)) {
                    E.Cast(siege);
                }
            }
            if (kinm.Item("laneclearW", true).GetValue<Boolean>() && W.IsReady() && Manapercent >= Wmana
                && Minions.Count >= kinm.Item("laneclearWminminions", true).GetValue<Slider>().Value) {
                W.Cast();                
            }
        }

        static void Event_OnNonKillableMinion(AttackableUnit minion) {
            var minionX = (Obj_AI_Minion)minion;
            if (Manapercent < kinm.Item("laneclearmanaminQ", true).GetValue<Slider>().Value) { return; }
            if (kinm.Item("laneclearlasthit", true).GetValue<Boolean>() && Q.CanCast(minionX)) {
                var minhealth = kinm.Item("laneclearlasthithealth", true).GetValue<Slider>().Value;
                if (minionX.Health <= Q.GetDamage(minionX) && minionX.HealthPercent >= minhealth) {
                    Q.Cast(minionX);
                }
            }
        }

        #endregion

        #region HARASS
        static void harass() {
            var Qmana = kinm.Item("harassmanaminQ", true).GetValue<Slider>().Value;
            var Wmana = kinm.Item("harassmanaminW", true).GetValue<Slider>().Value;
            var Emana = kinm.Item("harassmanaminE", true).GetValue<Slider>().Value;
            var enemies = HeroManager.Enemies.FindAll(h => Player.ServerPosition.Distance(h.ServerPosition) < 800f);
            foreach (var enemy in enemies) {
                if (hasundyingbuff(enemy)) { continue; }
                if (kinm.Item("harassQ", true).GetValue<Boolean>() && Manapercent > Qmana && Q.IsReady() &&
                    Game.CursorPos.Distance(enemy.Position) < kinm.Item("harassQwithin", true).GetValue<Slider>().Value) {
                    Q.Cast(Game.CursorPos);
                }
                if (kinm.Item("harassuseW", true).GetValue<Boolean>() && Manapercent > Wmana && W.IsReady() &&
                    Player.ServerPosition.Distance(enemy.ServerPosition) < kinm.Item("harassWwithin", true).GetValue<Slider>().Value) {
                    W.Cast();
                }
                if (kinm.Item("harassuseE", true).GetValue<Boolean>() && Manapercent > Emana && E.IsReady()) {
                    E.Cast(enemy);
                }
            }
        }

        static void Killsteal() {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => Q.CanCast(h) || E.CanCast(h))) {
                if (hasundyingbuff(enemy)) { continue; }
                var edmg = E.GetDamage(enemy);
                var qdmg = Q.GetDamage(enemy);
                var enemyhealth = enemy.Health;
                var enemyregen = enemy.HPRegenRate / 2;
                if (((enemyhealth + enemyregen) <= edmg) && E.CanCast(enemy) && E.CanCast(enemy)) {
                    E.Cast(enemy);
                }

                if (((enemyhealth + enemyregen) <= qdmg) && Q.CanCast(enemy) && Q.CanCast(enemy)) {
                    Q.Cast(enemy.ServerPosition);
                }

                //full e+q combo
                if (((enemyhealth + enemyregen) <= (edmg + qdmg)) && E.CanCast(enemy) && Q.CanCast(enemy)) {
                    E.Cast(enemy); return;
                    Q.Cast(enemy.ServerPosition);
                }
            }
        }
        #endregion

        #region MISC FUNCTIONS
        static void Event_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args) {
            if (Player.IsDead || !E.IsReady()) { return; }

            if (sender is Obj_AI_Hero && sender.IsEnemy && args.Target != null && args.Target.NetworkId == Player.NetworkId) {
                var enemy = (Obj_AI_Hero)sender;
                var slot = enemy.GetSpellSlot(args.SData.Name);
                if (slot == SpellSlot.Unknown) { return; }
                //ignite...
                if (slot == enemy.GetSpellSlot("SummonerDot")) {
                    var dmgonsoul = (float)enemy.GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite);
                    if (dmgonsoul > Player.Health && R.IsReady()) { R.Cast(); }
                }
                if (Player.HealthPercent <= kinm.Item("saveallieswhen", true).GetValue<Slider>().Value && !R.IsInRange(enemy.ServerPosition)) {
                    R.Cast(Player.ServerPosition);
                }
            }
        }

        static bool hasundyingbuff(Obj_AI_Hero target) {
            //checks for undying buffs and shields
            var hasbufforshield = TargetSelector.IsInvulnerable(target, TargetSelector.DamageType.Magical, false);
            if (hasbufforshield) { return true; }
            var hasbuff = HeroManager.Enemies.Find(a =>
                target.CharData.BaseSkinName == a.CharData.BaseSkinName && a.Buffs.Any(b =>
                    b.Name.ToLower().Contains("chrono shift") ||
                    b.Name.ToLower().Contains("poppyditarget")));
            if (hasbuff != null) { return true; }
            return false;
        }
        #endregion

    }
}
