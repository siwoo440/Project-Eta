using System.Reflection; // private static 메서드를 리플렉션으로 호출하기 위한 네임스페이스
using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEditor; // SerializedObject로 private 직렬화 필드를 테스트용으로 설정하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceRoleTag, StatusEffectType 등을 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스
using ProjectEta.UI; // PieceInfoPanelUI 리플렉션 테스트를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day31PieceInfoPanelTests // 31일차 기물 정보 패널의 표시 문구 로직과 선택 이벤트·하이라이트 수정을 검증하는 테스트 모음
    {
        [Test] // 역할 태그가 없으면 대시로, 여러 개면 가운뎃점으로 이어 표시되는지 검증
        public void BuildRoleTagsLabel_FormatsTagsCorrectly()
        {
            Assert.AreEqual("-", InvokeBuildRoleTagsLabel(PieceRoleTag.None)); // 역할 없음은 대시
            Assert.AreEqual("근접", InvokeBuildRoleTagsLabel(PieceRoleTag.Melee)); // 단일 태그
            Assert.AreEqual("근접 · 도약", InvokeBuildRoleTagsLabel(PieceRoleTag.Melee | PieceRoleTag.Jumper)); // 복수 태그는 가운뎃점으로 연결
        }

        [Test] // 상태 이상이 없으면 "없음", 있으면 중첩·지속 턴 정보가 함께 표시되는지 검증
        public void BuildStatusEffectsLabel_FormatsStatusEffectsCorrectly()
        {
            var piece = new PieceRuntimeState(CreatePieceDefinition("test_target"), Vector2Int.zero, true); // 테스트용 기물
            Assert.AreEqual("없음", InvokeBuildStatusEffectsLabel(piece)); // 상태가 없을 때 기본 문구

            var poison = CreateStatusDefinition(StatusEffectType.Poison, StatusStackMode.StacksAdd, maxStacks: 3, durationTurns: 3); // 중첩형 독
            piece.ApplyStatus(poison); // 1중첩
            piece.ApplyStatus(poison); // 2중첩
            Assert.AreEqual("독 2중첩(3턴)", InvokeBuildStatusEffectsLabel(piece)); // 중첩 수와 지속 턴이 함께 표시

            var stun = CreateStatusDefinition(StatusEffectType.Stun, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 1); // 갱신형 기절
            piece.ApplyStatus(stun); // 기절 추가 적용
            Assert.AreEqual("독 2중첩(3턴), 기절(1턴)", InvokeBuildStatusEffectsLabel(piece)); // 여러 상태는 쉼표로 연결, 1중첩은 중첩 수를 생략
        }

        [Test] // Artwork가 없을 때 표시할 약칭이 CardView와 동일한 규칙(PieceId 앞 3글자, 소문자)으로 만들어지는지 검증
        public void GetPortraitPlaceholder_UsesFirstThreeLettersOfPieceId()
        {
            Assert.AreEqual("roo", InvokeGetPortraitPlaceholder(CreatePieceDefinitionWithId("rook"))); // rook -> roo
            Assert.AreEqual("kin", InvokeGetPortraitPlaceholder(CreatePieceDefinitionWithId("king"))); // king -> kin
        }

        [Test] // 기물을 선택·해제할 때 SelectionChanged 이벤트가 올바른 인자로 발행되는지 검증
        public void SelectingAndDeselectingPiece_RaisesSelectionChangedWithCorrectArgument()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var origin = new Vector2Int(4, 1); // 선택할 기물의 좌표
                var piece = new PieceRuntimeState(CreatePieceDefinition("test_target"), origin, isPlayerPiece: true); // 테스트용 아군 기물
                context.RunState.Board.GetTile(origin).OccupyingPiece = piece; // 보드에 배치

                PieceRuntimeState observed = piece; // 초기값을 다르게 둬서 콜백이 실제로 호출됐는지 구분
                bool wasNullOnce = false; // 해제 시 null 통지가 왔는지 확인용 플래그
                context.Input.SelectionChanged += p => // 선택 변경 이벤트 구독
                {
                    observed = p; // 마지막으로 통지된 값 기록
                    if (p == null) wasNullOnce = true; // null 통지가 한 번이라도 왔는지 기록
                };

                context.Input.TrySelectPieceAt(origin); // 기물 선택
                Assert.AreSame(piece, observed, "선택 시 선택된 기물이 그대로 전달되어야 합니다."); // 선택 통지 확인

                context.Input.TrySelectPieceAt(origin); // 같은 기물을 다시 클릭해 선택 해제
                Assert.IsTrue(wasNullOnce, "선택 해제 시 null이 통지되어야 합니다."); // 해제 통지 확인
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 기절한 기물을 선택하면 이동 후보가 비어 있어 실제 이동 시도가 거부되는지 검증(31일차 하이라이트 버그 수정 회귀)
        public void SelectingStunnedPiece_ProducesNoMoveCandidates()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var origin = new Vector2Int(4, 4); // 기물 좌표(King형 이동 기본 정의 사용)
                var piece = new PieceRuntimeState(CreatePieceDefinition("test_target"), origin, isPlayerPiece: true); // 테스트용 아군 기물
                context.RunState.Board.GetTile(origin).OccupyingPiece = piece; // 보드에 배치

                var stun = CreateStatusDefinition(StatusEffectType.Stun, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 1); // 기절 정의
                piece.ApplyStatus(stun); // 기절 적용(CanMove/CanAttack이 false로 갱신됨)

                context.Input.TrySelectPieceAt(origin); // 기절한 기물 선택
                bool moved = context.Input.TryMoveSelectedPieceTo(new Vector2Int(4, 5)); // 인접한 빈 칸으로 이동 시도

                Assert.IsFalse(moved, "31일차 수정 전에는 기절 중에도 이동 후보가 그대로 나와 이동이 성공했을 것입니다."); // 수정 후에는 후보 자체가 없어 이동 거부
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        private static (GameObject Root, BoardInputController Input, RunState RunState, TurnManager TurnManager) CreateBoundContext() // AttackExecutionTests와 동일한 패턴의 초기화 도우미
        {
            var root = new GameObject("Day31InfoPanelTestRoot"); // 테스트용 오브젝트 생성
            var boardView = root.AddComponent<BoardView>(); // 보드 뷰 추가
            var boardInput = root.AddComponent<BoardInputController>(); // 입력 컨트롤러 추가
            var runState = new RunState(3); // 실제 전투와 같은 방식의 런 상태 생성
            var turnManager = new TurnManager(); // 실제 전투와 같은 방식의 턴 매니저 생성

            boardView.Bind(runState.Board); // 보드 뷰에 실제 보드 연결
            boardInput.Bind(runState, boardView, turnManager); // 입력에 실제 런 상태와 턴 매니저 연결

            turnManager.MarkInitialKingPlaced(); // 시작 배치는 킹을 놓아야만 끝나므로 필수 조건을 먼저 충족
            turnManager.TryEndDeploymentTurn(); // 일반 턴의 이동·공격을 검증하므로 시작 배치 턴을 명시적으로 종료해 PlayerTurn에서 시작

            return (root, boardInput, runState, turnManager); // 테스트에서 바로 쓸 수 있도록 묶어서 반환
        }

        private static PieceDefinition CreatePieceDefinition(string pieceId) // 테스트 전용 기물 정의 생성 도우미(King형 기본 이동)
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 인스턴스만 생성(에셋으로 저장하지 않음)
            var serialized = new SerializedObject(definition); // private 직렬화 필드에 접근하기 위한 SerializedObject
            serialized.FindProperty("_pieceId").stringValue = pieceId; // 식별자 설정
            serialized.FindProperty("_displayName").stringValue = pieceId; // 표시 이름 설정
            serialized.FindProperty("_baseHp").intValue = 5; // 테스트용 임시 체력
            serialized.FindProperty("_baseAtk").intValue = 1; // 테스트용 임시 공격력
            serialized.FindProperty("_occupancySize").vector2IntValue = Vector2Int.one; // 1칸 점유
            serialized.FindProperty("_description").stringValue = "31일차 테스트용 임시 기물 정의."; // 설명 설정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Undo 기록 없이 즉시 반영
            return definition; // 완성된 테스트용 정의 반환
        }

        private static PieceDefinition CreatePieceDefinitionWithId(string pieceId) // GetPortraitPlaceholder 검증 전용으로 PieceId만 채운 최소 정의 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 인스턴스만 생성
            var field = typeof(PieceDefinition).GetField("_pieceId", BindingFlags.NonPublic | BindingFlags.Instance); // private _pieceId 필드 탐색
            field.SetValue(definition, pieceId); // PieceId 값 주입
            return definition; // 완성된 정의 반환
        }

        private static StatusEffectDefinition CreateStatusDefinition(StatusEffectType statusType, StatusStackMode stackMode, int maxStacks, int durationTurns) // 테스트 전용 상태 이상 정의 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<StatusEffectDefinition>(); // 인스턴스만 생성(에셋으로 저장하지 않음)
            var serialized = new SerializedObject(definition); // private 직렬화 필드에 접근하기 위한 SerializedObject
            serialized.FindProperty("_statusType").intValue = (int)statusType; // 상태 종류 설정
            serialized.FindProperty("_displayName").stringValue = statusType.ToString(); // 표시 이름 설정
            serialized.FindProperty("_stackMode").enumValueIndex = (int)stackMode; // 중첩 방식 설정
            serialized.FindProperty("_maxStacks").intValue = maxStacks; // 최대 중첩 수 설정
            serialized.FindProperty("_defaultDurationTurns").intValue = durationTurns; // 기본 지속 턴 설정
            serialized.FindProperty("_description").stringValue = "31일차 테스트용 임시 상태 이상 정의."; // 설명 설정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Undo 기록 없이 즉시 반영
            return definition; // 완성된 테스트용 정의 반환
        }

        private static string InvokeBuildRoleTagsLabel(PieceRoleTag tags) // private static BuildRoleTagsLabel을 리플렉션으로 호출하는 도우미
        {
            var method = typeof(PieceInfoPanelUI).GetMethod("BuildRoleTagsLabel", BindingFlags.NonPublic | BindingFlags.Static); // private 정적 메서드 탐색
            return (string)method.Invoke(null, new object[] { tags }); // 결과 반환
        }

        private static string InvokeBuildStatusEffectsLabel(PieceRuntimeState piece) // private static BuildStatusEffectsLabel을 리플렉션으로 호출하는 도우미
        {
            var method = typeof(PieceInfoPanelUI).GetMethod("BuildStatusEffectsLabel", BindingFlags.NonPublic | BindingFlags.Static); // private 정적 메서드 탐색
            return (string)method.Invoke(null, new object[] { piece }); // 결과 반환
        }

        private static string InvokeGetPortraitPlaceholder(PieceDefinition definition) // private static GetPortraitPlaceholder를 리플렉션으로 호출하는 도우미
        {
            var method = typeof(PieceInfoPanelUI).GetMethod("GetPortraitPlaceholder", BindingFlags.NonPublic | BindingFlags.Static); // private 정적 메서드 탐색
            return (string)method.Invoke(null, new object[] { definition }); // 결과 반환
        }
    }
}
