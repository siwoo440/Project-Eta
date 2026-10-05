using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectEta.Board;

namespace ProjectEta.AI
{
    public sealed class EnemyAIAdvancedPlanner
    {
        private readonly EnemyAIPlanner _basePlanner = new EnemyAIPlanner();
        private readonly EnemyAIEvaluationBudget _budget;

        public EnemyAIPerformanceStats LastPerformanceStats { get; private set; } =
            EnemyAIPerformanceStats.Empty;

        public EnemyAIAdvancedPlanner() : this(new EnemyAIEvaluationBudget())
        {
        }

        public EnemyAIAdvancedPlanner(EnemyAIEvaluationBudget budget)
        {
            _budget = budget ?? new EnemyAIEvaluationBudget();
        }

        public List<AIActionCandidate> BuildCandidates(BoardState board)
        {
            List<EnemyAIScoredCandidate> scoredCandidates =
                BuildScoredCandidates(board);

            var finalCandidates =
                new List<AIActionCandidate>(scoredCandidates.Count);

            for (int i = 0; i < scoredCandidates.Count; i++)
            {
                finalCandidates.Add(scoredCandidates[i].FinalCandidate);
            }

            return finalCandidates;
        }

        public List<EnemyAIScoredCandidate> BuildScoredCandidates(BoardState board)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var scoredCandidates = new List<EnemyAIScoredCandidate>();

            if (board == null)
            {
                stopwatch.Stop();
                LastPerformanceStats = EnemyAIPerformanceStats.Empty;
                return scoredCandidates;
            }

            List<AIActionCandidate> baseCandidates =
                _basePlanner.BuildCandidates(board);

            List<AIActionCandidate> candidates =
                EnemyAICandidatePruner.Prune(
                    board,
                    baseCandidates,
                    _budget.MaxHeavyEvaluationCandidates,
                    out int invalidOrDuplicateCount,
                    out bool budgetCapped);

            var context = new EnemyAIEvaluationContext(board);
            bool usedFallback = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                AIActionCandidate candidate = candidates[i];
                int roleBonus = 0;
                int threatScore = 0;
                int specialBonus = 0;

                try
                {
                    roleBonus =
                        EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(
                            candidate,
                            board,
                            context);

                    threatScore =
                        EnemyAIThreatScoreEvaluator.Evaluate(
                            candidate,
                            board,
                            context.ThreatMap);

                    specialBonus =
                        EnemyAISpecialScoreEvaluator.Evaluate(
                            candidate,
                            board,
                            context);
                }
                catch (Exception exception)
                {
                    usedFallback = true;
                    Debug.LogWarning(
                        $"AI 정밀 평가 실패, Base 점수로 fallback: {candidate}\n{exception.Message}");
                }

                int finalScore =
                    candidate.Score +
                    roleBonus +
                    threatScore +
                    specialBonus;

                AIActionCandidate finalCandidate;

                if (candidate.ActionType == AIActionType.Ability &&
                    candidate.Ability != null)
                {
                    // 94일차: 같은 대상에 복수 Ability가 있어도 원래 Ability 참조를 보존한다.
                    finalCandidate = new AIActionCandidate(
                        candidate.Actor,
                        candidate.Origin,
                        candidate.Target,
                        candidate.TargetPiece,
                        candidate.Ability,
                        finalScore);
                }
                else
                {
                    finalCandidate = new AIActionCandidate(
                        candidate.Actor,
                        candidate.Origin,
                        candidate.Target,
                        candidate.ActionType,
                        candidate.TargetPiece,
                        finalScore);
                }

                scoredCandidates.Add(
                    new EnemyAIScoredCandidate(
                        candidate,
                        roleBonus,
                        threatScore,
                        specialBonus,
                        finalCandidate));
            }

            stopwatch.Stop();

            int discardedCount =
                Math.Max(
                    0,
                    baseCandidates.Count - scoredCandidates.Count);

            LastPerformanceStats = new EnemyAIPerformanceStats(
                baseCandidates.Count,
                scoredCandidates.Count,
                Math.Max(discardedCount, invalidOrDuplicateCount),
                context.ThreatMap.ProbeCount,
                context.FutureMovementResolveCount,
                stopwatch.Elapsed.TotalMilliseconds,
                budgetCapped,
                usedFallback);

            return scoredCandidates;
        }

        public bool TryChooseAction(
            BoardState board,
            out AIActionCandidate selectedAction)
        {
            List<EnemyAIScoredCandidate> scoredCandidates =
                BuildScoredCandidates(board);

            if (TryChooseBestAction(
                    scoredCandidates,
                    out selectedAction))
            {
                return true;
            }

            if (_basePlanner.TryChooseAction(
                    board,
                    out selectedAction))
            {
                LastPerformanceStats =
                    LastPerformanceStats.WithFallback(true);

                return true;
            }

            selectedAction = null;
            return false;
        }

        public bool TryChooseBestAction(
            IReadOnlyList<EnemyAIScoredCandidate> scoredCandidates,
            out AIActionCandidate selectedAction)
        {
            selectedAction = null;

            if (scoredCandidates == null ||
                scoredCandidates.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < scoredCandidates.Count; i++)
            {
                AIActionCandidate candidate =
                    scoredCandidates[i]?.FinalCandidate;

                if (candidate == null) continue;

                if (selectedAction == null ||
                    IsBetterCandidate(candidate, selectedAction))
                {
                    selectedAction = candidate;
                }
            }

            return selectedAction != null;
        }

        public static bool IsBetterCandidate(
            AIActionCandidate challenger,
            AIActionCandidate currentBest)
        {
            if (challenger == null) return false;
            if (currentBest == null) return true;

            if (challenger.Score != currentBest.Score)
            {
                return challenger.Score > currentBest.Score;
            }

            if (challenger.ActionType != currentBest.ActionType)
            {
                if (challenger.ActionType == AIActionType.Attack) return true;
                if (currentBest.ActionType == AIActionType.Attack) return false;
                if (challenger.ActionType == AIActionType.Ability) return true;
                if (currentBest.ActionType == AIActionType.Ability) return false;
            }

            string challengerId =
                challenger.Actor?.Definition?.PieceId ??
                string.Empty;

            string currentId =
                currentBest.Actor?.Definition?.PieceId ??
                string.Empty;

            int idComparison = string.Compare(
                challengerId,
                currentId,
                StringComparison.Ordinal);

            if (idComparison != 0)
            {
                return idComparison < 0;
            }

            if (challenger.Origin.y != currentBest.Origin.y)
            {
                return challenger.Origin.y < currentBest.Origin.y;
            }

            if (challenger.Origin.x != currentBest.Origin.x)
            {
                return challenger.Origin.x < currentBest.Origin.x;
            }

            if (challenger.Target.y != currentBest.Target.y)
            {
                return challenger.Target.y < currentBest.Target.y;
            }

            return challenger.Target.x < currentBest.Target.x;
        }
    }
}
