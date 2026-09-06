using UnityEngine; // Mathf 사용

namespace ProjectEta.King
{
    public sealed class KingCombatHudPresentation
    {
        public string KingName { get; } // King 표시 이름
        public string HpText { get; } // King HP 표시 문구
        public string PassiveName { get; } // 패시브 이름
        public string PrimaryStatus { get; } // 핵심 패시브 상태
        public string SecondaryStatus { get; } // 보조 패시브 상태

        public KingCombatHudPresentation(string kingName, string hpText, string passiveName, string primaryStatus, string secondaryStatus)
        {
            KingName = kingName ?? string.Empty; // King 이름 저장
            HpText = hpText ?? string.Empty; // HP 문구 저장
            PassiveName = passiveName ?? string.Empty; // 패시브 이름 저장
            PrimaryStatus = primaryStatus ?? string.Empty; // 핵심 상태 저장
            SecondaryStatus = secondaryStatus ?? string.Empty; // 보조 상태 저장
        }

        public static KingCombatHudPresentation Build(KingRunState state, int kingHp, bool isDeploymentTurn)
        {
            KingRunState safeState = state ?? new KingRunState(); // 누락 King 상태 기본값 보정
            KingSelectionPresentation presentation = KingSelectionPresentationCatalog.Get(safeState.Archetype); // 현재 King 표시 정보 조회
            string hpText = $"HP  ♥ {Mathf.Max(0, kingHp)}"; // 현재 King HP 표시 생성

            if (safeState.Archetype == KingArchetype.Attack)
            {
                string rageDots = BuildRageDots(safeState.RageStacks); // 공격형 격노 점 표시 생성
                string secondary = safeState.RageStacks > 0
                    ? $"다음 King 공격 ATK +{safeState.RageStacks}"
                    : "다음 공격 보너스 없음"; // 공격형 다음 공격 보너스 문구 생성
                return new KingCombatHudPresentation(presentation.DisplayName, hpText, presentation.PassiveName, $"격노  {rageDots}", secondary); // 공격형 HUD 표시 반환
            }

            if (safeState.Archetype == KingArchetype.Defense)
            {
                string barrier = safeState.BarrierActive ? "방벽  ◆ 활성" : "방벽  ◇ 비활성"; // 방어형 방벽 상태 표시 생성
                string movement = safeState.KingMovedThisTurn
                    ? "이번 턴 King 이동함 · 방벽 획득 불가"
                    : "이번 턴 이동 안 함 · 턴 종료 시 방벽 준비"; // 방어형 이동 여부 문구 생성
                return new KingCombatHudPresentation(presentation.DisplayName, hpText, presentation.PassiveName, barrier, movement); // 방어형 HUD 표시 반환
            }

            if (safeState.Archetype == KingArchetype.Strategy)
            {
                string primary = safeState.StrategyPreparationPending
                    ? "전술적 준비 · 카드 선택 중..."
                    : isDeploymentTurn
                        ? "전술적 준비 · 배치 턴"
                        : "전술적 준비 · 다음 배치 턴 발동"; // 전략형 현재 준비 상태 문구 생성
                string secondary = safeState.StrategyPreparationPending
                    ? "선택 완료 전 배치 입력 대기"
                    : "배치 턴마다 덱 위 3장 중 1장 선택"; // 전략형 보조 안내 생성
                return new KingCombatHudPresentation(presentation.DisplayName, hpText, presentation.PassiveName, primary, secondary); // 전략형 HUD 표시 반환
            }

            return new KingCombatHudPresentation(presentation.DisplayName, hpText, presentation.PassiveName, "특수 패시브 없음", "기본 King 규칙으로 전투"); // 기본 King HUD 표시 반환
        }

        private static string BuildRageDots(int rageStacks)
        {
            int safeStacks = Mathf.Clamp(rageStacks, 0, KingRunState.AttackRageMaxStacks); // 격노 스택 안전 범위 보정
            string result = string.Empty; // 격노 점 표시 초기화

            for (int i = 0; i < KingRunState.AttackRageMaxStacks; i++)
            {
                if (i > 0) result += " "; // 격노 점 간격 추가
                result += i < safeStacks ? "●" : "○"; // 활성·비활성 격노 점 추가
            }

            return result; // 완성 격노 점 표시 반환
        }
    }
}
