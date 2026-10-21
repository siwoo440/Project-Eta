using System; // 안정적인 ID 비교
using ProjectEta.Run; // 런·스테이지 규칙
using ProjectEta.Pieces; // 원형 상태 면역
using UnityEngine; // 보스 원형 로드

namespace ProjectEta.Boss // 보스 선택 영역
{ // 영역 시작
    public static class BossSelectionService // Seed 기반 페이즈 보스 선택
    { // 타입 시작
        public static BossSelectionSnapshot Select(RunState run, int phase, int depth) // 새 보스 규칙에 넣을 선택 결과
        { // 메서드 시작
            if (phase < 1 || phase > 5 || (depth != 5 && depth != 10)) return null; // 일반 노드·잘못된 페이즈 제외
            var pool = BossCatalog.GetPool(phase); // 해당 페이즈 실제 후보
            if (phase == 5) return Capture(BossCatalog.Find(depth == 5 ? "astragon" : "mordrain"), phase, depth, BossVariant.Base); // 마지막 페이즈 고정 보스
            if (pool.Count < 2) throw new InvalidOperationException("페이즈 보스 후보 부족: " + phase); // 반복 방지 불가능한 데이터 차단
            uint seed = StableSeed(run?.RouteMap?.MapSeed ?? 0, phase); // 전역 난수와 독립된 Seed
            int first = (int)(seed % (uint)pool.Count); // 깊이 5 후보 번호
            int second = (first + 1 + (int)((seed / (uint)pool.Count) % (uint)(pool.Count - 1))) % pool.Count; // 깊이 5와 다른 깊이 10 후보
            BossDefinition selected = pool[depth == 5 ? first : second]; // 조회 순서와 무관한 기본 선택
            if (run?.BalanceData?.stageRuleSnapshots != null) // 기존 상대 깊이 선택 존재 확인
            { // 조건 시작
                foreach (StageRuleSnapshot rules in run.BalanceData.stageRuleSnapshots) // 이미 저장된 스테이지 순회
                { // 반복 시작
                    BossSelectionSnapshot other = rules?.bossSelection; // 기존 보스 선택 조회
                    if (other == null || !other.IsValid || other.phase != phase || other.depth == depth || !string.Equals(other.bossId, selected.bossId, StringComparison.Ordinal)) continue; // 같은 페이즈 상대 보스 중복만 처리
                    selected = pool.Find(candidate => !string.Equals(candidate.bossId, other.bossId, StringComparison.Ordinal)); // 저장된 보스와 다른 후보 유지
                } // 반복 종료
            } // 조건 종료
            return Capture(selected, phase, depth, depth == 10 ? BossVariant.Enhanced : BossVariant.Base); // 선택 데이터 독립 복사
        } // 메서드 종료
        public static BossSelectionSnapshot CreateMasterKingPreview() // 후속 진엔딩 연결용 명시적 데이터
        { // 메서드 시작
            return Capture(BossCatalog.Find("master_king"), 0, 10, BossVariant.Base); // 일반 경로에서 자동 선택하지 않는 보스
        } // 메서드 종료
        private static BossSelectionSnapshot Capture(BossDefinition definition, int phase, int depth, BossVariant variant) // 선택 시점 수치 저장
        { // 메서드 시작
            if (definition == null) throw new InvalidOperationException("보스 정의 누락"); // 잘못된 풀 연결 차단
            PieceDefinition template = Resources.Load<PieceDefinition>(definition.templateResource); // 검증된 공통 전투 원형
            if (template == null) throw new InvalidOperationException("보스 원형 누락: " + definition.templateResource); // 보이지 않는 스폰 실패 차단
            bool enhanced = variant == BossVariant.Enhanced; // 강화형 수치 적용 여부
            return new BossSelectionSnapshot // 런에 저장할 독립 값
            { // 객체 시작
                bossId = definition.bossId, // 정체성 저장
                pieceId = "boss101_" + definition.bossId + "_" + (int)variant, // 복원 가능한 형태별 기물 ID
                displayName = definition.displayName + (enhanced ? " · 강화형" : string.Empty), // 실제 전투 이름
                phase = phase, // 페이즈 저장
                depth = depth, // 깊이 저장
                variant = variant, // 형태 저장
                maxHp = enhanced ? Mathf.CeilToInt(definition.baseHp * 1.25f) : definition.baseHp, // 임시 강화 체력
                attack = definition.baseAtk + (enhanced ? 1 : 0), // 임시 강화 공격력
                size = definition.size, // 점유 크기 저장
                templateResource = definition.templateResource, // 원형 경로 저장
                immuneStatusTags = (int)template.ImmuneStatusTags, // 면역 규칙 고정
                provisional = definition.provisional, // 임시값 표시 저장
                bespokePatternsImplemented = definition.bespokePatternsImplemented, // 전용 패턴 상태 저장
                plannedMovement = definition.plannedMovement, // 후속 이동 사양 복사
                plannedSpecial = definition.plannedSpecial, // 후속 특수 행동 복사
                plannedEnhanced = definition.plannedEnhanced // 후속 강화 사양 복사
            }; // 객체 종료
        } // 메서드 종료
        private static uint StableSeed(int mapSeed, int phase) // 프로세스별 해시 변화 없는 선택값
        { // 메서드 시작
            unchecked // 정수 오버플로를 포함한 고정 혼합
            { // 계산 시작
                uint value = (uint)mapSeed ^ ((uint)phase * 0x9E3779B9u); // 지도와 페이즈 혼합
                value = (value ^ (value >> 16)) * 0x85EBCA6Bu; // Seed 비트 확산
                return value ^ (value >> 13); // 음수 Seed도 안전한 무부호 결과
            } // 계산 종료
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
