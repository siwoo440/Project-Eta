using System.Reflection; // ScriptableObject의 private 직렬화 필드를 테스트에서 직접 채우기 위한 네임스페이스
using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEditor; // SerializedObject로 private 직렬화 필드를 테스트용으로 설정하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleHooks, CombatResolver, StatusEffectTickResolver, TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스
using ProjectEta.UI; // CombatLogUI를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day32CombatFeedbackTests // 32일차 전투 로그·배치 턴 배너가 실제 훅·턴 전환에 정확히 반응하는지 검증하는 테스트 모음
    {
        private static (GameObject Root, BoardInputController Input, RunState RunState, TurnManager TurnManager, CombatLogUI Log) CreateBoundContext() // 공통 초기화 도우미(Day29 패턴과 동일하게 CombatLogUI까지 붙여 반환)
        {
            var root = new GameObject("Day32FeedbackTestRoot"); // 테스트용 오브젝트 생성
            var boardView = root.AddComponent<BoardView>(); // 보드 뷰 추가
            var boardInput = root.AddComponent<BoardInputController>(); // 입력 컨트롤러 추가
            var log = root.AddComponent<CombatLogUI>(); // 32일차: 전투 로그 컴포넌트 추가
            var runState = new RunState(3); // 실제 전투와 같은 방식의 런 상태 생성
            var turnManager = new TurnManager(); // 실제 전투와 같은 방식의 턴 매니저 생성
            var hooks = new BattleHooks(); // 이번 테스트 전투가 사용할 훅 버스

            boardView.Bind(runState.Board); // 보드 뷰에 실제 보드 연결
            boardInput.Bind(runState, boardView, turnManager, hooks); // 입력에 실제 런 상태·턴 매니저·훅 버스 연결
            log.Bind(boardInput); // 전투 로그를 실제 훅에 연결

            turnManager.MarkInitialKingPlaced(); // 시작 배치는 킹을 놓아야만 끝나므로 필수 조건을 먼저 충족
            turnManager.TryEndDeploymentTurn(); // 일반 턴의 이동·공격을 검증하므로 시작 배치 턴을 명시적으로 종료해 PlayerTurn에서 시작

            return (root, boardInput, runState, turnManager, log); // 테스트에서 바로 쓸 수 있도록 묶어서 반환
        }

        private static PieceDefinition CreateDefinition(int baseHp, int baseAtk) // 테스트용 HP·ATK 값을 가진 King형 이동 기물 정의를 만드는 도우미 메서드
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

        [Test] // 기물이 이동하면 전투 로그에 이동 기록 한 줄이 추가되는지 검증
        public void MovingPiece_AddsMoveEntryToCombatLog()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var origin = new Vector2Int(4, 1); // 이동할 기물의 시작 좌표
                var destination = new Vector2Int(4, 2); // King형 이동으로 도달 가능한 인접 좌표
                var piece = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 1), origin, isPlayerPiece: true); // 테스트용 아군 기물
                context.RunState.Board.GetTile(origin).OccupyingPiece = piece; // 보드에 배치

                context.Input.TrySelectPieceAt(origin); // 기물 선택
                context.Input.TryMoveSelectedPieceTo(destination); // 이동 실행

                Assert.AreEqual(1, context.Log.EntryCount); // 이동 로그 한 줄만 추가되어야 함
                StringAssert.Contains("이동", context.Log.Entries[0]); // 이동 관련 문구 포함 확인
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 비치명 공격은 AfterAttack에서 한 줄만 기록되고 AfterDamage 중복 기록이 없는지 검증
        public void NonLethalAttack_AddsExactlyOneLogEntry()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var attackerOrigin = new Vector2Int(4, 1); // 공격자 시작 좌표
                var defenderOrigin = new Vector2Int(4, 2); // 대상 좌표(공격자와 인접)
                var attacker = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 2), attackerOrigin, isPlayerPiece: true); // 아군 공격자
                var defender = new PieceRuntimeState(CreateDefinition(baseHp: 5, baseAtk: 0), defenderOrigin, isPlayerPiece: false); // 적 대상(2 피해로는 생존)
                context.RunState.Board.GetTile(attackerOrigin).OccupyingPiece = attacker; // 보드에 공격자 배치
                context.RunState.Board.GetTile(defenderOrigin).OccupyingPiece = defender; // 보드에 대상 배치

                context.Input.TrySelectPieceAt(attackerOrigin); // 공격자 선택
                context.Input.TryAttackSelectedPieceTarget(defenderOrigin); // 공격 실행

                Assert.AreEqual(1, context.Log.EntryCount, "AfterAttack과 AfterDamage가 중복 기록되면 안 됩니다."); // 정확히 한 줄만 기록
                StringAssert.Contains("생존", context.Log.Entries[0]); // 생존 결과 문구 포함 확인
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 치명 공격은 처치 결과와 처치 후 전진 이동이 각각 한 줄씩 기록되고 중복 피해 기록은 없는지 검증
        public void LethalAttack_LogsDeathNoticeAndFollowUpMoveWithoutDuplicateDamageEntry()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var attackerOrigin = new Vector2Int(4, 1); // 공격자 시작 좌표
                var defenderOrigin = new Vector2Int(4, 2); // 대상 좌표(공격자와 인접)
                var attacker = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 2), attackerOrigin, isPlayerPiece: true); // 아군 공격자
                var defender = new PieceRuntimeState(CreateDefinition(baseHp: 1, baseAtk: 0), defenderOrigin, isPlayerPiece: false); // HP 1(2 피해면 사망)인 대상
                context.RunState.Board.GetTile(attackerOrigin).OccupyingPiece = attacker; // 보드에 공격자 배치
                context.RunState.Board.GetTile(defenderOrigin).OccupyingPiece = defender; // 보드에 대상 배치

                context.Input.TrySelectPieceAt(attackerOrigin); // 공격자 선택
                context.Input.TryAttackSelectedPieceTarget(defenderOrigin); // 공격 실행(치명타 -> 근접 처치 후 대상 칸으로 전진)

                Assert.AreEqual(2, context.Log.EntryCount, "치명 공격은 처치 결과 한 줄과 처치 후 전진 이동 한 줄, 총 두 줄이 기록되어야 합니다."); // AfterAttack(처치) + AfterMove(전진) = 2줄
                Assert.IsTrue(context.Log.Entries[0].Contains("처치") || context.Log.Entries[1].Contains("처치"), "둘 중 한 줄에는 처치 결과가 포함되어야 합니다."); // 처치 문구 확인
                Assert.IsTrue(context.Log.Entries[0].Contains("이동") || context.Log.Entries[1].Contains("이동"), "둘 중 한 줄에는 처치 후 전진 이동이 포함되어야 합니다."); // 전진 이동 문구 확인
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 공격자가 없는 상태 이상 틱 피해는 전투 로그에 별도로 기록되는지 검증
        public void StatusTickDamage_AddsSeparateLogEntry()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var position = new Vector2Int(4, 4); // 상태를 걸어둘 기물 좌표
                var piece = new PieceRuntimeState(CreateDefinition(baseHp: 10, baseAtk: 0), position, isPlayerPiece: true); // 체력 10짜리 테스트용 기물
                context.RunState.Board.GetTile(position).OccupyingPiece = piece; // 보드에 배치

                var poison = CreateStatusDefinition(StatusEffectType.Poison, StatusStackMode.StacksAdd, maxStacks: 3, durationTurns: 3, tickDamagePerStack: 2); // 중첩당 2피해 독(임시값)
                piece.ApplyStatus(poison); // 독 1중첩 적용

                StatusEffectTickResolver.ResolveTurnEndDamage(piece, context.Input.BattleHooks); // 턴 종료 틱 피해 정산(공격자 없음, source=null)

                Assert.AreEqual(1, context.Log.EntryCount, "상태 이상 틱 피해도 한 줄로 기록되어야 합니다."); // 실제 피해가 있었으므로 로그 한 줄 기록
                StringAssert.Contains("상태 이상 피해", context.Log.Entries[0]); // 상태 이상 피해 문구 포함 확인
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        private static StatusEffectDefinition CreateStatusDefinition(StatusEffectType statusType, StatusStackMode stackMode, int maxStacks, int durationTurns, int tickDamagePerStack) // 테스트 전용 상태 이상 정의 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<StatusEffectDefinition>(); // 인스턴스만 생성(에셋으로 저장하지 않음)
            var serialized = new SerializedObject(definition); // private 직렬화 필드에 접근하기 위한 SerializedObject
            serialized.FindProperty("_statusType").intValue = (int)statusType; // 상태 종류 설정
            serialized.FindProperty("_displayName").stringValue = statusType.ToString(); // 표시 이름 설정
            serialized.FindProperty("_stackMode").enumValueIndex = (int)stackMode; // 중첩 방식 설정
            serialized.FindProperty("_maxStacks").intValue = maxStacks; // 최대 중첩 수 설정
            serialized.FindProperty("_defaultDurationTurns").intValue = durationTurns; // 기본 지속 턴 설정
            serialized.FindProperty("_tickDamagePerStack").intValue = tickDamagePerStack; // 중첩당 틱 피해 설정
            serialized.FindProperty("_description").stringValue = "32일차 테스트용 임시 상태 이상 정의."; // 설명 설정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Undo 기록 없이 즉시 반영
            return definition; // 완성된 테스트용 정의 반환
        }

        [Test] // TurnStart 훅이 발행되면 턴 구분선 로그가 추가되는지 검증
        public void TurnStart_AddsTurnMarkerLogEntry()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                context.Input.BattleHooks.RaiseTurnStart(TurnState.PlayerTurn, 3); // 3턴 시작을 직접 발행

                Assert.AreEqual(1, context.Log.EntryCount); // 턴 시작 로그 한 줄이 추가되어야 함
                StringAssert.Contains("3턴", context.Log.Entries[0]); // 턴 번호가 포함되어야 함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 배치 턴으로 실제 전환될 때 배너 표시 시퀀스가 시작되는지, 일반 턴 전환에는 반응하지 않는지 검증
        public void DeploymentTurnBanner_ShowsOnlyOnDeploymentTurnTransition()
        {
            var root = new GameObject("Day32BannerTestRoot"); // 배너 전용 테스트 오브젝트 생성

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var banner = root.AddComponent<DeploymentTurnBannerUI>(); // 배너 컴포넌트 추가
                var turnManager = new TurnManager(); // 실제 전투와 같은 방식의 턴 매니저 생성
                banner.Bind(turnManager); // 배너를 턴 매니저에 연결

                turnManager.MarkInitialKingPlaced(); // 시작 배치는 킹을 놓아야만 끝나므로 필수 조건을 먼저 충족
                turnManager.TryEndDeploymentTurn(); // 초기 배치 종료 -> PlayerTurn(1턴) 진입

                Assert.IsFalse(banner.IsShowing, "일반 턴 진입 시에는 배너가 뜨지 않아야 합니다."); // 일반 턴에는 반응하지 않음 확인

                for (int i = 0; i < 4; i++) // 5턴째 적 턴 종료 시점까지 반복 진행(1~4턴)
                {
                    turnManager.TryCompletePlayerAction(); // 플레이어 행동 완료 -> EnemyTurn
                    turnManager.CompleteEnemyTurn(); // 적 턴 종료 -> 다음 PlayerTurn(주기 배치 시점 아님)
                }

                turnManager.TryCompletePlayerAction(); // 5턴 플레이어 행동 완료 -> EnemyTurn
                turnManager.CompleteEnemyTurn(); // 5턴째 적 턴 종료 -> 주기 배치 턴 진입(DeploymentInterval=5)

                Assert.IsTrue(banner.IsShowing, "배치 턴으로 실제 전환되면 배너 표시 시퀀스가 시작되어야 합니다."); // 배치 턴 전환 시 배너 표시 확인
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(root); // 테스트 오브젝트 정리
            }
        }
    }
}
