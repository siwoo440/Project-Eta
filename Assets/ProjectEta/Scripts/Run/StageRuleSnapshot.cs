using System; // 저장 직렬화
using System.Collections.Generic; // 적 배치 목록
using ProjectEta.Round; // 라운드 설정
using UnityEngine; // 런타임 데이터 객체

namespace ProjectEta.Run // 스테이지 규칙 영역
{ // 영역 시작
    [Serializable] // 저장 대상 지정
    public sealed class StageRuleSnapshot // 현재 런의 스테이지 규칙
    { // 타입 시작
        public int phase; // 적용 페이즈
        public string stageDefinitionId; // 스테이지 정의 ID
        public string displayName; // 라운드 표시 이름
        public int stageType; // 전투 종류
        public int turnLimit; // 전투 제한 턴
        public int victoryGold; // 승리 Gold
        public bool isBossRound; // 보스 전투 여부
        public string bossResourceName; // 보스 리소스 이름
        public Vector2Int bossAnchor; // 보스 시작 좌표
        public List<EnemySpawnDefinition> initialEnemies = new List<EnemySpawnDefinition>(); // 시작 적 배치
        public List<EnemySpawnDefinition> reinforcements = new List<EnemySpawnDefinition>(); // 증원 배치
        public bool IsValid => phase >= 1 && phase <= 5 && !string.IsNullOrWhiteSpace(stageDefinitionId) && turnLimit > 0 && victoryGold >= 0 && initialEnemies != null && reinforcements != null; // 복원 규칙 유효성
        public RoundDefinition CreateRuntimeRound() // 저장 규칙의 독립 라운드 생성
        { // 메서드 시작
            var round = ScriptableObject.CreateInstance<RoundDefinition>(); // 런타임 라운드 생성
            round.hideFlags = HideFlags.HideAndDontSave; // 에셋 저장 제외
            round.ConfigureRuntime(displayName, turnLimit, isBossRound, bossResourceName, bossAnchor, initialEnemies, reinforcements); // 저장 규칙 적용
            return round; // 독립 라운드 반환
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
