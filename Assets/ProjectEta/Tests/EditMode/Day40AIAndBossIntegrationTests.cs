using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using System.Reflection; // private 직렬화 필드 테스트 주입에 사용하는 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // Resources, ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleHooks를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Boss; // LargePieceTurnEndStatusBridge를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceDatabase를 사용하기 위한 네임스페이스
using ProjectEta.Round; // RoundDefinition과 RoundRuntimeController를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState와 RunSaveData를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day40AIAndBossIntegrationTests // 40일차 AI·보스·저장·대형 기물 통합 회귀 테스트
    {
        [Test] // 5·10라운드가 동일한 보스 통합 데이터로 선택되는지 검증
        public void RoundRuntimeController_ResolvesBossProfileForRound5And10()
        {
            Assert.AreEqual("PrototypeRound36", RoundRuntimeController.ResolveRoundResourceName(1)); // 일반 라운드는 기존 데이터
            Assert.AreEqual("PrototypeBossRound40", RoundRuntimeController.ResolveRoundResourceName(5)); // 5라운드는 보스 데이터
            Assert.AreEqual("PrototypeRound36", RoundRuntimeController.ResolveRoundResourceName(6)); // 일반 라운드는 다시 기존 데이터
            Assert.AreEqual("PrototypeBossRound40", RoundRuntimeController.ResolveRoundResourceName(10)); // 10라운드는 보스 데이터
        }

        [Test] // 새 보스 RoundDefinition 에셋의 필수 통합 데이터 검증
        public void PrototypeBossRound40_LoadsBossConfiguration()
        {
            var definition = Resources.Load<RoundDefinition>("PrototypeBossRound40"); // 보스 라운드 Resources 로드

            Assert.IsNotNull(definition); // 별도 Inspector 연결 없이 로드 가능
            Assert.IsTrue(definition.IsBossRound); // 보스 라운드 플래그 확인
            Assert.IsTrue(definition.HasBossConfiguration); // 실제 보스 리소스 설정 확인
            Assert.AreEqual("PrototypeBoss37", definition.BossResourceName); // 기존 37~39일차 보스 정의 재사용
            Assert.AreEqual(new Vector2Int(0, 8), definition.BossAnchor); // 2x2 기준 좌표 확인
            Assert.AreEqual(4, definition.InitialEnemies.Count); // 일반 역할 AI 검증용 시작 적 4기
            Assert.AreEqual(2, definition.Reinforcements.Count); // 턴 기반 증원 2건 유지
        }

        [Test] // 2x2 보스 네 칸이 저장 데이터 한 건으로만 기록되는지 검증
        public void RunState_SaveLargePiece_WritesSingleBoardEntry()
        {
            var bossDefinition = Resources.Load<PieceDefinition>("PrototypeBoss37"); // 실제 프로토타입 보스 정의 로드
            Assert.IsNotNull(bossDefinition); // 보스 데이터 존재 확인

            var runState = new RunState(3); // 테스트 런 생성
            var anchor = new Vector2Int(0, 8); // 보스 기준 좌표
            var boss = new PieceRuntimeState(bossDefinition, anchor, false); // 적 보스 런타임 생성
            Assert.IsTrue(runState.Board.TryOccupyArea(anchor, bossDefinition.OccupancySize, boss)); // 2x2 네 칸 점유

            RunSaveData saveData = runState.ToSaveData(); // 런 저장 데이터 생성
            int bossEntries = 0; // 보스 저장 항목 수

            for (int i = 0; i < saveData.boardPieces.Count; i++) // 저장된 보드 기물 순회
            {
                if (saveData.boardPieces[i].pieceId == bossDefinition.PieceId) bossEntries++; // 동일 보스 항목 누적
            }

            Assert.AreEqual(1, bossEntries); // 점유 네 칸이어도 기물 한 건만 저장
            Assert.AreEqual(anchor.x, saveData.boardPieces[0].x); // 기준 X 좌표 저장 확인
            Assert.AreEqual(anchor.y, saveData.boardPieces[0].y); // 기준 Y 좌표 저장 확인
        }

        [Test] // 2x2 보스 저장 복원 후 네 칸이 같은 런타임 기물을 가리키는지 검증
        public void RunState_RestoreLargePiece_RebuildsWholeFootprint()
        {
            var bossDefinition = Resources.Load<PieceDefinition>("PrototypeBoss37"); // 실제 보스 정의 로드
            Assert.IsNotNull(bossDefinition); // 보스 데이터 존재 확인

            var database = ScriptableObject.CreateInstance<PieceDatabase>(); // 테스트용 PieceDatabase 생성
            SetPrivateField(database, "_definitions", new List<PieceDefinition> { bossDefinition }); // 보스 정의 직접 등록

            var saveData = new RunSaveData // 보스 한 건만 가진 저장 데이터 생성
            {
                kingHp = 3, // 킹 체력
                currentRound = 5, // 보스 라운드
                metaCurrency = 0 // 메타 재화
            };

            saveData.boardPieces.Add(new PieceSaveData // 2x2 보스 저장 항목 추가
            {
                x = 0, // 기준 X
                y = 8, // 기준 Y
                pieceId = bossDefinition.PieceId, // 보스 PieceId
                currentHp = 9, // 중간 체력
                isPlayerPiece = false, // 적 진영
                movementCycleIndex = 0 // 순환 이동 미사용
            });

            RunState restored = RunState.FromSaveData(saveData, database); // 런 상태 복원
            var restoredBoss = restored.Board.GetTile(new Vector2Int(0, 8)).OccupyingPiece; // 기준 칸 보스 조회

            Assert.IsNotNull(restoredBoss); // 보스 복원 확인
            Assert.AreEqual(9, restoredBoss.CurrentHp); // 현재 HP 복원 확인
            Assert.AreSame(restoredBoss, restored.Board.GetTile(new Vector2Int(0, 9)).OccupyingPiece); // 왼쪽 위 점유 확인
            Assert.AreSame(restoredBoss, restored.Board.GetTile(new Vector2Int(1, 8)).OccupyingPiece); // 오른쪽 아래 점유 확인
            Assert.AreSame(restoredBoss, restored.Board.GetTile(new Vector2Int(1, 9)).OccupyingPiece); // 오른쪽 위 점유 확인
            Assert.AreEqual(1, restored.Board.CountPieces(false)); // 네 칸이어도 적 한 기로 계산
        }

        [Test] // 39일차 이전 방식으로 같은 대형 기물이 네 건 저장된 세이브도 한 기로 복원되는지 검증
        public void RunState_RestoreLegacyDuplicateLargeEntries_DeduplicatesByFootprint()
        {
            var bossDefinition = Resources.Load<PieceDefinition>("PrototypeBoss37"); // 실제 보스 정의 로드
            Assert.IsNotNull(bossDefinition); // 보스 데이터 존재 확인

            var database = ScriptableObject.CreateInstance<PieceDatabase>(); // 테스트 DB 생성
            SetPrivateField(database, "_definitions", new List<PieceDefinition> { bossDefinition }); // 보스 정의 등록

            var saveData = new RunSaveData // 구버전 형태 저장 데이터 생성
            {
                kingHp = 3, // 킹 체력
                currentRound = 5, // 보스 라운드
                metaCurrency = 0 // 메타 재화
            };

            saveData.boardPieces.Add(CreateBossSaveData(bossDefinition, 0, 8)); // 기존 기준 칸 저장
            saveData.boardPieces.Add(CreateBossSaveData(bossDefinition, 0, 9)); // 기존 중복 칸 저장
            saveData.boardPieces.Add(CreateBossSaveData(bossDefinition, 1, 8)); // 기존 중복 칸 저장
            saveData.boardPieces.Add(CreateBossSaveData(bossDefinition, 1, 9)); // 기존 중복 칸 저장

            RunState restored = RunState.FromSaveData(saveData, database); // 구버전 저장 복원

            Assert.AreEqual(1, restored.Board.CountPieces(false)); // 중복 항목이 있어도 실제 보스 한 기만 복원
            var restoredBoss = restored.Board.GetTile(new Vector2Int(0, 8)).OccupyingPiece; // 기준 보스 조회
            Assert.AreSame(restoredBoss, restored.Board.GetTile(new Vector2Int(1, 9)).OccupyingPiece); // 전체 점유가 같은 런타임 상태인지 확인
        }

        [Test] // PieceDatabase에 보스가 없어도 Resources 독립 보스 정의로 저장을 복원할 수 있는지 검증
        public void RunState_RestoreBoss_FallsBackToResourceDefinition()
        {
            var bossDefinition = Resources.Load<PieceDefinition>("PrototypeBoss37"); // 독립 Resources 보스 로드
            Assert.IsNotNull(bossDefinition); // 보스 리소스 확인

            var emptyDatabase = ScriptableObject.CreateInstance<PieceDatabase>(); // 보스가 등록되지 않은 빈 DB 생성
            var saveData = new RunSaveData // 최소 저장 데이터 생성
            {
                kingHp = 3, // 킹 체력
                currentRound = 10, // 최종 보스 테스트 라운드
                metaCurrency = 0 // 메타 재화
            };

            saveData.boardPieces.Add(CreateBossSaveData(bossDefinition, 0, 8)); // 보스 저장 항목 추가

            RunState restored = RunState.FromSaveData(saveData, emptyDatabase); // Resources fallback 복원 실행
            var restoredBoss = restored.Board.GetTile(new Vector2Int(0, 8)).OccupyingPiece; // 복원 보스 조회

            Assert.IsNotNull(restoredBoss); // DB 미등록이어도 보스 복원
            Assert.AreEqual(bossDefinition.PieceId, restoredBoss.Definition.PieceId); // 동일 보스 정의 확인
        }

        [Test] // 2x2 보스와 1x1 기물이 함께 있어도 TurnEnd 순회가 기물 수만큼만 실행되는지 검증
        public void LargePieceTurnEndStatusBridge_ProcessesEachRuntimePieceOnce()
        {
            var board = new BoardState(); // 테스트 보드 생성
            var bossDefinition = Resources.Load<PieceDefinition>("PrototypeBoss37"); // 2x2 보스 정의 로드
            Assert.IsNotNull(bossDefinition); // 보스 데이터 확인

            var boss = new PieceRuntimeState(bossDefinition, new Vector2Int(0, 8), false); // 대형 적 생성
            Assert.IsTrue(board.TryOccupyArea(boss.BoardPosition, bossDefinition.OccupancySize, boss)); // 2x2 전체 점유

            var normalDefinition = CreateOneByOneDefinition("day40_normal"); // 1x1 테스트 기물 정의 생성
            var normalPiece = new PieceRuntimeState(normalDefinition, new Vector2Int(4, 8), false); // 일반 적 생성
            board.GetTile(normalPiece.BoardPosition).OccupyingPiece = normalPiece; // 일반 적 한 칸 점유

            int processed = LargePieceTurnEndStatusBridge.ProcessUniquePieces(board, new BattleHooks()); // 고유 기물 기준 턴 종료 정산

            Assert.AreEqual(2, processed); // 보스 네 칸 + 일반 한 칸이어도 실제 두 기만 처리
        }

        private static PieceSaveData CreateBossSaveData(PieceDefinition bossDefinition, int x, int y) // 구버전 중복 보스 저장 항목 생성 도우미
        {
            return new PieceSaveData // 보스 스냅샷 반환
            {
                x = x, // 저장 X
                y = y, // 저장 Y
                pieceId = bossDefinition.PieceId, // 보스 PieceId
                currentHp = 10, // 테스트 체력
                isPlayerPiece = false, // 적 진영
                movementCycleIndex = 0 // 순환 이동 미사용
            };
        }

        private static PieceDefinition CreateOneByOneDefinition(string pieceId) // 상태 정산 순회용 1x1 기물 정의 생성
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 임시 PieceDefinition 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(definition, "_baseHp", 3); // HP 설정
            SetPrivateField(definition, "_baseAtk", 1); // ATK 설정
            SetPrivateField(definition, "_occupancySize", Vector2Int.one); // 1x1 점유 설정
            return definition; // 테스트 정의 반환
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value) // private 직렬화 필드 주입 도우미
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // private 필드 탐색
            Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조 변경 시 명확한 테스트 실패
            field.SetValue(target, value); // 테스트 값 주입
        }
    }
}
