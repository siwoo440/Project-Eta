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
        public int MinimumPhase // 원형이 처음 등장하는 페이즈
        { // 속성 범위 시작
            get; // 최소 페이즈 조회
        } // 속성 범위 종료
        public PieceRoleTag[] PreferredRoles // 순서별 우선 역할의 안전 복사본
        { // 속성 범위 시작
            get // 역할 배열 조회
            { // 범위 시작
                return (PieceRoleTag[])_preferredRoles.Clone(); // 외부 변경 없는 복사본 반환
            } // 범위 종료
        } // 속성 범위 종료
        public EnemyEncounterProfile(string profileId, StageType stageType, int enemyCountBonus, params PieceRoleTag[] preferredRoles) // 편성 원형 생성
            : this(profileId, stageType, enemyCountBonus, 1, preferredRoles) // 기존 호출의 1페이즈 호환
        { // 범위 시작
        } // 범위 종료
        public EnemyEncounterProfile(string profileId, StageType stageType, int enemyCountBonus, int minimumPhase, params PieceRoleTag[] preferredRoles) // 해금 페이즈 포함 원형 생성
        { // 범위 시작
            ProfileId = profileId ?? string.Empty; // null 없는 ID 저장
            StageType = stageType; // 전투 종류 저장
            EnemyCountBonus = enemyCountBonus < 0 ? 0 : enemyCountBonus; // 음수 추가 적 차단
            MinimumPhase = minimumPhase < 1 ? 1 : minimumPhase; // 잘못된 최소 페이즈 보정
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
            new EnemyEncounterProfile("normal_frontline", StageType.Battle, 0, 1, PieceRoleTag.Melee, PieceRoleTag.Melee, PieceRoleTag.Tanker, PieceRoleTag.Attacker), // 1페이즈 근접 전열형
            new EnemyEncounterProfile("normal_skirmish", StageType.Battle, 0, 1, PieceRoleTag.Jumper, PieceRoleTag.Attacker, PieceRoleTag.Jumper, PieceRoleTag.Melee), // 1페이즈 기동 교란형
            new EnemyEncounterProfile("normal_ranged", StageType.Battle, 0, 2, PieceRoleTag.Ranged, PieceRoleTag.Melee, PieceRoleTag.Ranged, PieceRoleTag.Tanker), // 2페이즈 원거리 엄호형
            new EnemyEncounterProfile("normal_guard", StageType.Battle, 0, 2, PieceRoleTag.Tanker, PieceRoleTag.Support, PieceRoleTag.Attacker, PieceRoleTag.Ranged), // 2페이즈 방어 지원형
            new EnemyEncounterProfile("normal_mixed", StageType.Battle, 1, 3, PieceRoleTag.Melee, PieceRoleTag.Ranged, PieceRoleTag.Jumper, PieceRoleTag.Support, PieceRoleTag.Attacker) // 3페이즈 혼합 증원형
        }; // 범위 종료
        private static readonly List<EnemyEncounterProfile> EliteProfiles = new List<EnemyEncounterProfile> // 정예 전투 원형 목록
        { // 범위 시작
            new EnemyEncounterProfile("elite_vanguard", StageType.Elite, 0, 1, PieceRoleTag.Attacker, PieceRoleTag.Attacker, PieceRoleTag.Jumper, PieceRoleTag.Ranged, PieceRoleTag.Tanker), // 1페이즈 공격 선봉형
            new EnemyEncounterProfile("elite_hunters", StageType.Elite, 0, 2, PieceRoleTag.Ranged, PieceRoleTag.Jumper, PieceRoleTag.Attacker, PieceRoleTag.Ranged, PieceRoleTag.Support), // 2페이즈 추격 사냥형
            new EnemyEncounterProfile("elite_fortress", StageType.Elite, 0, 3, PieceRoleTag.Tanker, PieceRoleTag.Support, PieceRoleTag.Ranged, PieceRoleTag.Tanker, PieceRoleTag.Attacker) // 3페이즈 방어 요새형
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
            return Select(stageType, seed, 5, string.Empty, string.Empty); // 기존 호출의 전체 원형 호환
        } // 범위 종료
        public static IReadOnlyList<EnemyEncounterProfile> GetAvailableProfiles(StageType stageType, int phase) // 현재 페이즈 해금 원형 조회
        { // 범위 시작
            IReadOnlyList<EnemyEncounterProfile> profiles = GetProfiles(stageType); // 종류별 전체 원형 조회
            var available = new List<EnemyEncounterProfile>(); // 해금 원형 결과 생성
            int safePhase = phase < 1 ? 1 : phase; // 음수 페이즈 보정
            for (int index = 0; index < profiles.Count; index++) // 전체 원형 순회
            { // 범위 시작
                EnemyEncounterProfile profile = profiles[index]; // 현재 원형 조회
                if (profile != null && profile.MinimumPhase <= safePhase) // 현재 페이즈 해금 여부 확인
                { // 범위 시작
                    available.Add(profile); // 해금 원형 추가
                } // 범위 종료
            } // 범위 종료
            return available; // 해금 원형 반환
        } // 범위 종료
        public static EnemyEncounterProfile Select(StageType stageType, int seed, int phase, string excludedProfileId, string forcedProfileId) // 해금·반복·개발 설정 포함 선택
        { // 범위 시작
            IReadOnlyList<EnemyEncounterProfile> unlocked = GetAvailableProfiles(stageType, phase); // 현재 해금 원형 조회
            for (int index = 0; index < unlocked.Count; index++) // 강제 원형 후보 순회
            { // 범위 시작
                EnemyEncounterProfile profile = unlocked[index]; // 현재 해금 원형 조회
                if (!string.IsNullOrWhiteSpace(forcedProfileId) && profile.ProfileId == forcedProfileId) // 유효한 강제 원형 확인
                { // 범위 시작
                    return profile; // 해금된 강제 원형 반환
                } // 범위 종료
            } // 범위 종료
            var profiles = new List<EnemyEncounterProfile>(); // 반복 제외 후보 생성
            for (int index = 0; index < unlocked.Count; index++) // 해금 원형 순회
            { // 범위 시작
                EnemyEncounterProfile profile = unlocked[index]; // 현재 원형 조회
                if (unlocked.Count > 1 && profile.ProfileId == excludedProfileId) continue; // 후보가 여러 개면 직전 원형 제외
                profiles.Add(profile); // 선택 후보 추가
            } // 범위 종료
            if (profiles.Count == 0) // 지원하지 않는 종류 확인
            { // 범위 시작
                return null; // 원형 선택 없음
            } // 범위 종료
            int selectedIndex = MixSeed(seed) % profiles.Count; // 혼합 Seed 기반 결정 인덱스
            return profiles[selectedIndex]; // 선택한 원형 반환
        } // 범위 종료
        private static int MixSeed(int seed) // 순차 Seed의 원형 선택 편향 완화
        { // 범위 시작
            unchecked // 정수 오버플로 허용
            { // 범위 시작
                uint value = (uint)seed; // 부호 없는 Seed 변환
                value ^= value >> 16; // 상하위 비트 혼합
                value *= 0x7FEB352Du; // 첫 분산 곱셈
                value ^= value >> 15; // 중간 비트 혼합
                value *= 0x846CA68Bu; // 둘째 분산 곱셈
                value ^= value >> 16; // 최종 비트 혼합
                return (int)(value & int.MaxValue); // 음수 없는 결정값 반환
            } // 범위 종료
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
