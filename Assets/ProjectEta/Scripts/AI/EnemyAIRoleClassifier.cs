using ProjectEta.Pieces; // PieceDefinition, PieceCategory, PieceRoleTag를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public static class EnemyAIRoleClassifier // PieceDefinition을 34일차 기본 AI 성격 하나로 분류하는 클래스
    {
        public static EnemyAIBasicRole GetBasicRole(PieceDefinition definition) // 기물 데이터에서 기본 AI 역할을 결정하는 메서드
        {
            if (definition == null) return EnemyAIBasicRole.None; // 정의가 없으면 역할 보정 없음

            if (definition.Category == PieceCategory.Special || definition.Category == PieceCategory.Monster || definition.Category == PieceCategory.Boss) // 35일차 이후에 별도 처리할 특수·몬스터·보스면
            {
                return EnemyAIBasicRole.None; // 34일차 기본 역할 보정을 적용하지 않음
            }

            PieceRoleTag tags = definition.RoleTags; // 기물의 비트 역할 태그 읽기

            if ((tags & PieceRoleTag.Slider) != 0) return EnemyAIBasicRole.Slider; // 복합 기물은 장거리 라인을 우선해 Slider를 대표 성격으로 선택
            if ((tags & PieceRoleTag.Jumper) != 0) return EnemyAIBasicRole.Jumper; // Slider가 아니면서 Jumper면 도약형으로 분류
            if ((tags & PieceRoleTag.Melee) != 0) return EnemyAIBasicRole.Melee; // 마지막으로 단거리 근접형 분류

            return EnemyAIBasicRole.None; // 세 기본 역할에 해당하지 않으면 공통 33일차 점수만 사용
        }
    }
}
