using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using System.Reflection; // 테스트용 private 직렬화 필드 주입에 사용하는 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementRuleData를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Round; // 36일차 라운드 데이터·UI 타입을 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day36RoundAndReinforcementTests // 36일차 라운드 데이터·증원·상단 정보 UI를 검증하는 테스트 모음
    {
        [Test] // 사용자가 요청한 상단 정보 문구 형식이 정확히 만들어지는지 검증
        public void RoundSummaryUI_BuildsRequestedDisplayText()
        {
            string text = RoundSummaryUI.BuildDisplayText(3, 5, 30, 6); // 예시 값으로 UI 문구 생성

            Assert.AreEqual("Round : 3    Turn : 5 / 30    현재 적 : 6", text); // 요청한 표기 형식과 정확히 일치해야 함
        }

        [Test] // Resources에 포함된 36일차 기본 라운드 데이터가 실제로 로드 가능한지 검증
        public void PrototypeRound36Asset_LoadsWithInitialEnemiesAndReinforcements()
        {
            var definition = Resources.Load<RoundDefinition>("PrototypeRound36"); // 기본 라운드 에셋 로드

            Assert.IsNotNull(definition); // 다운로드 후 별도 Inspector 설정 없이 Resources에서 로드돼야 함
            Assert.AreEqual(30, definition.TurnLimit); // 일반 라운드 제한은 30턴
            Assert.AreEqual(4, definition.InitialEnemies.Count); // 초기 적 4기
            Assert.AreEqual(2, definition.Reinforcements.Count); // 후속 증원 2건
            Assert.IsFalse(definition.IsBossRound); // 36일차 기본 라운드는 보스전이 아님
        }

        [Test] // 증원 데이터가 지정 턴 이전에는 실행되지 않고 지정 턴부터 실행 대상이 되는지 검증
        public void EnemySpawnDefinition_BecomesDueAtConfiguredTurn()
        {
            var spawn = new EnemySpawnDefinition("rook", new Vector2Int(4, 9), 5); // 5턴 증원 데이터 생성

            Assert.IsFalse(spawn.IsDue(4)); // 4턴에는 아직 증원 대상이 아님
            Assert.IsTrue(spawn.IsDue(5)); // 5턴부터 증원 대상
            Assert.IsTrue(spawn.IsDue(6)); // 처리되지 않았다면 이후 턴에도 due 상태 유지
        }

        [Test] // 현재 적 수가 보드의 실제 생존 적 기물 수와 일치하는지 검증
        public void RoundRuntimeController_CountEnemiesUsesBoardState()
        {
            var board = new BoardState(); // 빈 10x10 보드 생성
            var pawn = CreateDefinition("pawn", PieceMovementType.Pawn); // 테스트 폰 정의 생성
            var rook = CreateDefinition("rook", PieceMovementType.Rook); // 테스트 룩 정의 생성

            Place(board, pawn, new Vector2Int(4, 8), false); // 살아 있는 적 폰 배치
            Place(board, rook, new Vector2Int(6, 8), false); // 살아 있는 적 룩 배치
            Place(board, pawn, new Vector2Int(4, 1), true); // 플레이어 기물 배치

            Assert.AreEqual(2, RoundRuntimeController.CountCurrentEnemies(board)); // 적만 정확히 2기로 계산돼야 함
        }

        [Test] // 라운드 UI가 RunState의 현재 라운드와 턴 제한을 동적으로 사용하도록 표시 모델을 검증
        public void RoundSummaryModel_UsesRuntimeRoundTurnAndEnemyCount()
        {
            var runState = new RunState(3); // 런 상태 생성
            runState.CurrentRound = 3; // 현재 라운드를 3으로 설정
            var turnManager = new TurnManager(); // 시작 배치 턴의 TurnManager 생성
            var roundDefinition = ScriptableObject.CreateInstance<RoundDefinition>(); // 테스트용 라운드 정의 생성
            SetPrivateField(roundDefinition, "_turnLimit", 30); // 턴 제한 30 설정

            Place(runState.Board, CreateDefinition("pawn", PieceMovementType.Pawn), new Vector2Int(4, 8), false); // 적 1기 배치
            Place(runState.Board, CreateDefinition("rook", PieceMovementType.Rook), new Vector2Int(6, 8), false); // 적 2기 배치

            string text = RoundSummaryUI.BuildDisplayText( // 실제 런타임에서 사용하는 값으로 문구 생성
                runState.CurrentRound, // 현재 라운드
                turnManager.TurnNumber, // 현재 턴
                roundDefinition.TurnLimit, // 라운드 데이터 턴 제한
                RoundRuntimeController.CountCurrentEnemies(runState.Board)); // 현재 적 수

            Assert.AreEqual("Round : 3    Turn : 1 / 30    현재 적 : 2", text); // 런타임 값이 그대로 표시돼야 함
        }

        private static PieceRuntimeState Place(BoardState board, PieceDefinition definition, Vector2Int position, bool isPlayerPiece) // 테스트용 기물 배치 도우미
        {
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 생성
            board.GetTile(position).OccupyingPiece = piece; // 보드 점유 등록
            return piece; // 생성 기물 반환
        }

        private static PieceDefinition CreateDefinition(string pieceId, PieceMovementType movementType) // 테스트용 PieceDefinition 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 메모리 안에 임시 ScriptableObject 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(definition, "_category", PieceCategory.Basic); // 기본 기물 분류 설정
            SetPrivateField(definition, "_grade", PieceGrade.OneStar); // 테스트용 1성 등급 설정
            SetPrivateField(definition, "_movementType", movementType); // Legacy 이동 타입 설정
            SetPrivateField(definition, "_roleTags", PieceRoleTag.Attacker); // 최소 역할 태그 설정
            SetPrivateField(definition, "_baseHp", 3); // 테스트 HP 설정
            SetPrivateField(definition, "_baseAtk", 1); // 테스트 ATK 설정
            SetPrivateField(definition, "_occupancySize", Vector2Int.one); // 1x1 점유 설정
            SetPrivateField(definition, "_movementRules", Array.Empty<MovementRuleData>()); // Legacy 이동 경로 사용
            return definition; // 완성 정의 반환
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value) // private 필드 주입 공통 도우미
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 이름의 private 필드 탐색
            Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조 변경 시 명확하게 실패
            field.SetValue(target, value); // 테스트 값 주입
        }
    }
}
