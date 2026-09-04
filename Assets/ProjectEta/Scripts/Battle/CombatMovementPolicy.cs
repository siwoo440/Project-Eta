using ProjectEta.Pieces; // PieceDefinition과 PieceRoleTag를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 정책을 모아두는 네임스페이스
{
    public static class CombatMovementPolicy // 처치 후 공격자가 대상 칸을 점유할지 결정하는 공통 정책
    {
        public static bool ShouldOccupyDefenderTileAfterKill(PieceDefinition attackerDefinition) // 공격자 정의로 처치 후 전진 여부를 계산
        {
            if (attackerDefinition == null) return true; // 데이터가 없으면 기존 근접 전투 동작을 보존
            bool isRanged = (attackerDefinition.RoleTags & PieceRoleTag.Ranged) != 0; // Ranged 역할 태그 보유 여부 확인
            return !isRanged; // 원거리 공격은 처치해도 원위치, 그 외 공격은 기존처럼 대상 칸 점유
        }
    }
}
