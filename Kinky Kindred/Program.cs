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
        static SpellSlot smite = SpellSlot.Unknown;
        static Spell smiteSpell;

        static void Game_OnGameLoad(EventArgs args) {
            if (Player.ChampionName != "Kindred") { return; }
            Q = new Spell(SpellSlot.Q, 500f);//340 is the jump range. 840f is the total
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 500f);
            R = new Spell(SpellSlot.R, 550f);

            menuload();
            Game.OnUpdate += Game_OnUpdate;
            smitespell();
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

            Menu SmiteM = JungM.AddSubMenu(new Menu("Smite", "Smite"));
            SmiteM.AddItem(new MenuItem("always_smite_red", "Always Smite Red", true).SetValue(false));
            SmiteM.AddItem(new MenuItem("smite_r_if__dying", "Always Smite Red if health < %", true).SetValue(new Slider(15, 1, 100)));
            SmiteM.AddItem(new MenuItem("always_smite_blue", "Always Smite Blue", true).SetValue(false));
            SmiteM.AddItem(new MenuItem("always_smite_frog", "Always Smite Frog", true).SetValue(false));
            SmiteM.AddItem(new MenuItem("smite_brf_til_lvlonoff", "Always Smite Red/Blue/Frog til level:", true).SetValue(true));
            SmiteM.AddItem(new MenuItem("smite_brf_til_lvl", "", true).SetValue(new Slider(10, 1, 18)));
            SmiteM.AddItem(new MenuItem("always_smite_wolf", "Always Smite Wolves", true).SetValue(false));
            SmiteM.AddItem(new MenuItem("always_smite_golems", "Always Smite Golems", true).SetValue(true));
            SmiteM.AddItem(new MenuItem("always_smite_wraiths", "Always Smite Wraiths", true).SetValue(true));
            SmiteM.AddItem(new MenuItem("always_smite_baron", "Always Smite Baron", true).SetValue(true));
            SmiteM.AddItem(new MenuItem("always_smite_dragon", "Always Smite Dragon", true).SetValue(true));
            SmiteM.AddItem(new MenuItem("smite_ks", "Kill Steal Smite", true).SetValue(true));

            JungM.AddItem(new MenuItem("jungleclearQ", "Use Q", true).SetValue(true));
            JungM.AddItem(new MenuItem("jungleclearmanaminQ", "Q requires % mana", true).SetValue(new Slider(15, 0, 100)));
            JungM.AddItem(new MenuItem("jungleclearE", "Use E", true).SetValue(true));
            JungM.AddItem(new MenuItem("jungleclearmanaminE", "E requires % mana", true).SetValue(new Slider(35, 0, 100)));
            JungM.AddItem(new MenuItem("jungleclearW", "Use W", true).SetValue(true));
            JungM.AddItem(new MenuItem("jungleclearmanaminW", "W requires % mana", true).SetValue(new Slider(15, 0, 100)));
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
            LaneM.AddItem(new MenuItem("laneclearWminminions", "W requires X minions", true).SetValue(new Slider(6, 3, 10)));
            LaneM.AddItem(new MenuItem("laneclearmanaminW", "W requires % mana", true).SetValue(new Slider(65, 0, 100)));
            LaneM.AddItem(new MenuItem("laneclearFAST", "Clear using q/w if lots of minions", true).SetValue(true));
            LaneM.AddItem(new MenuItem("laneclearFASTminions", "Number of minions in range to activate ClearFast", true).SetValue(new Slider(15, 10, 100)));
            LaneM.AddItem(new MenuItem("laneclearFASTmana", "ClearFast requires % mana", true).SetValue(new Slider(15, 0, 100)));


            MiscM.AddItem(new MenuItem("killsteal", "Kill Steal", true).SetValue(true));
            MiscM.AddItem(new MenuItem("fleeKey", "Flee Toggle").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            MiscM.AddItem(new MenuItem("saveallies", "Save Allies (With R)", true).SetValue(true));
            MiscM.AddItem(new MenuItem("saveallieswhen", "Save when health < %", true).SetValue(new Slider(25, 0, 100)));
            Menu DontsM = MiscM.AddSubMenu(new Menu("Dont waste ult on:", "Dont waste ult on:"));
            foreach (var ally in HeroManager.Allies.FindAll(x => x.ChampionName != Player.ChampionName)) {
                DontsM.AddItem(new MenuItem("target" + ally.ChampionName, ally.ChampionName).SetValue(false));
            }

            kinkykmenu.AddToMainMenu();
        }
        #endregion

        #region EVENT GAME ON UPDATE
        static void Game_OnUpdate(EventArgs args) {
            if (Player.IsRecalling()) { return; }
            if (Player.IsDead) { return; }

            smartW();
            checksmitecamps();

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
            if (kinm.Item("fleeKey").GetValue<KeyBind>().Active) {
                fleee();
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
            var MINIONS = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var jungleinside = MINIONS.Find(x => x.CharData.BaseSkinName.ToLower().Contains("sru"));
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
            var clearFAST = kinm.Item("laneclearFASTmana", true).GetValue<Slider>().Value;
            var MINIONS = MinionManager.GetMinions(W.Range,MinionTypes.All,MinionTeam.Enemy);
            if (MINIONS.Count <= 0) { return; }

            if (kinm.Item("laneclearQ", true).GetValue<Boolean>() && Q.IsReady() && Manapercent >= Qmana) {
                //try to get as many minions as possible
                var qminions = MINIONS.FindAll(x => x.Position.Distance(Game.CursorPos) < 400f && x.Health < Q.GetDamage(x) && x.ServerPosition.Distance(Player.ServerPosition) < 340f);
                var siege = qminions.Find(x => x.CharData.BaseSkinName.ToLower().Contains("siege") || x.CharData.BaseSkinName.ToLower().Contains("super"));
                if (qminions.Count >= kinm.Item("laneclearQcast", true).GetValue<Slider>().Value) { Q.Cast(qminions.FirstOrDefault().ServerPosition); }
                //try to get sieges if Q is still available
                if (Q.IsReady() && siege != null && Q.IsInRange(siege)) {
                    Q.Cast(siege);
                }
            }
            if (kinm.Item("laneclearEsuper", true).GetValue<Boolean>() && E.IsReady() && Manapercent >= Emana && !Q.IsReady()) {
                var siege = MINIONS.Find(x => x.CharData.BaseSkinName.ToLower().Contains("siege") || x.CharData.BaseSkinName.ToLower().Contains("super"));
                if (siege != null && E.CanCast(siege)) {
                    E.Cast(siege);
                }
            }
            if (kinm.Item("laneclearW", true).GetValue<Boolean>() && W.IsReady() && Manapercent >= Wmana
                && MINIONS.Count >= kinm.Item("laneclearWminminions", true).GetValue<Slider>().Value) {
                W.Cast();                
            }
            if (kinm.Item("laneclearFAST", true).GetValue<Boolean>() && Manapercent >= clearFAST && MINIONS.Count >= kinm.Item("laneclearFASTminions", true).GetValue<Slider>().Value) {
                if (W.IsReady()) { W.Cast(); }
                if (Q.IsReady()) { Q.Cast(Game.CursorPos); }
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
            var cansmite = false;
            if (kinm.Item("smite_ks", true).GetValue<Boolean>() && smite != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(smite) == SpellState.Ready) { cansmite = true; }
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => Q.CanCast(h) || E.CanCast(h))) {
                if (hasundyingbuff(enemy) || enemy.HasBuffOfType(BuffType.Invulnerability) || enemy.HasBuffOfType(BuffType.SpellShield) || enemy.HasBuffOfType(BuffType.SpellImmunity)) { continue; }

                var edmg = E.GetDamage(enemy);
                var qdmg = Q.GetDamage(enemy);
                var enemyhealth = enemy.Health;
                var enemyregen = enemy.HPRegenRate / 2;
                double smitedmg = 0;
                if (cansmite) { smitedmg = Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Smite); }


                if (cansmite && smitedmg > enemy.Health) {
                    Player.Spellbook.CastSpell(smite, enemy);
                }

                if (((enemyhealth + enemyregen) <= edmg) && E.CanCast(enemy) && E.CanCast(enemy)) {
                    E.Cast(enemy);
                }

                if (((enemyhealth + enemyregen) <= qdmg) && Q.CanCast(enemy) && Q.CanCast(enemy)) {
                    Q.Cast(enemy.ServerPosition);
                }

                //check E+smite combo
                if (cansmite && E.IsReady() && ((smitedmg + edmg) > (enemyhealth + enemyregen))) {
                    E.Cast(enemy);
                    Player.Spellbook.CastSpell(smite, enemy);
                }

                //full e+q combo
                if (((enemyhealth + enemyregen) <= (edmg + qdmg)) && E.CanCast(enemy) && Q.CanCast(enemy)) {
                    E.Cast(enemy);
                    Q.Cast(enemy.ServerPosition);
                }
            }
        }
        #endregion

        #region MISC FUNCTIONS
        static void checksmitecamps() {
            var minion = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.All, MinionOrderTypes.MaxHealth).Find(x => x.CharData.BaseSkinName.ToLower().Contains("sru") && !x.CharData.BaseSkinName.ToLower().Contains("mini"));
            if (minion == null) { return; }
            Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.10f, Color.GreenYellow, "Mob is: " + minion.CharData.BaseSkinName);
            if (smite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(smite) != SpellState.Ready) { return; }
            var damage = Player.GetSummonerSpellDamage(minion, Damage.SummonerSpell.Smite);

            if (minion.CharData.BaseSkinName.ToLower().Contains("red")) {
                if (kinm.Item("always_smite_red", true).GetValue<Boolean>() && damage > minion.Health) {
                    Player.Spellbook.CastSpell(smite, minion);
                }
                if (((Player.Health / Player.MaxHealth) * 100) <= kinm.Item("smite_r_if__dying", true).GetValue<Slider>().Value) {
                    Player.Spellbook.CastSpell(smite, minion);
                }
                if (kinm.Item("smite_brf_til_lvlonoff", true).GetValue<Boolean>() && Player.Level <= kinm.Item("smite_brf_til_lvl", true).GetValue<Slider>().Value) {
                    Player.Spellbook.CastSpell(smite, minion);
                }
            }

            if (minion.CharData.BaseSkinName.ToLower().Contains("blue")) {
                if (kinm.Item("always_smite_blue", true).GetValue<Boolean>() && damage > minion.Health) { Player.Spellbook.CastSpell(smite, minion); }
                if (kinm.Item("smite_brf_til_lvlonoff", true).GetValue<Boolean>() && Player.Level <= kinm.Item("smite_brf_til_lvl", true).GetValue<Slider>().Value) {
                    Player.Spellbook.CastSpell(smite, minion);
                }
            }
            //always smite frog from start to get poison buffs
            if (minion.CharData.BaseSkinName.ToLower().Contains("gromp")) {
                if (kinm.Item("always_smite_frog", true).GetValue<Boolean>()) { Player.Spellbook.CastSpell(smite, minion); }
                if (kinm.Item("smite_brf_til_lvlonoff", true).GetValue<Boolean>() && Player.Level <= kinm.Item("smite_brf_til_lvl", true).GetValue<Slider>().Value) {
                    Player.Spellbook.CastSpell(smite, minion);
                }
            }
            if (kinm.Item("always_smite_golems", true).GetValue<Boolean>() && minion.CharData.BaseSkinName.ToLower().Contains("krug") && damage > minion.Health) {
                Player.Spellbook.CastSpell(smite, minion); return;
            }
            if (kinm.Item("always_smite_dragon", true).GetValue<Boolean>() && minion.CharData.BaseSkinName.ToLower().Contains("dragon") && damage > minion.Health) {
                Player.Spellbook.CastSpell(smite, minion); return;
            }
            if (kinm.Item("always_smite_wolf", true).GetValue<Boolean>() && minion.CharData.BaseSkinName.ToLower().Contains("murkwolf") && damage > minion.Health) {
                Player.Spellbook.CastSpell(smite, minion); return;
            }
            if (kinm.Item("always_smite_wraiths", true).GetValue<Boolean>() && minion.CharData.BaseSkinName.ToLower().Contains("razorbeak") && damage > minion.Health) {
                Player.Spellbook.CastSpell(smite, minion); return;
            }
            if (kinm.Item("always_smite_baron", true).GetValue<Boolean>() && minion.CharData.BaseSkinName.ToLower().Contains("baron") && damage > minion.Health) {
                Player.Spellbook.CastSpell(smite, minion); return;
            }
        }

        static void smitespell() {
            if (Player.Spellbook.GetSpell(SpellSlot.Summoner1).SData.Name.ToLower().Contains("smite")) {
                smite = SpellSlot.Summoner1;
                smiteSpell = new Spell(smite);
            } else if (Player.Spellbook.GetSpell(SpellSlot.Summoner2).SData.Name.ToLower().Contains("smite")) {
                smite = SpellSlot.Summoner2;
                smiteSpell = new Spell(smite);
            }
        }

        static void fleee() {
            if (!Q.IsReady()) { return; }
            Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.10f, Color.GreenYellow, "Wall Jump Active");
            var XXX = (Vector3)canjump();
            if (XXX != null) {
                Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.50f, Color.GreenYellow, "could jump here");
                Q.Cast(XXX);
                Orbwalking.Orbwalk(null, XXX, 90f, 0f, false, false);
            } else {
                Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.50f, Color.GreenYellow, "can't jump here");
            }
        }

        static Vector3? canjump() {
            var wallCheck = VectorHelper.GetFirstWallPoint(Player.Position, Player.Position);
            //loop angles around the player to check for a point to jump to
            //credits to hellsing wherever it has his code here somewhere... xD
            float maxAngle = 80;
            float step = maxAngle / 20;
            float currentAngle = 0;
            float currentStep = 0;
            Vector3 currentPosition = Player.Position;
            Vector2 direction = ((Player.Position.To2D() + 50) - currentPosition.To2D()).Normalized();
            while (true) {
                if (currentStep > maxAngle && currentAngle < 0) { break; }

                if ((currentAngle == 0 || currentAngle < 0) && currentStep != 0) {
                    currentAngle = (currentStep) * (float)Math.PI / 180;
                    currentStep += step;
                } else if (currentAngle > 0) {
                    currentAngle = -currentAngle;
                }

                Vector3 checkPoint;

                // One time only check for direct line of sight without rotating
                if (currentStep == 0) {
                    currentStep = step;
                    checkPoint = currentPosition + 300 * direction.To3D();
                } else {
                    checkPoint = currentPosition + 300 * direction.Rotated(currentAngle).To3D();
                }
                if (checkPoint.IsWall()) { continue; }
                // Check if there is a wall between the checkPoint and currentPosition
                wallCheck = VectorHelper.GetFirstWallPoint(checkPoint, currentPosition);
                if (wallCheck == null) { continue; } //jump to the next loop
                //get the jump point
                Vector3 wallPositionOpposite = (Vector3)VectorHelper.GetFirstWallPoint((Vector3)wallCheck, currentPosition, 5);
                //check if the walking path is big enough to be worth a jump..if not then just skip to the next loop
                if (Player.GetPath(wallPositionOpposite).ToList().To2D().PathLength() - Player.Distance(wallPositionOpposite) < 340) {
                    Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.50f, Color.GreenYellow, "not worth a jump...");
                    continue;
                }

                //check the jump distance and if its short enough then jump...
                if (Player.Distance(wallPositionOpposite, true) < Math.Pow(300 - Player.BoundingRadius / 2, 2)) {
                    return wallPositionOpposite;
                }
            }
            return null;
        }

        void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args) {

            //Patented by XcxooxL
            if (!sender.IsEnemy || !sender.IsValidTarget() || !sender.IsVisible) return;

            if (args.Target != null && !args.Target.IsAlly) return;
            double damage = 0;
            //if it is an Auto Attack !
            if (args.SData.IsAutoAttack()) {
                var Target = args.Target as Obj_AI_Hero;
                if (Target == null)
                    return; //must be a champion
                if (args.SData.Name.ToLower().Contains("crit")) {
                    damage += sender.GetAutoAttackDamage(Target) * 2;
                    //Console.WriteLine("Critical " + damage);
                    if (sender.InventoryItems.Any(item => item.Id.Equals(3031))) {
                        Console.WriteLine("Infinity Edge");
                        damage += damage * 1.25;
                    }
                    //Infinity edge
                } else {
                    damage += sender.GetAutoAttackDamage(Target, true);
                }
                damage += 2; //to be on the safe side
                Add(Target, damage, sender.Distance(Target) / args.SData.MissileSpeed + 1 / sender.AttackDelay);
                Console.WriteLine(
                    "Target : " + Target.Name + "Damage : " + damage + " Time To Hit : " +
                    sender.Distance(Target) / args.SData.MissileSpeed * 1000);

            } else //if its a Spell
              {
                float delay = 0;
                var missileSpeed = args.SData.MissileSpeed;
                foreach (var spellInfo in
                    SpellDatabase.Spells.Where(spellInfo => spellInfo.spellName.Equals(args.SData.Name))) {
                    if (spellInfo.spellType.Equals(SpellType.Line)) {
                        _myPoly = new Geometry.Polygon.Rectangle(
                            args.Start, args.Start.Extend(args.End, spellInfo.range), spellInfo.radius);
                    } else if (spellInfo.spellType.Equals(SpellType.Circular)) {

                        var pos = sender.Distance(args.End) > spellInfo.range
                            ? sender.Position.Extend(args.End, spellInfo.range)
                            : args.End;
                        _myPoly = new Geometry.Polygon.Circle(pos, spellInfo.radius);
                    }
                    missileSpeed = spellInfo.projectileSpeed;
                    delay += spellInfo.spellDelay;
                    break;
                }

                //Patented by xcxooxl ALL OF THIS IS MINE ! YOU WANT IT? CREDIT ME!

                if (sender is Obj_AI_Hero) {
                    var enemy = sender as Obj_AI_Hero;
                    foreach (var ally in TrackList) {
                        var timeToHit = delay + ally.Distance(args.Start) / missileSpeed * 1000 +
                                        args.SData.ChannelDuration + args.SData.DelayTotalTimePercent * -1;
                        if (args.SData.TargettingType.Equals(SpellDataTargetType.Unit)) //Targeted
                        {
                            damage += enemy.GetDamageSpell(args.Target as Obj_AI_Base, args.Slot).CalculatedDamage;
                            Add(ally, damage, timeToHit);
                        }


                        Console.WriteLine(
                            "Spellname" + args.SData.Name + " Time to hit " + timeToHit + "MissileSpeed " + missileSpeed);

                        var futurePos = Prediction.GetPrediction(ally, timeToHit / 1000).UnitPosition;
                        futurePosCircle = new Geometry.Polygon.Circle(futurePos, 125);
                        if (_myPoly.IsInside(futurePos)) {
                            damage += enemy.GetDamageSpell(ally, args.Slot).CalculatedDamage;
                            Add(ally, damage, timeToHit);
                        }
                        Utility.DelayAction.Add(
                            (int)(timeToHit + 1200), () => {
                                futurePosCircle = null;
                                _myPoly = null;
                            }); //stop drawing polygons

                    }
                }
            }

            //Patented by XcxooxL
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

    #region VECTOR HELPER FROM STACKOVERFLOW
    internal class VectorHelper {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        // Credits to furikuretsu from Stackoverflow (http://stackoverflow.com/a/10772759)
        // Modified for my needs
        #region ConeCalculations

        public static bool IsLyingInCone(Vector2 position, Vector2 apexPoint, Vector2 circleCenter, double aperture) {
            // This is for our convenience
            double halfAperture = aperture / 2;

            // Vector pointing to X point from apex
            Vector2 apexToXVect = apexPoint - position;

            // Vector pointing from apex to circle-center point.
            Vector2 axisVect = apexPoint - circleCenter;

            // X is lying in cone only if it's lying in 
            // infinite version of its cone -- that is, 
            // not limited by "round basement".
            // We'll use dotProd() to 
            // determine angle between apexToXVect and axis.
            bool isInInfiniteCone = DotProd(apexToXVect, axisVect) / Magn(apexToXVect) / Magn(axisVect) >
            // We can safely compare cos() of angles 
            // between vectors instead of bare angles.
            Math.Cos(halfAperture);

            if (!isInInfiniteCone)
                return false;

            // X is contained in cone only if projection of apexToXVect to axis
            // is shorter than axis. 
            // We'll use dotProd() to figure projection length.
            bool isUnderRoundCap = DotProd(apexToXVect, axisVect) / Magn(axisVect) < Magn(axisVect);

            return isUnderRoundCap;
        }

        private static float DotProd(Vector2 a, Vector2 b) {
            return a.X * b.X + a.Y * b.Y;
        }

        private static float Magn(Vector2 a) {
            return (float)(Math.Sqrt(a.X * a.X + a.Y * a.Y));
        }

        #endregion

        public static Vector2? GetFirstWallPoint(Vector3 from, Vector3 to, float step = 25) {
            return GetFirstWallPoint(from.To2D(), to.To2D(), step);
        }

        public static Vector2? GetFirstWallPoint(Vector2 from, Vector2 to, float step = 25) {
            var direction = (to - from).Normalized();

            for (float d = 0; d < from.Distance(to); d = d + step) {
                var testPoint = from + d * direction;
                var flags = NavMesh.GetCollisionFlags(testPoint.X, testPoint.Y);
                if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building)) {
                    return from + (d - step) * direction;
                }
            }

            return null;
        }

        public static List<Obj_AI_Base> GetDashObjects(IEnumerable<Obj_AI_Base> predefinedObjectList = null) {
            List<Obj_AI_Base> objects;
            if (predefinedObjectList != null)
                objects = predefinedObjectList.ToList();
            else
                objects = ObjectManager.Get<Obj_AI_Base>().Where(o => o.IsValidTarget(Orbwalking.GetRealAutoAttackRange(o))).ToList();

            var apexPoint = player.ServerPosition.To2D() + (player.ServerPosition.To2D() - Game.CursorPos.To2D()).Normalized() * Orbwalking.GetRealAutoAttackRange(player);

            return objects.Where(o => VectorHelper.IsLyingInCone(o.ServerPosition.To2D(), apexPoint, player.ServerPosition.To2D(), Math.PI)).OrderBy(o => o.Distance(apexPoint, true)).ToList();
        }
    }
    #endregion
}


