using System; // 재료 설정 직렬화
using UnityEngine; // 런타임 설정 에셋
using ProjectEta.Pieces; // 기물과 등급

namespace ProjectEta.Run // 경제 설정 영역
{ // 범위 시작
    [Serializable] // 재료별 설정 저장
    public sealed class MaterialBalanceWeight // 1성 재료별 상대 가중치
    { // 범위 시작
        public string pieceId; // 대상 기물 ID
        public int weight = 100; // 기본 균등 가중치
    } // 범위 종료
    [CreateAssetMenu(fileName = "RunBalance96", menuName = "ProjectEta/Run Balance Profile")] // 설정 생성 메뉴
    public sealed class RunBalanceProfile : ScriptableObject // 재료 공급과 경제 임시 기준
    { // 범위 시작
        [SerializeField] private MaterialBalanceWeight[] _materialWeights = Array.Empty<MaterialBalanceWeight>(); // 재료 가중치 목록
        [SerializeField] private string _profileId = "day96-baseline"; // 측정 기준 식별자
        private static RunBalanceProfile _current; // 런타임 설정 캐시
        public int startingGold = 100; // 기존 시작 재화
        public int oneStarPrice = 25; // 기존 1성 구매 가격
        public int purchasePhaseStep = 5; // 구매 페이즈 가산
        public int purchaseStageStep = 3; // 구매 깊이 구간 가산
        public int removePrice = 35; // 기존 제거 기본 가격
        public int removePhaseStep = 5; // 제거 페이즈 가산
        public int healPrice = 20; // 기존 회복 기본 가격
        public int healPhaseStep = 4; // 회복 페이즈 가산
        public int upgradePrice = 45; // 기존 강화 기본 가격
        public int upgradePhaseStep = 8; // 강화 페이즈 가산
        public int upgradeStageStep = 5; // 강화 깊이 구간 가산
        public int battleGold = 15; // 일반 승리 임시 보상
        public int eliteGold = 25; // 정예 승리 임시 보상
        public int midBossGold = 40; // 중간 보스 임시 보상
        public int finalBossGold = 60; // 최종 보스 임시 보상
        public int rewardPhaseStep = 5; // 승리 보상 페이즈 가산
        public string ProfileId => _profileId ?? "day96-baseline"; // 설정 버전 표시
        public static RunBalanceProfile Current // 실제 게임 설정 조회
        { // 범위 시작
            get // 설정 로드 범위
            { // 범위 시작
                if (_current == null) // 최초 조회 확인
                { // 범위 시작
                    _current = Resources.Load<RunBalanceProfile>("RunBalance96"); // 빌드 포함 설정 로드
                    if (_current == null) // 기존 프로젝트 호환
                    { // 범위 시작
                        _current = CreateInstance<RunBalanceProfile>(); // 기본값 임시 설정
                        _current.hideFlags = HideFlags.HideAndDontSave; // 임시 에셋 저장 제외
                    } // 범위 종료
                } // 범위 종료
                return _current; // 현재 기준 반환
            } // 범위 종료
        } // 범위 종료
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 시작 캐시 초기화
        private static void ResetCache() // 이전 플레이 참조 초기화
        { // 범위 시작
            _current = null; // 설정 다시 로드
        } // 범위 종료
        public int GetMaterialWeight(PieceDefinition card) // 기물별 재료 가중치 조회
        { // 범위 시작
            if (card == null) // 잘못된 기물 확인
            { // 범위 시작
                return 100; // 안전 기본값
            } // 범위 종료
            foreach (var item in _materialWeights ?? Array.Empty<MaterialBalanceWeight>()) // 설정된 재료 순회
            { // 범위 시작
                if (item != null && item.pieceId == card.PieceId) // 동일 재료 확인
                { // 범위 시작
                    return item.weight <= 0 ? 100 : Mathf.Clamp(item.weight, 1, 10000); // 재료 누락과 과도한 가중치 방지
                } // 범위 종료
            } // 범위 종료
            return 100; // 미등록 재료 균등 적용
        } // 범위 종료
        public int GetBattleGold(StageType type, int phase) // 승리 보상 계산
        { // 범위 시작
            int amount; // 기본 보상
            switch (type) // 전투 종류 분기
            { // 범위 시작
                case StageType.Battle: // 일반 전투 보상
                    amount = battleGold; // 기준 보상 선택
                    break; // 분기 종료
                case StageType.Elite: // 정예 전투 보상
                    amount = eliteGold; // 기준 보상 선택
                    break; // 분기 종료
                case StageType.MidBoss: // 중간 보스 보상
                    amount = midBossGold; // 기준 보상 선택
                    break; // 분기 종료
                case StageType.FinalBoss: // 최종 보스 보상
                    amount = finalBossGold; // 기준 보상 선택
                    break; // 분기 종료
                default: return 0; // 비전투 보상 제외
            } // 범위 종료
            return ClampCost((long)amount + (Mathf.Clamp(phase, 1, 5) - 1) * (long)Mathf.Max(0, rewardPhaseStep)); // 진행도 보상 보정
        } // 범위 종료
        public static int ClampCost(long value) // 가격과 지급량 안전 범위
        { // 범위 시작
            return (int)Math.Max(1L, Math.Min(100000L, value)); // 음수와 과도한 가격 차단
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
