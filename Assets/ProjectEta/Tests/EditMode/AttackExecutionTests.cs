using System.Reflection; // ScriptableObject의 private 직렬화 필드를 테스트에서 직접 채우기 위한 네임스페이스
using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager, TurnState, CombatResult를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class AttackExecutionTests // 12일차: HP·ATK 전투 판정이 실제 클릭 공격과 올바르게 연결되는지 검증하는 테스트 모음
    {
        private static (GameObject Root, BoardInputController Input, RunState RunState, TurnManager TurnManager) CreateBoundContext() // 테스트마다 반복되는 초기화를 모아둔 도우미 메서드
        {
            var root = new GameObject("AttackTestRoot"); // 테스트용 오브젝트 생성
            var boardView = root.AddComponent<BoardView>(); // 보드 뷰 추가
            var boardInput = root.AddComponent<BoardInputController>(); // 입력 컨트롤러 추가
            var runState = new RunState(3); // 실제 전투와 같은 방식의 런 상태 생성
            var turnManager = new TurnManager(); // 실제 전투와 같은 방식의 턴 매니저 생성

            boardView.Bind(runState.Board); // 보드 뷰에 실제 보드 연결
            boardInput.Bind(runState, boardView, turnManager); // 입력에 실제 런 상태와 턴 매니저 연결

            return (root, boardInput, runState, turnManager); // 테스트에서 바로 쓸 수 있도록 묶어서 반환
        }

        private static PieceDefinition CreateDefinition(int baseHp, int baseAtk) // 테스트용 HP·ATK 값을 가진 기물 정의를 만드는 도우미 메서드
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 인스펙터 없이 사용할 임시 기물 정의 생성
            SetPrivateField(definition, "_baseHp", baseHp); // private 직렬화 필드에 테스트용 HP 직접 대입
            SetPrivateField(definition, "_baseAtk", baseAtk); // private 직렬화 필드에 테스트용 ATK 직접 대입
            return definition; // 완성된 정의 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // 리플렉션으로 private 필드 값을 설정하는 공용 도우미 메서드
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance); // 대상 타입에서 지정한 이름의 private 인스턴스 필드 조회
            field.SetValue(target, value); // 조회한 필드에 값 대입
        }

        [Test] // 13일차: SpawnTestEnemy가 지정한 칸에 적 기물을 실제로 배치하는지 확인하는 테스트
        public void SpawnTestEnemy_PlacesEnemyPieceOnBoard()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var enemyDefinition = CreateDefinition(baseHp: 3, baseAtk: 1); // 테스트용 적 정의
                var position = new Vector2Int(4, 8); // 적 영역 안의 좌표

                var enemy = context.Input.SpawnTestEnemy(enemyDefinition, position); // 지정 좌표에 적 기물 배치 시도

                Assert.IsNotNull(enemy); // 소환이 성공해야 함
                Assert.IsFalse(enemy.IsPlayerPiece); // 소환된 기물은 적으로 표시돼야 함
                Assert.AreSame(enemy, context.RunState.Board.GetTile(position).OccupyingPiece); // 보드의 해당 칸이 이 기물로 점유돼야 함
                Assert.AreEqual(1, context.RunState.Board.CountPieces(isPlayerPiece: false)); // 적군 수가 정확히 1이어야 함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 13일차: 이미 점유된 칸에는 SpawnTestEnemy가 실패하는지 확인하는 테스트
        public void SpawnTestEnemy_Fails_WhenTileAlreadyOccupied()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var position = new Vector2Int(4, 8); // 배치를 시도할 좌표
                var blockerDefinition = CreateDefinition(baseHp: 1, baseAtk: 0); // 미리 칸을 막아둘 기물 정의
                var blocker = new PieceRuntimeState(blockerDefinition, position, isPlayerPiece: false); // 해당 칸을 먼저 점유할 기물 생성
                context.RunState.Board.GetTile(position).OccupyingPiece = blocker; // 칸을 미리 점유시킴

                var enemyDefinition = CreateDefinition(baseHp: 3, baseAtk: 1); // 새로 배치를 시도할 적 정의
                var result = context.Input.SpawnTestEnemy(enemyDefinition, position); // 이미 점유된 칸에 배치 시도

                Assert.IsNull(result); // 소환이 거부돼 null을 반환해야 함
                Assert.AreSame(blocker, context.RunState.Board.GetTile(position).OccupyingPiece); // 기존 점유 기물이 그대로 유지돼야 함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 14일차: SpawnTestEnemySquad가 폰+룩을 각각 지정한 위치에 배치하는지 확인하는 테스트
        public void SpawnTestEnemySquad_PlacesPawnAndRookAtExpectedPositions()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var pawnDefinition = CreateDefinition(baseHp: 1, baseAtk: 1); // 테스트용 폰 정의
                var rookDefinition = CreateDefinition(baseHp: 3, baseAtk: 2); // 테스트용 룩 정의
                SetPrivateField(context.Input, "_pawnDefinition", pawnDefinition); // 인스펙터 연결을 흉내내 private 필드에 직접 대입
                SetPrivateField(context.Input, "_rookDefinition", rookDefinition); // 인스펙터 연결을 흉내내 private 필드에 직접 대입

                var anchor = new Vector2Int(4, 8); // 적 스쿼드 기준 좌표
                context.Input.SpawnTestEnemySquad(anchor); // 폰+룩 2기 배치 실행

                Assert.AreSame(pawnDefinition, context.RunState.Board.GetTile(anchor).OccupyingPiece.Definition); // 기준 칸에 폰이 배치돼야 함
                Assert.AreSame(rookDefinition, context.RunState.Board.GetTile(anchor + new Vector2Int(2, 0)).OccupyingPiece.Definition); // 오른쪽 2칸에 룩이 배치돼야 함
                Assert.AreEqual(2, context.RunState.Board.CountPieces(isPlayerPiece: false)); // 적군 수가 정확히 2여야 함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 비치명 공격 시 HP만 줄고 양측 위치는 그대로 유지되는지 확인하는 테스트
        public void TryAttackSelectedPieceTarget_NonLethal_ReducesHpButKeepsPositions()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var attackerDefinition = CreateDefinition(baseHp: 3, baseAtk: 2); // ATK 2인 공격자 정의
                var defenderDefinition = CreateDefinition(baseHp: 5, baseAtk: 0); // HP 5(2 피해로는 죽지 않음)인 대상 정의
                var attackerOrigin = new Vector2Int(4, 1); // 공격자 시작 좌표
                var defenderOrigin = new Vector2Int(4, 2); // 대상 좌표(공격자와 인접)

                var attacker = new PieceRuntimeState(attackerDefinition, attackerOrigin, isPlayerPiece: true); // 아군 공격자 생성
                var defender = new PieceRuntimeState(defenderDefinition, defenderOrigin, isPlayerPiece: false); // 적 대상 생성
                context.RunState.Board.GetTile(attackerOrigin).OccupyingPiece = attacker; // 보드에 공격자 배치
                context.RunState.Board.GetTile(defenderOrigin).OccupyingPiece = defender; // 보드에 대상 배치

                context.Input.TrySelectPieceAt(attackerOrigin); // 공격자 선택
                CombatResult observedResult = null; // 이벤트로 전달되는 전투 결과를 담을 변수
                context.Input.AttackResolved += result => observedResult = result; // 전투 결과 이벤트 구독

                bool attacked = context.Input.TryAttackSelectedPieceTarget(defenderOrigin); // 인접한 적 공격 시도

                Assert.IsTrue(attacked); // 공격이 실행돼야 함
                Assert.AreEqual(3, defender.CurrentHp); // 5 - 2 = 3 HP만 남아야 함
                Assert.AreEqual(attackerOrigin, attacker.BoardPosition); // 비치명 공격이므로 공격자는 원위치를 유지해야 함
                Assert.AreEqual(defenderOrigin, defender.BoardPosition); // 대상도 자리를 유지해야 함
                Assert.AreSame(defender, context.RunState.Board.GetTile(defenderOrigin).OccupyingPiece); // 대상 칸의 점유는 그대로 대상이어야 함
                Assert.IsNotNull(observedResult); // 전투 결과 이벤트가 발생해야 함
                Assert.IsFalse(observedResult.DefenderDied); // 이벤트로 전달된 결과도 비치명이어야 함
                Assert.AreEqual(TurnState.EnemyTurn, context.TurnManager.CurrentState); // 공격도 플레이어 행동으로 처리돼 적 턴으로 전환돼야 함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 치명 공격 시 대상이 제거되고 공격자가 대상 칸을 점유하는지 확인하는 테스트
        public void TryAttackSelectedPieceTarget_Lethal_RemovesDefenderAndMovesAttackerIntoTile()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var attackerDefinition = CreateDefinition(baseHp: 3, baseAtk: 2); // ATK 2인 공격자 정의
                var defenderDefinition = CreateDefinition(baseHp: 1, baseAtk: 0); // HP 1(2 피해면 사망)인 대상 정의
                var attackerOrigin = new Vector2Int(4, 1); // 공격자 시작 좌표
                var defenderOrigin = new Vector2Int(4, 2); // 대상 좌표(공격자와 인접)

                var attacker = new PieceRuntimeState(attackerDefinition, attackerOrigin, isPlayerPiece: true); // 아군 공격자 생성
                var defender = new PieceRuntimeState(defenderDefinition, defenderOrigin, isPlayerPiece: false); // 적 대상 생성
                context.RunState.Board.GetTile(attackerOrigin).OccupyingPiece = attacker; // 보드에 공격자 배치
                context.RunState.Board.GetTile(defenderOrigin).OccupyingPiece = defender; // 보드에 대상 배치

                context.Input.TrySelectPieceAt(attackerOrigin); // 공격자 선택
                bool attacked = context.Input.TryAttackSelectedPieceTarget(defenderOrigin); // 인접한 적 공격 시도(치명타)

                Assert.IsTrue(attacked); // 공격이 실행돼야 함
                Assert.IsTrue(defender.IsDead); // 대상 HP가 0 이하로 사망 처리돼야 함
                Assert.AreEqual(defenderOrigin, attacker.BoardPosition); // 공격자가 대상이 있던 칸으로 이동(점유)해야 함
                Assert.IsNull(context.RunState.Board.GetTile(attackerOrigin).OccupyingPiece); // 공격자의 원래 칸은 비어야 함
                Assert.AreSame(attacker, context.RunState.Board.GetTile(defenderOrigin).OccupyingPiece); // 대상 칸은 이제 공격자가 점유해야 함
                Assert.AreEqual(TurnState.EnemyTurn, context.TurnManager.CurrentState); // 공격도 플레이어 행동으로 처리돼 적 턴으로 전환돼야 함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 13일차: 마지막 적을 처치하면 BoardState.CountPieces가 적군 0을 반환하는지 확인하는 테스트(승리 조건이 의존하는 데이터)
        public void TryAttackSelectedPieceTarget_Lethal_LeavesZeroRemainingEnemies()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var attackerDefinition = CreateDefinition(baseHp: 3, baseAtk: 5); // 확실히 처치 가능한 ATK 5 공격자
                var defenderDefinition = CreateDefinition(baseHp: 1, baseAtk: 0); // HP 1인 유일한 적 정의
                var attackerOrigin = new Vector2Int(4, 1); // 공격자 시작 좌표
                var defenderOrigin = new Vector2Int(4, 2); // 유일한 적의 좌표(공격자와 인접)

                var attacker = new PieceRuntimeState(attackerDefinition, attackerOrigin, isPlayerPiece: true); // 아군 공격자 생성
                var defender = new PieceRuntimeState(defenderDefinition, defenderOrigin, isPlayerPiece: false); // 보드 위 유일한 적 생성
                context.RunState.Board.GetTile(attackerOrigin).OccupyingPiece = attacker; // 보드에 공격자 배치
                context.RunState.Board.GetTile(defenderOrigin).OccupyingPiece = defender; // 보드에 유일한 적 배치

                context.Input.TrySelectPieceAt(attackerOrigin); // 공격자 선택
                context.Input.TryAttackSelectedPieceTarget(defenderOrigin); // 유일한 적 처치

                Assert.AreEqual(0, context.RunState.Board.CountPieces(isPlayerPiece: false)); // 남은 적이 0이어야 승리 판정이 정상 동작함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 공격 후보 목록에 없는 칸은 공격이 거부되고 상태가 변하지 않는지 확인하는 테스트
        public void TryAttackSelectedPieceTarget_Fails_WhenTargetIsNotAnAttackCandidate()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var attackerDefinition = CreateDefinition(baseHp: 3, baseAtk: 2); // ATK 2인 공격자 정의
                var attackerOrigin = new Vector2Int(4, 1); // 공격자 시작 좌표
                var emptyFarTile = new Vector2Int(0, 0); // 공격 후보에 포함될 수 없는 먼 빈 칸

                var attacker = new PieceRuntimeState(attackerDefinition, attackerOrigin, isPlayerPiece: true); // 아군 공격자 생성
                context.RunState.Board.GetTile(attackerOrigin).OccupyingPiece = attacker; // 보드에 공격자 배치

                context.Input.TrySelectPieceAt(attackerOrigin); // 공격자 선택
                bool attacked = context.Input.TryAttackSelectedPieceTarget(emptyFarTile); // 후보가 아닌 칸을 공격 시도

                Assert.IsFalse(attacked); // 공격이 거부돼야 함
                Assert.AreEqual(attackerOrigin, attacker.BoardPosition); // 공격자 위치가 그대로 유지돼야 함
                Assert.AreEqual(TurnState.PlayerTurn, context.TurnManager.CurrentState); // 턴도 넘어가지 않아야 함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }
    }
}
