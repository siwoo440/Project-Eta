using System.Collections.Generic;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.AI
{
    public static class EnemyAIAbilityCandidateBuilder
    {
        private static readonly Vector2Int[] Around8 =
        {
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        public static List<AIActionCandidate> BuildCandidates(
            BoardState board,
            PieceRuntimeState actor)
        {
            var result = new List<AIActionCandidate>();

            if (board == null ||
                actor == null ||
                actor.IsDead ||
                actor.Definition == null)
            {
                return result;
            }

            PieceAbilityDefinition[] abilities = actor.Definition.Abilities;

            for (int abilityIndex = 0; abilityIndex < abilities.Length; abilityIndex++)
            {
                PieceAbilityDefinition ability = abilities[abilityIndex];

                if (ability == null ||
                    ability.Trigger != AbilityTrigger.Active ||
                    ability.ActionCost != AbilityActionCost.PlayerAction)
                {
                    continue;
                }

                AbilityEffectData[] effects = ability.Effects;
                if (effects.Length == 0 || effects[0] == null) continue;

                switch (effects[0].EffectType)
                {
                    case AbilityEffectType.Heal:
                        AddAllyTargetCandidates(board, actor, ability, result, requireMissingHp: true);
                        break;

                    case AbilityEffectType.ModifyMoveRange:
                        AddAllyTargetCandidates(board, actor, ability, result, requireMissingHp: false);
                        break;

                    case AbilityEffectType.ModifyDamage:
                        AddAllyTargetCandidates(board, actor, ability, result, requireMissingHp: false);
                        break;

                    case AbilityEffectType.ApplyStatus: // 현자 적 상태 이상 후보
                        AddEnemyStatusCandidates(board, actor, ability, result); // 적 대상 후보 추가
                        break; // 상태 이상 분기 종료
                    case AbilityEffectType.Summon:
                        AddSummonCandidates(board, actor, ability, result);
                        break;
                }
            }

            return result;
        }

        private static void AddAllyTargetCandidates(
            BoardState board,
            PieceRuntimeState actor,
            PieceAbilityDefinition ability,
            List<AIActionCandidate> result,
            bool requireMissingHp)
        {
            int radius = ResolveRadius(ability);
            IReadOnlyList<PieceRuntimeState> pieces = AbilityBoardRegistry.GetUniquePieces(board);

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceRuntimeState target = pieces[i];

                if (target == null ||
                    target.IsDead ||
                    (object.ReferenceEquals(target, actor) && !FiveStarActiveAbilityService.IsSageChoice(ability)) || // 현자의 자기 회복과 보호 허용
                    target.IsPlayerPiece != actor.IsPlayerPiece)
                {
                    continue;
                }

                if (requireMissingHp &&
                    target.Definition != null &&
                    target.CurrentHp >= target.Definition.BaseHp)
                {
                    continue;
                }

                int distance = Mathf.Max(
                    Mathf.Abs(actor.BoardPosition.x - target.BoardPosition.x),
                    Mathf.Abs(actor.BoardPosition.y - target.BoardPosition.y));

                if (distance > radius) continue;

                int score = EnemyAIAbilityScoreEvaluator.Evaluate(
                    actor,
                    target,
                    target.BoardPosition,
                    ability,
                    board);

                if (score <= int.MinValue / 8) continue;

                result.Add(new AIActionCandidate(
                    actor,
                    actor.BoardPosition,
                    target.BoardPosition,
                    target,
                    ability,
                    score));
            }
        }

        private static void AddEnemyStatusCandidates(BoardState board, PieceRuntimeState actor, PieceAbilityDefinition ability, List<AIActionCandidate> result) // 적 상태 이상 후보 구성
        { // 상태 이상 후보 범위
            foreach (var target in AbilityBoardRegistry.GetUniquePieces(board)) // 살아 있는 적 순회
            { // 대상 순회 범위
                if (target.IsDead || target.IsPlayerPiece == actor.IsPlayerPiece) // 대상 진영 확인
                { // 대상 제외 범위
                    continue; // 아군과 사망 기물 제외
                } // 대상 제외 범위 종료
                int score = EnemyAIAbilityScoreEvaluator.Evaluate(actor, target, target.BoardPosition, ability, board); // 실제 미리보기 기반 평가
                if (score > int.MinValue / 8) // 적용 가능한 후보 확인
                { // 후보 추가 범위
                    result.Add(new AIActionCandidate(actor, actor.BoardPosition, target.BoardPosition, target, ability, score)); // 선택 능력 참조 유지
                } // 후보 추가 범위 종료
            } // 대상 순회 범위 종료
        } // 상태 이상 후보 범위 종료

        private static void AddSummonCandidates(
            BoardState board,
            PieceRuntimeState actor,
            PieceAbilityDefinition ability,
            List<AIActionCandidate> result)
        {
            int radius = Mathf.Max(1, ResolveRadius(ability));

            for (int i = 0; i < Around8.Length; i++)
            {
                Vector2Int direction = Around8[i];

                for (int step = 1; step <= radius; step++)
                {
                    Vector2Int target = actor.BoardPosition + direction * step;
                    if (!board.IsInsideBoard(target)) break;

                    int score = EnemyAIAbilityScoreEvaluator.Evaluate(
                        actor,
                        null,
                        target,
                        ability,
                        board);

                    if (score <= int.MinValue / 8) continue;

                    result.Add(new AIActionCandidate(
                        actor,
                        actor.BoardPosition,
                        target,
                        null,
                        ability,
                        score));
                }
            }
        }

        private static int ResolveRadius(PieceAbilityDefinition ability)
        {
            if (ability == null) return 1;

            AbilityEffectData[] effects = ability.Effects;
            int radius = 1;

            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null)
                {
                    radius = Mathf.Max(radius, effects[i].Radius);
                }
            }

            return radius;
        }
    }
}
