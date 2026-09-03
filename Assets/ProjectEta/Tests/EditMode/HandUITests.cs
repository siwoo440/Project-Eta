using System.Reflection; // 테스트에서 private 직렬화 필드에 값을 주입하기 위한 네임스페이스
using NUnit.Framework; // EditMode 테스트와 Assert 기능을 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Object, Vector2Int를 사용하기 위한 네임스페이스
using UnityEngine.EventSystems; // CardView의 좌클릭 드래그 이벤트를 직접 검증하기 위한 네임스페이스
using UnityEngine.UI; // CanvasGroup 상태를 검증하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager와 TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView와 BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // HandState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceMovementType을 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스
using ProjectEta.UI; // 18일차 HandUI와 CardView를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class HandUITests // 카드 이미지 손패와 UI 드롭 소환 흐름을 검증하는 테스트 모음
    {
        [Test] // 실제 HandState 카드 수만큼 카드 UI가 만들어지는지 검증
        public void Bind_RendersOneCardViewPerHandCard()
        {
            var context = CreateContext(); // 공통 카드 UI 테스트 컨텍스트 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 킹 포함 초기 손패 5장 구성
                context.HandUI.Bind(context.BoardInput); // 손패 UI에 실제 BoardInputController 연결

                Assert.AreEqual(context.RunState.Hand.Hand.Count, context.HandUI.CardCount); // 실제 손패 장수와 표시 카드 수가 같아야 함
                Assert.AreEqual(5, context.HandUI.CardCount); // 프로토타입 초기 손패는 5장이어야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 시작 배치에서는 킹 카드만 드래그 가능 상태로 표시되는지 검증
        public void InitialDeployment_OnlyKingCardIsInteractableBeforeKingPlacement()
        {
            var context = CreateContext(); // 초기 배치 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 킹 포함 손패 구성
                context.HandUI.Bind(context.BoardInput); // UI 연결

                var kingView = context.HandUI.FindCardView(context.Definitions[0]); // 킹 카드 UI 탐색
                var nonKingView = context.HandUI.FindFirstNonKingCardView(); // 비킹 카드 UI 탐색

                Assert.IsNotNull(kingView); // 킹 카드 UI가 존재해야 함
                Assert.IsNotNull(nonKingView); // 비교할 비킹 카드 UI도 존재해야 함
                Assert.IsTrue(kingView.IsInteractable); // 킹은 시작 배치에서 드래그 가능해야 함
                Assert.IsFalse(nonKingView.IsInteractable); // 킹 이전에는 다른 카드 드래그가 잠겨야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // UI 드롭 경로로 킹을 소환하면 손패가 갱신되고 배치 턴은 계속 유지되는지 검증
        public void TryDropCardAtCell_InitialKing_RemovesCardAndKeepsDeploymentOpen()
        {
            var context = CreateContext(); // 초기 배치 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 시작 손패 구성
                context.HandUI.Bind(context.BoardInput); // 손패 UI 연결
                var kingView = context.HandUI.FindCardView(context.Definitions[0]); // 킹 카드 UI 탐색
                int handBefore = context.RunState.Hand.Hand.Count; // 드롭 전 손패 장수 저장

                bool result = context.HandUI.TryDropCardAtCell(kingView, new Vector2Int(0, 0)); // 실제 드래그 종료가 호출할 셀 기반 소환 경로 실행

                Assert.IsTrue(result); // 유효한 킹 드롭은 성공해야 함
                Assert.AreEqual(handBefore - 1, context.RunState.Hand.Hand.Count); // 손패에서 킹 카드 1장이 제거돼야 함
                Assert.AreEqual(context.RunState.Hand.Hand.Count, context.HandUI.CardCount); // 손패 UI도 즉시 새 카드 수로 갱신돼야 함
                Assert.AreEqual(TurnState.DeploymentTurn, context.TurnManager.CurrentState); // 자유 배치 턴은 자동 종료되면 안 됨
                Assert.IsTrue(context.TurnManager.IsInitialKingPlaced); // 킹 필수 조건이 충족돼야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 일반 PlayerTurn에서 UI 카드 드롭 소환이 성공하면 즉시 EnemyTurn이 되는지 검증
        public void TryDropCardAtCell_PlayerTurn_SummonEndsPlayerTurn()
        {
            var context = CreateStartedBattleContext(); // 시작 배치가 끝난 1턴 PlayerTurn 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.HandUI.Bind(context.BoardInput); // 현재 손패를 UI에 표시
                var summonView = context.HandUI.FindFirstNonKingCardView(); // 일반 턴에 소환할 카드 UI 탐색
                Assert.IsNotNull(summonView); // 소환할 카드가 있어야 함

                bool result = context.HandUI.TryDropCardAtCell(summonView, new Vector2Int(2, 2)); // 카드 UI를 아군 빈 칸에 드롭

                Assert.IsTrue(result); // 소환 성공
                Assert.AreEqual(TurnState.EnemyTurn, context.TurnManager.CurrentState); // 일반 턴 소환은 즉시 적 턴으로 전환
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 잘못된 셀에 UI 카드를 드롭하면 손패 카드가 소비되지 않는지 검증
        public void TryDropCardAtCell_InvalidEnemyArea_ReturnsCardToHandState()
        {
            var context = CreateContext(); // 초기 배치 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 시작 손패 구성
                context.HandUI.Bind(context.BoardInput); // 손패 UI 연결
                var kingView = context.HandUI.FindCardView(context.Definitions[0]); // 시작 배치 가능 킹 카드 UI 탐색
                int handBefore = context.RunState.Hand.Hand.Count; // 실패 전 손패 수 저장

                bool result = context.HandUI.TryDropCardAtCell(kingView, new Vector2Int(0, 9)); // 적 영역에 잘못 드롭

                Assert.IsFalse(result); // 잘못된 드롭은 실패해야 함
                Assert.AreEqual(handBefore, context.RunState.Hand.Hand.Count); // 손패 카드가 소비되면 안 됨
                Assert.AreEqual(handBefore, context.HandUI.CardCount); // UI 카드 수도 그대로 유지돼야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }


        [Test] // 런타임으로 생성된 모든 카드에 CanvasGroup이 반드시 붙어 MissingComponentException을 방지하는지 검증
        public void CardView_Bind_AlwaysEnsuresCanvasGroup()
        {
            var context = CreateContext(); // 공통 손패 UI 테스트 컨텍스트 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 킹 포함 초기 손패 구성
                context.HandUI.Bind(context.BoardInput); // CardView들을 런타임 생성
                var kingView = context.HandUI.FindCardView(context.Definitions[0]); // 킹 카드 UI 탐색

                Assert.IsNotNull(kingView); // 킹 CardView가 존재해야 함
                Assert.IsNotNull(kingView.GetComponent<CanvasGroup>()); // 드래그에서 사용하는 CanvasGroup이 반드시 존재해야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 좌클릭 드래그 중 카드 UI는 완전히 숨기고 3D 고스트만 조작 대상으로 남기는지 검증
        public void CardView_BeginDrag_HidesCardVisual_AndInvalidReleaseRestoresIt()
        {
            var context = CreateContext(); // 공통 손패 UI 테스트 컨텍스트 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 초기 손패 구성
                context.HandUI.Bind(context.BoardInput); // 카드 UI 생성
                var kingView = context.HandUI.FindCardView(context.Definitions[0]); // 시작 배치 가능한 킹 카드 UI 탐색
                var canvasGroup = kingView.GetComponent<CanvasGroup>(); // 카드 전체 표시·Raycast 제어 컴포넌트 확보
                var pointer = new PointerEventData(EventSystem.current) // 실제 EventSystem을 사용하는 포인터 이벤트 데이터 생성
                {
                    button = PointerEventData.InputButton.Left, // 좌클릭 드래그로 설정
                    position = new Vector2(960f, 700f) // 화면 위쪽 드래그 위치를 가정
                };

                kingView.OnBeginDrag(pointer); // 좌클릭 드래그 시작 직접 호출

                Assert.AreEqual(0f, canvasGroup.alpha, 0.001f); // 드래그 중에는 카드 이미지가 완전히 보이지 않아야 함
                Assert.IsFalse(canvasGroup.blocksRaycasts); // 보드 Raycast를 카드 UI가 가로막지 않아야 함

                pointer.position = new Vector2(-100f, -100f); // 보드 밖 위치로 이동해 실패 Drop 상황 구성
                kingView.OnEndDrag(pointer); // 좌클릭 Release 처리

                Assert.AreEqual(1f, canvasGroup.alpha, 0.001f); // 실패 Drop이면 카드 UI가 다시 보여야 함
                Assert.IsTrue(canvasGroup.blocksRaycasts); // UI Raycast도 원상 복구돼야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 카드를 위로 드래그할 때 손패가 화면 아래로 내려가고 드래그 종료 시 복귀하는지 검증
        public void DragPresentation_LowersAndRestoresHand()
        {
            var context = CreateContext(); // 손패 UI 테스트 컨텍스트 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 실제 시작 손패 구성
                context.HandUI.Bind(context.BoardInput); // 화면 하단 손패 UI 생성

                context.HandUI.LowerHandForDrag(); // 카드가 손패 위쪽으로 올라간 상황을 직접 재현
                Assert.IsTrue(context.HandUI.IsHandLowered); // 손패가 아래로 내려간 상태여야 함
                Assert.That(context.HandUI.HandAnchoredPosition.y, Is.LessThan(-100f)); // 실제 Y 위치도 화면 아래 방향이어야 함

                context.HandUI.RestoreHandAfterDrag(); // 좌클릭을 놓아 드래그가 끝난 상황 재현
                Assert.IsFalse(context.HandUI.IsHandLowered); // 드래그 종료 후 기본 상태로 복귀해야 함
                Assert.That(context.HandUI.HandAnchoredPosition.y, Is.GreaterThanOrEqualTo(0f)); // 손패가 다시 화면 하단 안쪽으로 돌아와야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 카드 드래그가 유효한 보드 칸을 가리키면 실제 기물 실루엣 고스트가 생성되는지 검증
        public void PreviewCardDropAtCell_CreatesPieceGhostOnTargetCell()
        {
            var context = CreateContext(); // 초기 배치 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 킹 포함 시작 손패 구성
                var king = context.Definitions[0]; // 초기 배치에서 사용할 킹 카드 정의

                bool result = context.BoardInput.PreviewCardDropAtCell(king, new Vector2Int(0, 0)); // 아군 빈 칸에 고스트 프리뷰 요청

                Assert.IsTrue(result); // 유효한 소환 위치여야 함
                Assert.IsTrue(context.BoardInput.HasCardDropGhost); // 실제 3D 기물 고스트 오브젝트가 존재해야 함
                Assert.AreEqual(new Vector2Int(0, 0), context.BoardInput.CardDropPreviewCell); // 고스트가 현재 목표 셀을 추적해야 함
                Assert.IsTrue(context.BoardInput.IsCardDropPreviewValid); // 유효 위치 색상 상태여야 함

                context.BoardInput.ClearCardDropPreview(); // 드래그 종료 상황처럼 프리뷰 정리
                Assert.IsFalse(context.BoardInput.HasCardDropGhost); // 고스트가 제거돼야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 점유된 칸을 가리킬 때도 위치 확인용 고스트는 보이지만 실제 Drop은 유효하지 않은지 검증
        public void PreviewCardDropAtCell_OccupiedCell_ShowsInvalidGhost()
        {
            var context = CreateContext(); // 초기 배치 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 시작 손패 구성
                var blocker = new PieceRuntimeState(context.Definitions[1], new Vector2Int(1, 1), true); // 점유 상태를 만들 테스트 기물 생성
                context.RunState.Board.GetTile(new Vector2Int(1, 1)).OccupyingPiece = blocker; // 아군 영역의 목표 칸을 미리 점유

                bool result = context.BoardInput.PreviewCardDropAtCell(context.Definitions[0], new Vector2Int(1, 1)); // 점유 칸에 프리뷰 요청

                Assert.IsFalse(result); // 실제 소환 가능한 위치는 아니어야 함
                Assert.IsTrue(context.BoardInput.HasCardDropGhost); // 그래도 마우스가 가리키는 위치를 보여주는 고스트는 존재해야 함
                Assert.IsFalse(context.BoardInput.IsCardDropPreviewValid); // 잘못된 위치라는 상태를 기록해야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        private static TestContext CreateStartedBattleContext() // 초기 킹 배치를 끝내고 1턴 PlayerTurn 상태를 만드는 보조 메서드
        {
            var context = CreateContext(); // 초기 배치 상태 생성
            context.BoardInput.EnsurePrototypeStartingHand(); // 킹 포함 시작 손패 구성
            context.HandUI.Bind(context.BoardInput); // 카드 UI 연결
            var kingView = context.HandUI.FindCardView(context.Definitions[0]); // 킹 카드 UI 탐색
            Assert.IsTrue(context.HandUI.TryDropCardAtCell(kingView, new Vector2Int(0, 0))); // UI 드롭으로 킹 배치
            Assert.IsTrue(context.TurnManager.TryEndDeploymentTurn()); // 자유 배치를 명시적으로 종료
            Assert.AreEqual(TurnState.PlayerTurn, context.TurnManager.CurrentState); // 1턴 PlayerTurn 확인
            return context; // 준비된 컨텍스트 반환
        }

        private static TestContext CreateContext() // 손패 UI 테스트에 필요한 공통 객체를 만드는 메서드
        {
            var root = new GameObject("HandUITestRoot"); // 테스트 루트 GameObject 생성
            var boardView = root.AddComponent<BoardView>(); // 실제 보드 좌표/상태 뷰 추가
            var boardInput = root.AddComponent<BoardInputController>(); // 실제 소환 로직을 가진 입력 컨트롤러 추가
            var handUI = root.AddComponent<HandUI>(); // 18일차 카드 이미지 손패 UI 추가
            var runState = new RunState(3); // 실제 전투와 같은 RunState 생성
            var turnManager = new TurnManager(); // 초기 배치 턴으로 시작하는 턴 매니저 생성
            var definitions = CreateDefinitions(); // 킹~퀸 6종 테스트 카드 정의 생성

            SetPrivateField(boardInput, "_kingDefinition", definitions[0]); // 킹 데이터 주입
            SetPrivateField(boardInput, "_pawnDefinition", definitions[1]); // 폰 데이터 주입
            SetPrivateField(boardInput, "_knightDefinition", definitions[2]); // 나이트 데이터 주입
            SetPrivateField(boardInput, "_bishopDefinition", definitions[3]); // 비숍 데이터 주입
            SetPrivateField(boardInput, "_rookDefinition", definitions[4]); // 룩 데이터 주입
            SetPrivateField(boardInput, "_queenDefinition", definitions[5]); // 퀸 데이터 주입

            boardView.Bind(runState.Board); // 보드 뷰에 실제 RunState.Board 연결
            boardInput.Bind(runState, boardView, turnManager); // 입력에 RunState와 TurnManager 연결

            return new TestContext(root, boardInput, handUI, runState, turnManager, definitions); // 테스트 컨텍스트 반환
        }

        private static PieceDefinition[] CreateDefinitions() // 킹~퀸 6종 테스트 정의를 생성하는 보조 메서드
        {
            var types = new[] // 카드별 이동 타입 배열
            {
                PieceMovementType.King, // 킹
                PieceMovementType.Pawn, // 폰
                PieceMovementType.Knight, // 나이트
                PieceMovementType.Bishop, // 비숍
                PieceMovementType.Rook, // 룩
                PieceMovementType.Queen // 퀸
            };
            var definitions = new PieceDefinition[types.Length]; // 같은 길이의 정의 배열 생성

            for (int i = 0; i < definitions.Length; i++) // 각 기물 정의를 순회하며
            {
                definitions[i] = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 ScriptableObject 생성
                SetPrivateField(definitions[i], "_movementType", types[i]); // 실제 이동 타입 주입
                SetPrivateField(definitions[i], "_displayName", types[i].ToString()); // 카드 이름 주입
                SetPrivateField(definitions[i], "_baseHp", 3 + i); // 카드 우하단 HP 테스트 값 주입
                SetPrivateField(definitions[i], "_baseAtk", 1 + i); // 카드 좌하단 ATK 테스트 값 주입
            }

            return definitions; // 완성된 테스트 카드 정의 배열 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // private 직렬화 필드에 값을 주입하는 보조 메서드
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정한 private 필드 탐색
            Assert.IsNotNull(field, $"필드 {fieldName}을 찾을 수 없습니다."); // 필드명이 바뀌면 명확한 테스트 실패를 만들기
            field.SetValue(target, value); // 테스트 값 주입
        }

        private sealed class TestContext // 테스트 객체와 정리 책임을 묶는 내부 컨텍스트 클래스
        {
            public GameObject Root { get; } // 테스트 루트
            public BoardInputController BoardInput { get; } // 실제 카드 소환 입력 컨트롤러
            public HandUI HandUI { get; } // 18일차 손패 UI
            public RunState RunState { get; } // 실제 런 상태
            public TurnManager TurnManager { get; } // 실제 턴 상태
            public PieceDefinition[] Definitions { get; } // 테스트 카드 정의 배열

            public TestContext(GameObject root, BoardInputController boardInput, HandUI handUI, RunState runState, TurnManager turnManager, PieceDefinition[] definitions) // 생성자
            {
                Root = root; // 루트 저장
                BoardInput = boardInput; // 입력 컨트롤러 저장
                HandUI = handUI; // 손패 UI 저장
                RunState = runState; // 런 상태 저장
                TurnManager = turnManager; // 턴 매니저 저장
                Definitions = definitions; // 카드 정의 저장
            }

            public void Dispose() // 테스트 자원을 정리하는 메서드
            {
                Object.DestroyImmediate(Root); // 루트와 자식 Canvas/CardView 제거
                foreach (var definition in Definitions) // 생성한 카드 정의를 순회하며
                {
                    if (definition != null) Object.DestroyImmediate(definition); // ScriptableObject 제거
                }
            }
        }
    }
}
