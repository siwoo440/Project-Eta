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
using ProjectEta.Run;

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day93ThreeStarIntegrationTests
    {
        private PieceDatabase _database;

        [SetUp]
        public void SetUp()
        {
            AbilityBoardRegistry.Clear();
            TemporarySummonService.ClearAll();
            MovementRangeModifierService.ClearAll();
            TileBlockService.ClearAll();
            AIAbilityCandidateRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();
            Assert.That(EditorApplication.ExecuteMenuItem("Project Eta/Day 93/Patch Three Star Abilities"), Is.True);

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
            AbilityBoardRegistry.Clear();
            AIAbilityCandidateRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();
        }

        [Test]
        public void ThreeStarRoster_정확히18종이고현재스탯을유지한다()
        {
            var expected = new Dictionary<string, Vector2Int>
            {
                ["paladin"] = new Vector2Int(5, 4),
                ["war_chariot"] = new Vector2Int(5, 4),
                ["grenadier"] = new Vector2Int(4, 4),
                ["pikeman"] = new Vector2Int(5, 4),
                ["crossbowman"] = new Vector2Int(4, 5),
                ["guardian"] = new Vector2Int(6, 3),
                ["hunter"] = new Vector2Int(4, 5),
                ["falcon"] = new Vector2Int(4, 5),
                ["unicorn"] = new Vector2Int(4, 4),
                ["gryphon"] = new Vector2Int(5, 4),
                ["dragon_horse"] = new Vector2Int(5, 4),
                ["dragon_king"] = new Vector2Int(6, 4),
                ["artillery"] = new Vector2Int(4, 5),
                ["vanguard"] = new Vector2Int(5, 5),
                ["tactician"] = new Vector2Int(4, 3),
                ["medic"] = new Vector2Int(5, 2),
                ["summoner"] = new Vector2Int(4, 3),
                ["sniper"] = new Vector2Int(3, 5)
            };

            int count = 0;
            var ids = new HashSet<string>();

            for (int i = 0; i < _database.Definitions.Count; i++)
            {
                PieceDefinition definition = _database.Definitions[i];
                if (definition == null || definition.Grade != PieceGrade.ThreeStar) continue;

                count++;
                Assert.That(ids.Add(definition.PieceId), Is.True, definition.PieceId);
                Assert.That(expected.ContainsKey(definition.PieceId), Is.True, definition.PieceId);

                Vector2Int stat = expected[definition.PieceId];
                Assert.That(definition.BaseHp, Is.EqualTo(stat.x), definition.PieceId + " HP");
                Assert.That(definition.BaseAtk, Is.EqualTo(stat.y), definition.PieceId + " ATK");
                Assert.That(definition.MovementRules, Is.Not.Null);
                Assert.That(definition.MovementRules.Length, Is.GreaterThan(0), definition.PieceId + " MovementRules");
            }

            Assert.That(count, Is.EqualTo(18));
        }

        [Test]
        public void ThreeStarExplicitAbilities_기획서에명시된8종이연결된다()
        {
            string[] pieceIds =
            {
                "paladin", "war_chariot", "guardian", "hunter",
                "vanguard", "tactician", "medic", "summoner"
            };

            for (int i = 0; i < pieceIds.Length; i++)
            {
                PieceDefinition piece = _database.FindById(pieceIds[i]);
                Assert.That(piece, Is.Not.Null, pieceIds[i]);
                Assert.That(piece.Abilities.Length, Is.GreaterThan(0), pieceIds[i]);
                Assert.That(piece.Abilities[0], Is.Not.Null, pieceIds[i]);
            }
        }

        [Test]
        public void PaladinGuard_인접아군피해를전투당1회1감소한다()
        {
            var board = new BoardState();

            PieceRuntimeState paladin = Spawn(board, "paladin", new Vector2Int(3, 3), true);
            PieceRuntimeState ally = Spawn(board, "medic", new Vector2Int(4, 3), true);
            PieceRuntimeState attacker = Spawn(board, "hunter", new Vector2Int(4, 4), false);

            int first = DamageResolver.ApplyDamage(ally, 3, attacker);
            int second = DamageResolver.ApplyDamage(ally, 3, attacker);

            Assert.That(first, Is.EqualTo(2));
            Assert.That(second, Is.EqualTo(3));
            Assert.That(paladin.HasUsedBattleAbility(ThreeStarPassiveAbilityResolver.PaladinGuardId), Is.True);
        }

        [Test]
        public void GuardianEscort_왕피해만전투당1회감소한다()
        {
            var board = new BoardState();

            PieceRuntimeState guardian = Spawn(board, "guardian", new Vector2Int(3, 3), true);
            PieceRuntimeState king = Spawn(board, "king", new Vector2Int(4, 3), true);
            PieceRuntimeState attacker = Spawn(board, "hunter", new Vector2Int(4, 4), false);

            int applied = DamageResolver.ApplyDamage(king, 2, attacker);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(guardian.HasUsedBattleAbility(ThreeStarPassiveAbilityResolver.GuardianEscortId), Is.True);
        }

        [Test]
        public void HunterTracking_대상HP절반이하면피해1증가한다()
        {
            var board = new BoardState();

            PieceRuntimeState hunter = Spawn(board, "hunter", new Vector2Int(2, 2), true);
            PieceRuntimeState target = Spawn(board, "paladin", new Vector2Int(3, 2), false);
            target.CurrentHp = 2;

            int applied = DamageResolver.ApplyDamage(target, 1, hunter);

            Assert.That(applied, Is.EqualTo(2));
        }

        [Test]
        public void VanguardFrontline_전방공격피해가1증가한다()
        {
            var board = new BoardState();

            PieceRuntimeState vanguard = Spawn(board, "vanguard", new Vector2Int(4, 4), true);
            PieceRuntimeState target = Spawn(board, "paladin", new Vector2Int(4, 5), false);

            int applied = DamageResolver.ApplyDamage(target, 1, vanguard);

            Assert.That(applied, Is.EqualTo(2));
        }

        [Test]
        public void WarChariotCharge_최근3칸이동후첫공격만1증가한다()
        {
            var board = new BoardState();

            PieceRuntimeState chariot = Spawn(board, "war_chariot", new Vector2Int(1, 1), true);
            PieceRuntimeState target = Spawn(board, "paladin", new Vector2Int(5, 4), false);

            board.GetTile(new Vector2Int(1, 1)).OccupyingPiece = null;
            chariot.BoardPosition = new Vector2Int(1, 4);
            board.GetTile(chariot.BoardPosition).OccupyingPiece = chariot;

            int first = DamageResolver.ApplyDamage(target, 1, chariot);
            int second = DamageResolver.ApplyDamage(target, 1, chariot);

            Assert.That(first, Is.EqualTo(2));
            Assert.That(second, Is.EqualTo(1));
        }

        [Test]
        public void MedicAI_부상아군HealAbility후보를생성한다()
        {
            var board = new BoardState();

            PieceRuntimeState medic = Spawn(board, "medic", new Vector2Int(4, 4), false);
            PieceRuntimeState ally = Spawn(board, "guardian", new Vector2Int(5, 4), false);
            ally.CurrentHp = 2;

            List<AIActionCandidate> candidates =
                EnemyAIAbilityCandidateBuilder.BuildCandidates(board, medic);

            bool found = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                AIActionCandidate candidate = candidates[i];
                if (candidate.ActionType == AIActionType.Ability &&
                    candidate.Ability != null &&
                    candidate.Ability.AbilityId == "three_medic_first_aid" &&
                    object.ReferenceEquals(candidate.TargetPiece, ally))
                {
                    found = true;
                    break;
                }
            }

            Assert.That(found, Is.True);
        }

        [Test]
        public void TacticianAI_인접아군이동보정Ability후보를생성한다()
        {
            var board = new BoardState();

            PieceRuntimeState tactician = Spawn(board, "tactician", new Vector2Int(4, 4), false);
            PieceRuntimeState ally = Spawn(board, "vanguard", new Vector2Int(5, 4), false);

            List<AIActionCandidate> candidates =
                EnemyAIAbilityCandidateBuilder.BuildCandidates(board, tactician);

            bool found = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                AIActionCandidate candidate = candidates[i];
                if (candidate.ActionType == AIActionType.Ability &&
                    candidate.Ability != null &&
                    candidate.Ability.AbilityId == "three_tactician_order" &&
                    object.ReferenceEquals(candidate.TargetPiece, ally))
                {
                    found = true;
                    break;
                }
            }

            Assert.That(found, Is.True);
        }


        [Test]
        public void TacticianOrder_같은기물에중복적용되지않는다()
        {
            var board = new BoardState();
            PieceRuntimeState tactician = Spawn(board, "tactician", new Vector2Int(4, 4), true);
            PieceRuntimeState ally = Spawn(board, "vanguard", new Vector2Int(5, 4), true);
            PieceAbilityDefinition ability = FindAbility(tactician, "three_tactician_order");

            AbilityExecutionResult first = PieceAbilityService.ExecuteAbility(
                ability,
                new AbilityExecutionContext(
                    tactician,
                    ally,
                    ally.BoardPosition,
                    board: board));

            AbilityExecutionResult second = PieceAbilityService.ExecuteAbility(
                ability,
                new AbilityExecutionContext(
                    tactician,
                    ally,
                    ally.BoardPosition,
                    board: board));

            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.False);
        }

        [Test]
        public void Summoner_임시폰은동시에1개만유지한다()
        {
            var board = new BoardState();
            PieceRuntimeState summoner = Spawn(board, "summoner", new Vector2Int(4, 4), false);
            PieceAbilityDefinition ability = FindAbility(summoner, "three_summoner_temporary_pawn");

            AbilityExecutionResult first = PieceAbilityService.ExecuteAbility(
                ability,
                new AbilityExecutionContext(
                    summoner,
                    targetPosition: new Vector2Int(5, 4),
                    board: board));

            AbilityExecutionResult second = PieceAbilityService.ExecuteAbility(
                ability,
                new AbilityExecutionContext(
                    summoner,
                    targetPosition: new Vector2Int(3, 4),
                    board: board));

            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.False);
            Assert.That(TemporarySummonService.CountForSource(summoner), Is.EqualTo(1));
        }

        [Test]
        public void EnemyAIAbilityExecution_Heal후EnemyTurn을정상종료한다()
        {
            var run = new RunState(3);
            var turn = new TurnManager();
            var hooks = new BattleHooks();

            PieceRuntimeState medic =
                new PieceRuntimeState(_database.FindById("medic"), new Vector2Int(4, 4), false);
            PieceRuntimeState ally =
                new PieceRuntimeState(_database.FindById("guardian"), new Vector2Int(5, 4), false);

            ally.CurrentHp = 2;
            run.Board.GetTile(medic.BoardPosition).OccupyingPiece = medic;
            run.Board.GetTile(ally.BoardPosition).OccupyingPiece = ally;

            turn.MarkInitialKingPlaced();
            Assert.That(turn.TryEndDeploymentTurn(), Is.True);
            Assert.That(turn.TryCompletePlayerAction(), Is.True);
            Assert.That(turn.CurrentState, Is.EqualTo(TurnState.EnemyTurn));

            PieceAbilityDefinition ability = FindAbility(medic, "three_medic_first_aid");
            var action = new AIActionCandidate(
                medic,
                medic.BoardPosition,
                ally.BoardPosition,
                ally,
                ability,
                1000);

            bool executed = EnemyAIActionExecutor.TryExecute(
                action,
                run,
                turn,
                hooks,
                null,
                out CombatResult combatResult);

            Assert.That(executed, Is.True);
            Assert.That(combatResult, Is.Null);
            Assert.That(ally.CurrentHp, Is.EqualTo(3));
            Assert.That(turn.CurrentState, Is.Not.EqualTo(TurnState.EnemyTurn));
        }

        [Test]
        public void RangedThreeStar_기존MovementResolver에서원거리공격후보를유지한다()
        {
            var board = new BoardState();
            PieceRuntimeState sniper = Spawn(board, "sniper", new Vector2Int(4, 4), false);
            PieceRuntimeState target = Spawn(board, "paladin", new Vector2Int(4, 7), true);

            MovementResult result = MovementResolver.GetReachableTiles(sniper, board);

            Assert.That(result.AttackTiles.Contains(target.BoardPosition), Is.True);
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
            Assert.That(piece, Is.Not.Null);

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
