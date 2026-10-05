#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.AI;
using ProjectEta.Battle;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day94FourStarIntegrationTests
    {
        private PieceDatabase _database;

        [SetUp]
        public void SetUp()
        {
            AbilityBoardRegistry.Clear();
            TemporarySummonService.ClearAll();
            MovementRangeModifierService.ClearAll();
            TileBlockService.ClearAll();
            TemporaryDamageModifierService.Clear();
            AIAbilityCandidateRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();

            Assert.That(
                EditorApplication.ExecuteMenuItem("Project Eta/Day 94/Patch Four Star Abilities"),
                Is.True);

            _database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(
                "Assets/ProjectEta/Data/PieceDatabase.asset");

            Assert.That(_database, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            TemporarySummonService.ClearAll();
            MovementRangeModifierService.ClearAll();
            TileBlockService.ClearAll();
            TemporaryDamageModifierService.Clear();
            AbilityBoardRegistry.Clear();
            AIAbilityCandidateRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();
        }

        [Test]
        public void FourStarRoster_정확히18종이고기획서스탯을유지한다()
        {
            var expected = new Dictionary<string, Vector2Int>
            {
                ["marshal"] = new Vector2Int(6, 6),
                ["grand_cannon"] = new Vector2Int(6, 6),
                ["imperial_knight"] = new Vector2Int(6, 6),
                ["high_priest"] = new Vector2Int(6, 5),
                ["war_rider"] = new Vector2Int(6, 6),
                ["siege_chariot"] = new Vector2Int(7, 6),
                ["grand_guardian"] = new Vector2Int(8, 4),
                ["grand_unicorn"] = new Vector2Int(6, 6),
                ["grand_gryphon"] = new Vector2Int(6, 6),
                ["archmage"] = new Vector2Int(5, 6),
                ["war_cleric"] = new Vector2Int(7, 5),
                ["executioner"] = new Vector2Int(6, 7),
                ["storm_knight"] = new Vector2Int(6, 6),
                ["bastion"] = new Vector2Int(9, 3),
                ["field_commander"] = new Vector2Int(6, 5),
                ["illusionist"] = new Vector2Int(5, 5),
                ["deadeye"] = new Vector2Int(5, 7),
                ["gatekeeper"] = new Vector2Int(8, 4)
            };

            int count = 0;

            for (int i = 0; i < _database.Definitions.Count; i++)
            {
                PieceDefinition definition = _database.Definitions[i];
                if (definition == null || definition.Grade != PieceGrade.FourStar) continue;

                count++;
                Assert.That(expected.ContainsKey(definition.PieceId), Is.True, definition.PieceId);

                Vector2Int stat = expected[definition.PieceId];
                Assert.That(definition.BaseHp, Is.EqualTo(stat.x), definition.PieceId + " HP");
                Assert.That(definition.BaseAtk, Is.EqualTo(stat.y), definition.PieceId + " ATK");
                Assert.That(definition.MovementRules.Length, Is.GreaterThan(0), definition.PieceId);
            }

            Assert.That(count, Is.EqualTo(18));
        }

        [Test]
        public void FourStarExplicitAbilities_기획서명시13종에연결된다()
        {
            string[] ids =
            {
                "marshal", "grand_cannon", "high_priest", "siege_chariot",
                "grand_guardian", "archmage", "war_cleric", "executioner",
                "storm_knight", "bastion", "field_commander", "deadeye", "gatekeeper"
            };

            for (int i = 0; i < ids.Length; i++)
            {
                PieceDefinition piece = _database.FindById(ids[i]);
                Assert.That(piece, Is.Not.Null, ids[i]);
                Assert.That(piece.Abilities.Length, Is.GreaterThan(0), ids[i]);
            }

            Assert.That(_database.FindById("field_commander").Abilities.Length, Is.EqualTo(2));
        }

        [Test]
        public void MarshalCommand_인접아군ATK를1증가시키고BaseAtk는변경하지않는다()
        {
            var board = new BoardState();
            PieceRuntimeState marshal = Spawn(board, "marshal", new Vector2Int(4, 4), true);
            PieceRuntimeState ally = Spawn(board, "executioner", new Vector2Int(5, 4), true);

            int baseAtk = ally.Definition.BaseAtk;

            Assert.That(AuraResolver.GetAttack(ally, board), Is.EqualTo(baseAtk + 1));
            Assert.That(ally.Definition.BaseAtk, Is.EqualTo(baseAtk));
        }

        [Test]
        public void HighPriestBlessing_거리2아군을2회복한다()
        {
            var board = new BoardState();
            PieceRuntimeState priest = Spawn(board, "high_priest", new Vector2Int(4, 4), true);
            PieceRuntimeState ally = Spawn(board, "grand_guardian", new Vector2Int(6, 4), true);
            ally.CurrentHp = 4;

            PieceAbilityDefinition ability = FindAbility(priest, FourStarAbilityIds.HighPriestBlessing);

            AbilityExecutionResult result = PieceAbilityService.ExecuteAbility(
                ability,
                new AbilityExecutionContext(
                    priest,
                    ally,
                    ally.BoardPosition,
                    board: board));

            Assert.That(result.Success, Is.True);
            Assert.That(ally.CurrentHp, Is.EqualTo(6));
        }

        [Test]
        public void GrandGuardian_인접아군피해1을자신이분담한다()
        {
            var board = new BoardState();
            PieceRuntimeState guardian = Spawn(board, "grand_guardian", new Vector2Int(3, 3), true);
            PieceRuntimeState ally = Spawn(board, "marshal", new Vector2Int(4, 3), true);
            PieceRuntimeState enemy = Spawn(board, "executioner", new Vector2Int(4, 4), false);

            int guardianBefore = guardian.CurrentHp;
            int applied = DamageResolver.ApplyDamage(ally, 4, enemy);

            Assert.That(applied, Is.EqualTo(3));
            Assert.That(guardian.CurrentHp, Is.EqualTo(guardianBefore - 1));
        }

        [Test]
        public void GrandGuardian_HP1이면피해분담하지않는다()
        {
            var board = new BoardState();
            PieceRuntimeState guardian = Spawn(board, "grand_guardian", new Vector2Int(3, 3), true);
            PieceRuntimeState ally = Spawn(board, "marshal", new Vector2Int(4, 3), true);
            PieceRuntimeState enemy = Spawn(board, "executioner", new Vector2Int(4, 4), false);

            guardian.CurrentHp = 1;
            int applied = DamageResolver.ApplyDamage(ally, 4, enemy);

            Assert.That(applied, Is.EqualTo(4));
            Assert.That(guardian.CurrentHp, Is.EqualTo(1));
        }

        [Test]
        public void Executioner_대상HP3분의1이하면피해2증가한다()
        {
            var board = new BoardState();
            PieceRuntimeState executioner = Spawn(board, "executioner", new Vector2Int(4, 4), true);
            PieceRuntimeState target = Spawn(board, "grand_guardian", new Vector2Int(4, 5), false);
            target.CurrentHp = 2;

            int applied = DamageResolver.ApplyDamage(target, 1, executioner);

            Assert.That(applied, Is.EqualTo(3));
        }

        [Test]
        public void Deadeye_거리5이상공격은피해1증가한다()
        {
            var board = new BoardState();
            PieceRuntimeState deadeye = Spawn(board, "deadeye", new Vector2Int(1, 1), true);
            PieceRuntimeState target = Spawn(board, "marshal", new Vector2Int(1, 6), false);

            int applied = DamageResolver.ApplyDamage(target, 1, deadeye);

            Assert.That(applied, Is.EqualTo(2));
        }

        [Test]
        public void Bastion_이동하지않은Player기물은첫피해만2감소한다()
        {
            var board = new BoardState();
            PieceRuntimeState bastion = Spawn(board, "bastion", new Vector2Int(4, 4), true);
            PieceRuntimeState enemy = Spawn(board, "executioner", new Vector2Int(4, 5), false);

            bastion.ResetOwnTurnMovement();
            bastion.ResetBastionFortifyCycle();

            int first = DamageResolver.ApplyDamage(bastion, 4, enemy);
            int second = DamageResolver.ApplyDamage(bastion, 4, enemy);

            Assert.That(first, Is.EqualTo(2));
            Assert.That(second, Is.EqualTo(4));
        }

        [Test]
        public void FieldCommander_CommandAttack은다음공격1회피해를1증가시킨다()
        {
            var board = new BoardState();
            PieceRuntimeState commander = Spawn(board, "field_commander", new Vector2Int(4, 4), true);
            PieceRuntimeState ally = Spawn(board, "executioner", new Vector2Int(5, 4), true);
            PieceRuntimeState enemy = Spawn(board, "marshal", new Vector2Int(5, 5), false);

            PieceAbilityDefinition ability = FindAbility(
                commander,
                FourStarAbilityIds.FieldCommanderAttack);

            Assert.That(
                PieceAbilityService.ExecuteAbility(
                    ability,
                    new AbilityExecutionContext(
                        commander,
                        ally,
                        ally.BoardPosition,
                        board: board)).Success,
                Is.True);

            Assert.That(DamageResolver.ApplyDamage(enemy, 1, ally), Is.EqualTo(2));
            Assert.That(DamageResolver.ApplyDamage(enemy, 1, ally), Is.EqualTo(1));
        }

        [Test]
        public void GrandCannon_공격후십자인접적최대2개에보조피해를준다()
        {
            var board = new BoardState();
            var hooks = new BattleHooks();

            PieceRuntimeState cannon = Spawn(board, "grand_cannon", new Vector2Int(4, 1), true);
            PieceRuntimeState main = Spawn(board, "marshal", new Vector2Int(4, 4), false);
            PieceRuntimeState sideA = Spawn(board, "marshal", new Vector2Int(4, 5), false);
            PieceRuntimeState sideB = Spawn(board, "marshal", new Vector2Int(5, 4), false);
            PieceRuntimeState sideC = Spawn(board, "marshal", new Vector2Int(3, 4), false);

            int a = sideA.CurrentHp;
            int b = sideB.CurrentHp;
            int c = sideC.CurrentHp;

            CombatResult result = CombatResolver.ResolveAttack(cannon, main, hooks);
            hooks.RaiseAfterAttack(result);

            int damaged =
                (sideA.CurrentHp < a ? 1 : 0) +
                (sideB.CurrentHp < b ? 1 : 0) +
                (sideC.CurrentHp < c ? 1 : 0);

            Assert.That(damaged, Is.EqualTo(2));
        }

        [Test]
        public void WarCleric_공격후인접최저HP아군을1회복한다()
        {
            var board = new BoardState();
            var hooks = new BattleHooks();

            PieceRuntimeState cleric = Spawn(board, "war_cleric", new Vector2Int(4, 4), true);
            PieceRuntimeState ally = Spawn(board, "marshal", new Vector2Int(5, 4), true);
            PieceRuntimeState enemy = Spawn(board, "marshal", new Vector2Int(4, 5), false);
            ally.CurrentHp = 2;

            CombatResult result = CombatResolver.ResolveAttack(cleric, enemy, hooks);
            hooks.RaiseAfterAttack(result);

            Assert.That(ally.CurrentHp, Is.EqualTo(3));
        }

        [Test]
        public void Archmage_공격적중후상태이상하나를부여한다()
        {
            var board = new BoardState();
            var hooks = new BattleHooks();

            PieceRuntimeState archmage = Spawn(board, "archmage", new Vector2Int(4, 4), true);
            PieceRuntimeState enemy = Spawn(board, "grand_guardian", new Vector2Int(5, 5), false);

            CombatResult result = CombatResolver.ResolveAttack(archmage, enemy, hooks);
            hooks.RaiseAfterAttack(result);

            Assert.That(enemy.StatusEffects.Count, Is.GreaterThan(0));
        }

        [Test]
        public void SiegeChariot_파괴가능장애물하나를넘어이동후장애물을제거한다()
        {
            var board = new BoardState();
            var hooks = new BattleHooks();

            PieceRuntimeState siege = Spawn(board, "siege_chariot", new Vector2Int(1, 1), true);
            Vector2Int obstacle = new Vector2Int(2, 1);
            Vector2Int destination = new Vector2Int(3, 1);
            board.GetTile(obstacle).SetObstacle(true, true);

            MovementResult movement = MovementResolver.GetReachableTiles(siege, board);
            Assert.That(movement.MoveTiles.Contains(destination), Is.True);

            board.ClearPiece(siege);
            hooks.RaiseBeforeMove(siege, siege.BoardPosition, destination);
            Vector2Int origin = siege.BoardPosition;
            siege.BoardPosition = destination;
            board.GetTile(destination).OccupyingPiece = siege;
            hooks.RaiseAfterMove(siege, origin, destination);

            Assert.That(board.GetTile(obstacle).HasObstacle, Is.False);
        }

        [Test]
        public void Gatekeeper_이동후인접빈칸하나를봉쇄한다()
        {
            var board = new BoardState();
            var hooks = new BattleHooks();

            PieceRuntimeState gatekeeper = Spawn(board, "gatekeeper", new Vector2Int(3, 3), true);
            Spawn(board, "marshal", new Vector2Int(6, 3), false);

            Vector2Int origin = gatekeeper.BoardPosition;
            Vector2Int destination = new Vector2Int(4, 3);

            board.ClearPiece(gatekeeper);
            hooks.RaiseBeforeMove(gatekeeper, origin, destination);
            gatekeeper.BoardPosition = destination;
            board.GetTile(destination).OccupyingPiece = gatekeeper;
            hooks.RaiseAfterMove(gatekeeper, origin, destination);

            Assert.That(TileBlockService.GetBlockedPositions(board).Count, Is.EqualTo(1));
        }

        [Test]
        public void HighPriestAI_부상아군Heal2후보를생성한다()
        {
            var board = new BoardState();
            PieceRuntimeState priest = Spawn(board, "high_priest", new Vector2Int(4, 4), false);
            PieceRuntimeState ally = Spawn(board, "grand_guardian", new Vector2Int(6, 4), false);
            ally.CurrentHp = 2;

            List<AIActionCandidate> candidates =
                EnemyAIAbilityCandidateBuilder.BuildCandidates(board, priest);

            bool found = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Ability?.AbilityId == FourStarAbilityIds.HighPriestBlessing)
                {
                    found = true;
                    break;
                }
            }

            Assert.That(found, Is.True);
        }

        [Test]
        public void FieldCommanderAI_공격명령과이동명령후보를만든다()
        {
            var board = new BoardState();
            PieceRuntimeState commander = Spawn(board, "field_commander", new Vector2Int(4, 4), false);
            Spawn(board, "executioner", new Vector2Int(5, 4), false);

            List<AIActionCandidate> candidates =
                EnemyAIAbilityCandidateBuilder.BuildCandidates(board, commander);

            bool attack = false;
            bool move = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                string id = candidates[i].Ability?.AbilityId;
                if (id == FourStarAbilityIds.FieldCommanderAttack) attack = true;
                if (id == FourStarAbilityIds.FieldCommanderMove) move = true;
            }

            Assert.That(attack, Is.True);
            Assert.That(move, Is.True);
        }

        private PieceRuntimeState Spawn(
            BoardState board,
            string pieceId,
            Vector2Int position,
            bool isPlayer)
        {
            PieceDefinition definition = _database.FindById(pieceId);
            Assert.That(definition, Is.Not.Null, pieceId);

            var state = new PieceRuntimeState(definition, position, isPlayer);
            board.GetTile(position).OccupyingPiece = state;
            return state;
        }

        private static PieceAbilityDefinition FindAbility(
            PieceRuntimeState piece,
            string abilityId)
        {
            for (int i = 0; i < piece.Definition.Abilities.Length; i++)
            {
                PieceAbilityDefinition ability = piece.Definition.Abilities[i];
                if (ability != null && ability.AbilityId == abilityId) return ability;
            }

            Assert.Fail("Ability not found: " + abilityId);
            return null;
        }
    }
}
#endif
