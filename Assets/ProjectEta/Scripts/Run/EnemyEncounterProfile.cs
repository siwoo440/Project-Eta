using System.Collections.Generic; // 읽기 전용 편성 원형 목록
using ProjectEta.Pieces; // 역할 태그 사용

namespace ProjectEta.Run // 96일차 적 편성 원형 영역
{ // 범위 시작
    public sealed class EnemyEncounterProfile // 일반·정예 전투 한 종류의 역할 구성
    { // 범위 시작
        private readonly PieceRoleTag[] _preferredRoles; // 순서별 우선 역할 복사본
        public string ProfileId // 저장과 측정에 사용하는 원형 ID
        { // 속성 범위 시작
            get; // 원형 ID 조회
        } // 속성 범위 종료
        public StageType StageType // 일반·정예 구분
        { // 속성 범위 시작
            get; // 스테이지 종류 조회
        } // 속성 범위 종료
        public int EnemyCountBonus // 기본 라운드에 더할 적 수
        { // 속성 범위 시작
            get; // 원형별 추가 적 수 조회
        } // 속성 범위 종료
        public PieceRoleTag[] PreferredRoles // 순서별 우선 역할의 안전 복사본
        { // 속성 범위 시작
            get // 역할 배열 조회
            { // 범위 시작
                return (PieceRoleTag[])_preferredRoles.Clone(); // 외부 변경 없는 복사본 반환
            } // 범위 종료
        } // 속성 범위 종료
        public EnemyEncounterProfile(string profileId, StageType stageType, int enemyCountBonus, params PieceRoleTag[] preferredRoles) // 편성 원형 생성
        { // 범위 시작
            ProfileId = profileId ?? string.Empty; // null 없는 ID 저장
            StageType = stageType; // 전투 종류 저장
            EnemyCountBonus = enemyCountBonus < 0 ? 0 : enemyCountBonus; // 음수 추가 적 차단
            _preferredRoles = preferredRoles != null ? (PieceRoleTag[])preferredRoles.Clone() : new PieceRoleTag[0]; // 역할 순서 독립 복사
        } // 범위 종료
        public PieceRoleTag GetPreferredRole(int index) // 배치 순번의 우선 역할 조회
        { // 범위 시작
            if (_preferredRoles.Length == 0) // 역할 설정 누락 확인
            { // 범위 시작
                return PieceRoleTag.None; // 역할 제한 없는 기본값
            } // 범위 종료
            int safeIndex = index < 0 ? 0 : index % _preferredRoles.Length; // 반복 가능한 역할 순번 보정
            return _preferredRoles[safeIndex]; // 해당 순번 우선 역할 반환
        } // 범위 종료
    } // 범위 종료

    public static class EnemyEncounterProfileCatalog // 제품용 일반 5종·정예 3종 원형
    { // 범위 시작
        private static readonly List<EnemyEncounterProfile> NormalProfiles = new List<EnemyEncounterProfile> // 일반 전투 원형 목록
        { // 범위 시작
            new EnemyEncounterProfile("normal_frontline", StageType.Battle, 0, PieceRoleTag.Melee, PieceRoleTag.Melee, PieceRoleTag.Tanker, PieceRoleTag.Attacker), // 근접 전열형
            new EnemyEncounterProfile("normal_skirmish", StageType.Battle, 0, PieceRoleTag.Jumper, PieceRoleTag.Attacker, PieceRoleTag.Jumper, PieceRoleTag.Melee), // 기동 교란형
            new EnemyEncounterProfile("normal_ranged", StageType.Battle, 0, PieceRoleTag.Ranged, PieceRoleTag.Melee, PieceRoleTag.Ranged, PieceRoleTag.Tanker), // 원거리 엄호형
            new EnemyEncounterProfile("normal_guard", StageType.Battle, 0, PieceRoleTag.Tanker, PieceRoleTag.Support, PieceRoleTag.Attacker, PieceRoleTag.Ranged), // 방어 지원형
            new EnemyEncounterProfile("normal_mixed", StageType.Battle, 1, PieceRoleTag.Melee, PieceRoleTag.Ranged, PieceRoleTag.Jumper, PieceRoleTag.Support, PieceRoleTag.Attacker) // 혼합 증원형
        }; // 범위 종료
        private static readonly List<EnemyEncounterProfile> EliteProfiles = new List<EnemyEncounterProfile> // 정예 전투 원형 목록
        { // 범위 시작
            new EnemyEncounterProfile("elite_vanguard", StageType.Elite, 0, PieceRoleTag.Attacker, PieceRoleTag.Attacker, PieceRoleTag.Jumper, PieceRoleTag.Ranged, PieceRoleTag.Tanker), // 공격 선봉형
            new EnemyEncounterProfile("elite_fortress", StageType.Elite, 0, PieceRoleTag.Tanker, PieceRoleTag.Support, PieceRoleTag.Ranged, PieceRoleTag.Tanker, PieceRoleTag.Attacker), // 방어 요새형
            new EnemyEncounterProfile("elite_hunters", StageType.Elite, 0, PieceRoleTag.Ranged, PieceRoleTag.Jumper, PieceRoleTag.Attacker, PieceRoleTag.Ranged, PieceRoleTag.Support) // 추격 사냥형
        }; // 범위 종료
        private static readonly List<EnemyEncounterProfile> EmptyProfiles = new List<EnemyEncounterProfile>(); // 비대상 스테이지 빈 목록
        public static IReadOnlyList<EnemyEncounterProfile> GetProfiles(StageType stageType) // 종류별 편성 원형 조회
        { // 범위 시작
            if (stageType == StageType.Battle) // 일반 전투 확인
            { // 범위 시작
                return NormalProfiles; // 일반 5종 반환
            } // 범위 종료
            if (stageType == StageType.Elite) // 정예 전투 확인
            { // 범위 시작
                return EliteProfiles; // 정예 3종 반환
            } // 범위 종료
            return EmptyProfiles; // 보스·비전투 원형 없음
        } // 범위 종료
        public static EnemyEncounterProfile Select(StageType stageType, int seed) // 결정 Seed 기반 원형 선택
        { // 범위 시작
            IReadOnlyList<EnemyEncounterProfile> profiles = GetProfiles(stageType); // 종류별 후보 원형 조회
            if (profiles.Count == 0) // 지원하지 않는 종류 확인
            { // 범위 시작
                return null; // 원형 선택 없음
            } // 범위 종료
            int index = (seed & int.MaxValue) % profiles.Count; // 음수 없는 결정 인덱스
            return profiles[index]; // 선택한 원형 반환
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
