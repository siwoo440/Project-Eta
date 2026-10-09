using System; // 런타임 타입 조회
using System.Collections.Generic; // 시험 기물 목록
using System.Reflection; // 신규 서비스 계약 검사
using NUnit.Framework; // 회귀 검증
using UnityEditor; // 시험 에셋 필드 설정
using UnityEngine; // JSON과 오브젝트 정리
using UnityEngine.UI; // 실제 보상 제목 조회
using ProjectEta.Battle; // 승리 결과
using ProjectEta.Cards; // 카드 보유 상한
using ProjectEta.Meta; // 영구 해금 상태
using ProjectEta.Pieces; // 기물 정의
using ProjectEta.Round; // 증원 데이터
using ProjectEta.Run; // 런 규칙과 저장
using ProjectEta.UI; // 실제 보상 화면

namespace ProjectEta.Tests.EditMode // 99일차 검사 영역
{ // 영역 시작
    public sealed class Day99StageContentTests // 규칙 안내와 저장 회귀 검사
    { // 클래스 시작
        private readonly List<UnityEngine.Object> _objects = new List<UnityEngine.Object>(); // 시험 자원 목록
        [TearDown] // 시험 종료 정리
        public void Cleanup() // 생성 자원 해제
        { // 메서드 시작
            foreach (UnityEngine.Object item in _objects) // 시험 자원 순회
            { // 반복 시작
                if (item != null) // 남은 자원 확인
                { // 조건 시작
                    UnityEngine.Object.DestroyImmediate(item); // 시험 자원 제거
                } // 조건 종료
            } // 반복 종료
            _objects.Clear(); // 정리 목록 초기화
        } // 메서드 종료

        [Test] // 정예 규칙 분리 검사
        public void Elite_UsesSeparateRoundWithShorterTurnLimit() // 별도 라운드 소비 확인
        { // 메서드 시작
            StageDefinition normal = StageDefinitionCatalog.Resolve("stage_2_battle", 2); // 일반 정의 조회
            StageDefinition elite = StageDefinitionCatalog.Resolve("stage_2_elite", 2); // 정예 정의 조회
            Assert.That(elite.RoundDefinition, Is.Not.SameAs(normal.RoundDefinition)); // 공유 데이터 분리 확인
            Assert.That(elite.RoundDefinition.TurnLimit, Is.LessThan(normal.RoundDefinition.TurnLimit)); // 정예 시간 압박 확인
        } // 메서드 종료

        [Test] // 증원 적용 시점 검사
        public void Elite_ReinforcementBecomesDueBeforeNormal() // 정예 조기 증원 확인
        { // 메서드 시작
            RoundDefinition normal = StageDefinitionCatalog.Resolve("stage_3_battle", 3).RoundDefinition; // 일반 라운드
            RoundDefinition elite = StageDefinitionCatalog.Resolve("stage_3_elite", 3).RoundDefinition; // 정예 라운드
            Assert.That(normal.Reinforcements[0].IsDue(2), Is.False); // 일반 증원 대기 확인
            Assert.That(elite.Reinforcements[0].IsDue(2), Is.True); // 정예 증원 도달 확인
            Assert.That(elite.Reinforcements[1].IsDue(4), Is.True); // 두 번째 조기 증원 확인
        } // 메서드 종료

        [Test] // 보상 표시 정책 검사
        public void RewardTitle_DescribesOneStarAcquisitionInsteadOfHighGrade() // 실제 획득 안내 확인
        { // 메서드 시작
            GameObject host = Track(new GameObject("Day99Reward")); // 보상 호스트 생성
            CardRewardUI ui = host.AddComponent<CardRewardUI>(); // 실제 화면 생성
            ui.Show(Array.Empty<PieceDefinition>(), CardRewardSource.EliteVictory, new CardRewardProfile(CardRewardQuality.Advanced, CardRewardSource.EliteVictory, 2, 55, 38, 7), card => Assert.Fail("검사 중 선택 실행 금지")); // 정예 보상 표시
            Text title = host.GetComponentInChildren<Canvas>().transform.Find("CardRewardRoot/RewardHeader/RewardTitle").GetComponent<Text>(); // 실제 제목 조회
            StringAssert.Contains("1성", title.text); // 실제 획득 등급 안내 확인
            StringAssert.DoesNotContain("고급", title.text); // 직접 고등급 획득 오해 차단
            Canvas.ForceUpdateCanvases(); // 실제 제목 배치 갱신
            Assert.That(title.preferredHeight, Is.LessThanOrEqualTo(title.rectTransform.rect.height)); // 보상 제목 잘림 방지 확인
        } // 메서드 종료

