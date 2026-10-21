using System; // 보스 ID 비교
using System.Collections.Generic; // 독립 정의 재사용
using ProjectEta.Run; // 저장 규칙 조회
using ProjectEta.Pieces; // 실제 전투 기물
using UnityEngine; // 원형 복제·JSON 키

namespace ProjectEta.Boss // 보스 전투 정의 영역
{ // 영역 시작
    public static class BossRuntimeDefinitionFactory // 저장된 수치로 원본 에셋과 독립된 기물 생성
    { // 타입 시작
        private static readonly Dictionary<string, PieceDefinition> Cache = new Dictionary<string, PieceDefinition>(); // 같은 저장 규칙의 중복 복제 방지
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 수명 초기화
        private static void Initialize() // 이전 플레이 정의 정리와 종료 연결
        { // 메서드 시작
            Clear(); // 이전 런타임 복제 정리
            Application.quitting -= Clear; // 중복 종료 구독 해제
            Application.quitting += Clear; // 플레이어 종료 시 복제 정리
        } // 메서드 종료
        public static PieceDefinition Resolve(BossSelectionSnapshot snapshot) // 저장 규칙의 실제 기물 조회
        { // 메서드 시작
            if (snapshot == null || !snapshot.IsValid) return null; // 잘못된 저장 데이터 제외
            string key = JsonUtility.ToJson(snapshot); // 선택 수치 전체를 포함한 재사용 키
            if (Cache.TryGetValue(key, out PieceDefinition cached) && cached != null) return cached; // 동일 규칙 복제 재사용
            PieceDefinition template = Resources.Load<PieceDefinition>(snapshot.templateResource); // 기존 전투 원형 조회
            if (template == null) throw new InvalidOperationException("저장 보스 원형 누락: " + snapshot.templateResource); // 복원 누락 명시
            PieceDefinition definition = UnityEngine.Object.Instantiate(template); // 리소스 에셋 보호
            definition.hideFlags = HideFlags.HideAndDontSave; // 프로젝트 저장 제외
            definition.ConfigureBossRuntime(snapshot.pieceId, snapshot.displayName, snapshot.maxHp, snapshot.attack, snapshot.size, (StatusEffectType)snapshot.immuneStatusTags); // 선택 당시 실제 전투 수치 적용
            Cache[key] = definition; // 관리 대상 복제 보관
            return definition; // 전투·저장 복원 공통 정의
        } // 메서드 종료
        public static PieceDefinition ResolveSavedPiece(RunState run, string pieceId) // 전투 중 저장된 보스 정의 복원
        { // 메서드 시작
            if (run?.BalanceData?.stageRuleSnapshots == null || string.IsNullOrWhiteSpace(pieceId)) return null; // 구버전·빈 ID 제외
            foreach (StageRuleSnapshot rules in run.BalanceData.stageRuleSnapshots) // 저장된 전투 규칙 순회
            { // 반복 시작
                BossSelectionSnapshot snapshot = rules?.bossSelection; // 보스 선택 데이터 조회
                if (snapshot != null && snapshot.IsValid && string.Equals(snapshot.pieceId, pieceId, StringComparison.Ordinal)) return Resolve(snapshot); // 같은 기물 ID의 고정 수치 복원
            } // 반복 종료
            return null; // 일반 기물·구버전 보스는 기존 경로 사용
        } // 메서드 종료
        public static void Clear() // 런타임 정의 수명 정리
        { // 메서드 시작
            foreach (PieceDefinition definition in Cache.Values) // 생성한 복제만 순회
            { // 반복 시작
                if (definition == null) continue; // 이미 제거된 복제 제외
                if (Application.isPlaying) UnityEngine.Object.Destroy(definition); // 플레이 중 안전한 제거
                else UnityEngine.Object.DestroyImmediate(definition); // 에디터 검사 즉시 정리
            } // 반복 종료
            Cache.Clear(); // 정적 참조 해제
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