        [Test] // 저장된 규칙 고정 검사
        public void Rules_SurviveSaveAndIgnoreLaterAssetAndGoldChanges() // 이어하기 동일 규칙 확인
        { // 메서드 시작
            RunState run = new RunState(3); // 시험 런 생성
            StageDefinition definition = CreateEliteDefinition(); // 독립 정예 정의
            RunBalanceProfile balance = Track(ScriptableObject.CreateInstance<RunBalanceProfile>()); // 독립 경제 설정
            object before = GetRules(run, definition, 1, balance); // 최초 규칙 저장
            SerializedObject round = new SerializedObject(definition.RoundDefinition); // 시험 라운드 편집
            round.FindProperty("_turnLimit").intValue = 1; // 이후 설정 변경 재현
            round.ApplyModifiedPropertiesWithoutUndo(); // 시험 변경 적용
            balance.eliteGold = 99; // 이후 보상 변경 재현
            RunState restored = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), null); // 실제 런 저장 복원
            object after = GetRules(restored, definition, 1, balance); // 저장된 규칙 재조회
            Assert.That(Read<int>(after, "turnLimit"), Is.EqualTo(25)); // 최초 제한 유지 확인
            Assert.That(Read<int>(after, "victoryGold"), Is.EqualTo(25)); // 최초 보상 유지 확인
            Assert.That(Read<int>(before, "turnLimit"), Is.EqualTo(25)); // 원본 스냅샷 독립성 확인
        } // 메서드 종료

        [Test] // 페이즈별 규칙 키 검사
        public void Rules_KeepSeparatePhaseRewards() // 진행도 보상 분리 확인
        { // 메서드 시작
            RunState run = new RunState(3); // 시험 런 생성
            StageDefinition definition = CreateEliteDefinition(); // 정예 정의 생성
            RunBalanceProfile balance = Track(ScriptableObject.CreateInstance<RunBalanceProfile>()); // 기본 경제 설정
            Assert.That(Read<int>(GetRules(run, definition, 1, balance), "victoryGold"), Is.EqualTo(25)); // 첫 페이즈 보상
            Assert.That(Read<int>(GetRules(run, definition, 2, balance), "victoryGold"), Is.EqualTo(30)); // 둘째 페이즈 보상
        } // 메서드 종료

        [Test] // 지도 잠금 설명 검사
        public void Preview_DistinguishesReachableVisitedAndDisconnectedNodes() // 실제 경로 선택 안내 확인
        { // 메서드 시작
            RunState run = new RunState(3); // 시험 런
            StageNode current = new StageNode("current", Vector2Int.zero, 1, "stage_1_battle"); // 현재 방문 노드
            StageNode next = new StageNode("next", Vector2Int.one, 2, "stage_2_elite"); // 연결 노드
            StageNode locked = new StageNode("locked", new Vector2Int(4, 4), 4, "stage_4_elite"); // 연결 없는 노드
            current.SetNextNodeIds(new[] { "next" }); // 다음 경로 연결
            run.RouteMap.Configure(1, current, new[] { next, locked }); // 실제 지도 구성
            string available = BuildPreview(run, next); // 선택 가능한 안내
            StringAssert.Contains("선택 가능", available); // 선택 상태 확인
            StringAssert.Contains("25 Gold", available); // 정예 보상 안내 확인
            StringAssert.Contains("25턴", available); // 정예 제한 안내 확인
            StringAssert.Contains("1성", available); // 실제 카드 정책 확인
            StringAssert.Contains("방문 완료", BuildPreview(run, current)); // 방문 상태 확인
            StringAssert.Contains("접근 불가", BuildPreview(run, locked)); // 경로 잠금 확인
        } // 메서드 종료

        [Test] // 런 고정 해금 사유 검사
        public void Eligibility_UsesFrozenUnlockRatherThanLaterMetaChange() // 현재 런 해금 고정 확인
        { // 메서드 시작
            PieceDefinition piece = CreatePiece("locked", "unlock_test"); // 해금 요구 기물
            MetaProgressState progress = new MetaProgressState(); // 미해금 상태
            RunContentUnlockSnapshot frozen = RunContentUnlockSnapshot.Capture("test", progress); // 현재 런 고정 해금
            progress.Unlock(MetaUnlockType.Piece, "unlock_test"); // 런 도중 영구 해금
            string reason = GetReason(piece, Array.Empty<PieceDefinition>(), Array.Empty<PieceDefinition>(), frozen); // 현재 런 제외 사유
            StringAssert.Contains("미해금", reason); // 현재 런 잠금 유지
            RunContentUnlockSnapshot next = RunContentUnlockSnapshot.Capture("next", progress); // 다음 런 해금
            Assert.That(GetReason(piece, Array.Empty<PieceDefinition>(), Array.Empty<PieceDefinition>(), next), Is.Empty); // 다음 런 획득 허용
        } // 메서드 종료

        [Test] // 사망 카드 보유 상한 검사
        public void Eligibility_CountsDeadCopiesInExclusionReason() // 사망 포함 상한 확인
        { // 메서드 시작
            PieceDefinition piece = CreatePiece("limited", string.Empty); // 기본 기물 생성
            List<PieceDefinition> dead = new List<PieceDefinition>(); // 사망 보유 목록
            for (int index = 0; index < CardOwnershipRules.GetOwnedLimit(PieceGrade.OneStar); index++) // 실제 상한만큼 구성
            { // 반복 시작
                dead.Add(piece); // 사망 카드 등록
            } // 반복 종료
            StringAssert.Contains("보유 상한", GetReason(piece, Array.Empty<PieceDefinition>(), dead, null)); // 획득 제외 사유 확인
        } // 메서드 종료

        [Test] // 실제 Gold 지급 검사
        public void Victory_PaysSavedPreviewGoldOnlyOnceAfterRestore() // 안내 금액과 중복 지급 차단 확인
        { // 메서드 시작
            RunState run = new RunState(3); // 독립 런 생성
            StageNode current = new StageNode("phase_1_elite", Vector2Int.zero, 2, "stage_2_elite"); // 완료 정예 노드
            run.CurrentRound = 2; // 현재 깊이 설정
            run.RouteMap.Configure(2, current, Array.Empty<StageNode>()); // 실제 완료 위치 연결
            StageDefinition definition = StageDefinitionCatalog.Resolve(current.StageDefinitionId, 2); // 실제 정의 조회
            var balance = Track(ScriptableObject.CreateInstance<RunBalanceProfile>()); // 독립 보상 설정
            balance.eliteGold = 47; // 저장 규칙과 현재 설정 차이 재현
            RunStageRuleService.GetOrCreate(run, definition, 1, balance); // 안내된 규칙 저장
            run.RecordBattleOutcome(BattleOutcome.Victory); // 지급 가능한 실제 승리 상태
            RunState restored = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), null); // 런 저장 복원
            int before = RunEconomyService.GetOrCreate(restored).Currency; // 지급 전 재화 조회
            Assert.That(RunBattleGoldRewardService.TryGrant(restored), Is.True); // 실제 지급 경로 실행
            Assert.That(RunEconomyService.GetOrCreate(restored).Currency - before, Is.EqualTo(47)); // 저장 안내 금액 지급 확인
            Assert.That(RunBattleGoldRewardService.TryGrant(restored), Is.False); // 동일 전투 재지급 차단 확인
            RunState savedAgain = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(restored.ToSaveData())), null); // 지급 후 저장 복원
            Assert.That(RunBattleGoldRewardService.TryGrant(savedAgain), Is.False); // 이어하기 중복 지급 차단 확인
        } // 메서드 종료

        [Test] // 구버전 저장 호환 검사
        public void LegacyBalance_MissingRulesCreatesCurrentDefaultsWithoutDroppingClaims() // 누락 목록 복원 확인
        { // 메서드 시작
            RunState run = new RunState(3); // 독립 런 생성
            RunSaveData data = run.ToSaveData(); // 실제 저장 데이터 생성
            data.balanceData.stageRuleSnapshots = null; // 구버전 규칙 목록 누락 재현
            data.balanceData.goldRewardClaims.Add("1:1"); // 기존 지급 이력 보관
            RunState restored = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(data)), null); // 실제 저장 복원
            StageRuleSnapshot rules = RunStageRuleService.GetOrCreate(restored, StageDefinitionCatalog.Resolve("stage_2_elite", 2), 1); // 최초 조회 규칙 생성
            Assert.That(rules.turnLimit, Is.EqualTo(25)); // 현행 기본 규칙 적용 확인
            Assert.That(restored.BalanceData.goldRewardClaims, Does.Contain("1:1")); // 기존 지급 이력 보존 확인
        } // 메서드 종료

        private object GetRules(RunState run, StageDefinition definition, int phase, RunBalanceProfile balance) // 실제 규칙 서비스 호출
        { // 메서드 시작
            Type type = FindService("RunStageRuleService"); // 신규 계약 타입
            return type.GetMethod("GetOrCreate").Invoke(null, new object[] { run, definition, phase, balance }); // 실제 규칙 획득
        } // 메서드 종료
        private string BuildPreview(RunState run, StageNode node) // 실제 지도 설명 호출
        { // 메서드 시작
            return (string)FindService("StagePreviewFormatter").GetMethod("Build").Invoke(null, new object[] { run, node }); // 실제 안내 반환
        } // 메서드 종료
        private string GetReason(PieceDefinition piece, IReadOnlyList<PieceDefinition> owned, IReadOnlyList<PieceDefinition> dead, RunContentUnlockSnapshot snapshot) // 실제 제외 사유 호출
        { // 메서드 시작
            return (string)FindService("RunContentEligibility").GetMethod("GetExclusionReason").Invoke(null, new object[] { piece, owned, dead, snapshot }); // 실제 정책 결과
        } // 메서드 종료
        private static Type FindService(string name) // 새 API 미구현 검출
        { // 메서드 시작
            Type type = typeof(StageDefinitionCatalog).Assembly.GetType("ProjectEta.Run." + name); // 런타임 계약 조회
            Assert.That(type, Is.Not.Null, name + " 미구현"); // 실제 서비스 존재 확인
            return type; // 계약 반환
        } // 메서드 종료
        private static T Read<T>(object value, string field) // 실제 스냅샷 값 조회
        { // 메서드 시작
            return (T)value.GetType().GetField(field).GetValue(value); // 공개 저장 값 반환
        } // 메서드 종료
        private StageDefinition CreateEliteDefinition() // 원본 변경 없는 정예 시험 정의
        { // 메서드 시작
            RoundDefinition round = Track(UnityEngine.Object.Instantiate(StageDefinitionCatalog.Resolve("stage_2_elite", 2).RoundDefinition)); // 독립 라운드 복제
            StageDefinition definition = Track(ScriptableObject.CreateInstance<StageDefinition>()); // 독립 정의 생성
            definition.ConfigureRuntime("stage_2_elite", "시험 정예", StageType.Elite, round, "PrototypeEliteReward"); // 정예 데이터 연결
            return definition; // 시험 정의 반환
        } // 메서드 종료
        private PieceDefinition CreatePiece(string id, string unlock) // 시험 기물 생성
        { // 메서드 시작
            PieceDefinition piece = Track(ScriptableObject.CreateInstance<PieceDefinition>()); // 독립 기물 생성
            SerializedObject data = new SerializedObject(piece); // 시험 데이터 편집
            data.FindProperty("_pieceId").stringValue = id; // 기물 ID 설정
            data.FindProperty("_displayName").stringValue = id; // 표시 이름 설정
            data.FindProperty("_requiredMetaUnlockId").stringValue = unlock; // 해금 요구 설정
            data.FindProperty("_grade").intValue = (int)PieceGrade.OneStar; // 획득 등급 설정
            data.FindProperty("_movementType").intValue = (int)PieceMovementType.Pawn; // 일반 이동 설정
            data.ApplyModifiedPropertiesWithoutUndo(); // 시험 데이터 적용
            return piece; // 시험 기물 반환
        } // 메서드 종료
        private T Track<T>(T item) where T : UnityEngine.Object // 시험 자원 등록
        { // 메서드 시작
            _objects.Add(item); // 정리 목록 추가
            return item; // 생성 자원 반환
        } // 메서드 종료
    } // 클래스 종료
} // 영역 종료
