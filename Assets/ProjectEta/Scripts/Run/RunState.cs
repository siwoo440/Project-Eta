using System; // StringComparison 사용
using System.Collections.Generic; // HashSet<T>·IReadOnlyList<T> 사용
using UnityEngine; // Vector2Int·Resources 사용
using ProjectEta.Battle; // BattleOutcome 사용
using ProjectEta.Board; // BoardState 사용
using ProjectEta.Cards; // DeckState·HandState 사용
using ProjectEta.Fusion; // FusionDiscoveryLog 사용
using ProjectEta.Pieces; // PieceDefinition·PieceRuntimeState·PieceDatabase 사용

namespace ProjectEta.Run // 런 관련 타입 네임스페이스
{
    public class RunState // 런 전체 상태 최상위 객체
    {
        private int _kingHp; // 킹 체력 내부 값

        public BattleState Battle { get; private set; } // 현재 전투 임시 상태
        public RoundState Round { get; } // 현재 1~10라운드 진행 상태
        public BoardState Board => Battle.Board; // 기존 호출부 호환 보드 접근
        public DeckState Deck { get; } // 런에서 유지되는 덱 상태
        public HandState Hand => Battle.Hand; // 기존 호출부 호환 손패 접근
        public FusionDiscoveryLog FusionDiscovery { get; } // 숨김 합성식 발견 기록
        public int MetaCurrency { get; set; } // 보유 메타 재화
        public bool IsDefeated => _kingHp <= 0; // 킹 체력 기반 패배 여부
        public RoundProgressStatus CurrentRoundStatus => Round.Status; // 현재 라운드 진행 상태
        public bool IsBossRound => Round.IsBossRound; // 현재 보스 라운드 여부
        public BattleOutcome LastBattleOutcome => Round.BattleOutcome; // 현재 라운드 전투 결과

        public int CurrentRound // 기존 라운드 번호 접근 호환 프로퍼티
        {
            get => Round.RoundNumber; // RoundState 라운드 번호 반환
            set => Round.SetRoundNumber(value); // RoundState 라운드 번호 변경
        }

        public int KingHp // 킹 체력 프로퍼티
        {
            get => _kingHp; // 현재 킹 체력 반환
            set => _kingHp = value < 0 ? 0 : value; // 음수 체력 방지
        }

        public RunState(int startingKingHp) // 새 런 상태 생성
        {
            _kingHp = startingKingHp; // 시작 킹 체력 저장
            Battle = new BattleState(); // 첫 전투 임시 상태 생성
            Round = new RoundState(RoundState.FirstRound); // 1라운드 상태 생성
            Deck = new DeckState(); // 새 덱 상태 생성
            FusionDiscovery = new FusionDiscoveryLog(); // 합성 발견 기록 생성
        }

        public void StartCurrentRound() // 현재 라운드 진행 시작
        {
            Round.Begin(); // 라운드 상태를 진행 중으로 변경
        }

        public void RecordBattleOutcome(BattleOutcome outcome) // 현재 라운드 전투 결과 기록
        {
            Round.Complete(outcome); // 승패 결과를 라운드 상태에 반영
        }

        public void ResetBattleState() // 다음 전투를 위한 임시 전투 상태 초기화
        {
            Battle = new BattleState(); // 새 보드·손패 상태로 교체
        }

        public RunSaveData ToSaveData() // 현재 런을 저장 데이터로 변환
        {
            var data = new RunSaveData // 저장 객체 생성
            {
                kingHp = _kingHp, // 킹 체력 기록
                currentRound = CurrentRound, // 라운드 번호 기록
                metaCurrency = MetaCurrency, // 메타 재화 기록
                roundStatus = (int)CurrentRoundStatus, // 라운드 진행 상태 기록
                battleOutcome = (int)LastBattleOutcome, // 전투 결과 기록
                isBossRound = IsBossRound // 보스 라운드 플래그 기록
            };

            foreach (var recipeId in FusionDiscovery.DiscoveredRecipeIds) data.discoveredRecipeIds.Add(recipeId); // 발견 합성식 기록
            foreach (var card in Hand.Hand) data.handCardIds.Add(card.PieceId); // 손패 기록
            foreach (var card in Deck.OwnedCardPool) data.ownedCardPoolIds.Add(card.PieceId); // 보유 풀 기록
            foreach (var card in Deck.DrawPile) data.drawPileIds.Add(card.PieceId); // 드로우 순서 기록
            foreach (var card in Deck.DeadCardPile) data.deadCardPileIds.Add(card.PieceId); // 죽은 카드 기록

            var savedPieces = new HashSet<PieceRuntimeState>(); // 대형 기물 중복 저장 방지

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var boardPosition = new Vector2Int(x, y); // 현재 검사 좌표 생성
                    var occupyingPiece = Board.GetTile(boardPosition).OccupyingPiece; // 현재 점유 기물 조회
                    if (occupyingPiece == null || !savedPieces.Add(occupyingPiece)) continue; // 빈 칸·중복 대형 기물 제외

                    var pieceSaveData = new PieceSaveData // 기물 저장 객체 생성
                    {
                        x = occupyingPiece.BoardPosition.x, // 기물 기준 X 기록
                        y = occupyingPiece.BoardPosition.y, // 기물 기준 Y 기록
                        pieceId = occupyingPiece.Definition.PieceId, // 기물 ID 기록
                        currentHp = occupyingPiece.CurrentHp, // 현재 체력 기록
                        isPlayerPiece = occupyingPiece.IsPlayerPiece, // 진영 기록
                        movementCycleIndex = occupyingPiece.MovementCycleIndex // Chameleon 순환 단계 기록
                    };

                    foreach (var statusEffect in occupyingPiece.StatusEffects) // 상태 이상 순회
                    {
                        pieceSaveData.statusEffects.Add(new StatusEffectSaveData // 상태 이상 저장 객체 추가
                        {
                            statusType = (int)statusEffect.Definition.StatusType, // 상태 종류 기록
                            remainingTurns = statusEffect.RemainingTurns, // 남은 턴 기록
                            stackCount = statusEffect.StackCount // 중첩 수 기록
                        });
                    }

                    data.boardPieces.Add(pieceSaveData); // 기물 저장 항목 추가
                }
            }

            return data; // 완성 저장 데이터 반환
        }

        public static RunState FromSaveData(RunSaveData data, PieceDatabase database, StatusEffectDatabase statusEffectDatabase = null) // 저장 데이터 기반 런 복원
        {
            if (data == null) return null; // 잘못된 저장 데이터 방어

            var runState = new RunState(data.kingHp) // 저장 킹 체력으로 런 생성
            {
                MetaCurrency = data.metaCurrency // 메타 재화 복원
            };

            int restoredRound = data.currentRound <= 0 ? RoundState.FirstRound : data.currentRound; // 구버전 라운드 기본값 보정
            RoundProgressStatus restoredStatus = ParseRoundStatus(data.roundStatus); // 저장 라운드 상태 검증
            BattleOutcome restoredOutcome = ParseBattleOutcome(data.battleOutcome); // 저장 전투 결과 검증
            runState.Round.Restore(restoredRound, restoredStatus, restoredOutcome); // 라운드 진행 상태 복원

            if (data.discoveredRecipeIds != null) runState.FusionDiscovery.Restore(data.discoveredRecipeIds); // 숨김 합성식 기록 복원

            if (data.handCardIds != null) // 손패 저장 데이터 확인
            {
                foreach (var pieceId in data.handCardIds) // 손패 복원
                {
                    var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                    if (definition != null) runState.Hand.TryAddCard(definition); // 손패 카드 추가
                }
            }

            if (data.ownedCardPoolIds != null) // 보유 카드 풀 저장 데이터 확인
            {
                foreach (var pieceId in data.ownedCardPoolIds) // 보유 풀 복원
                {
                    var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                    if (definition != null) runState.Deck.AddToOwnedPool(definition); // 보유 풀 카드 추가
                }
            }

            if (data.drawPileIds != null) // 구버전 저장 호환 드로우 데이터 확인
            {
                foreach (var pieceId in data.drawPileIds) // 드로우 순서 복원
                {
                    var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                    if (definition != null) runState.Deck.AddToDrawPile(definition); // 드로우 더미 추가
                }
            }

            if (data.deadCardPileIds != null) // 죽은 카드 저장 데이터 확인
            {
                foreach (var pieceId in data.deadCardPileIds) // 죽은 카드 복원
                {
                    var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                    if (definition != null) runState.Deck.MoveToDeadPile(definition); // 죽은 카드 더미 추가
                }
            }

            if (data.boardPieces != null) // 보드 저장 데이터 확인
            {
                foreach (var pieceData in data.boardPieces) // 보드 기물 복원
                {
                    if (pieceData == null) continue; // 잘못된 항목 제외

                    var definition = FindDefinition(database, pieceData.pieceId); // PieceDatabase·Resources 정의 조회
                    if (definition == null) continue; // 정의 누락 기물 제외

                    var boardPosition = new Vector2Int(pieceData.x, pieceData.y); // 저장 기준 좌표 생성
                    var anchorTile = runState.Board.GetTile(boardPosition); // 기준 좌표 타일 조회
                    if (anchorTile == null || anchorTile.OccupyingPiece != null) continue; // 범위 밖·구버전 대형 기물 중복 제외

                    var runtimePiece = new PieceRuntimeState(definition, boardPosition, pieceData.isPlayerPiece) // 런타임 기물 생성
                    {
                        CurrentHp = pieceData.currentHp // 현재 체력 복원
                    };

                    runtimePiece.RestoreMovementCycleIndex(pieceData.movementCycleIndex); // Chameleon 순환 단계 복원

                    if (statusEffectDatabase != null && pieceData.statusEffects != null) // 상태 이상 DB·저장 데이터 확인
                    {
                        foreach (var statusData in pieceData.statusEffects) // 상태 이상 순회
                        {
                            var statusDefinition = statusEffectDatabase.FindByType((StatusEffectType)statusData.statusType); // 상태 종류 정의 조회
                            if (statusDefinition != null) runtimePiece.RestoreStatusEffect(statusDefinition, statusData.remainingTurns, statusData.stackCount); // 지속 턴·중첩 복원
                        }
                    }

                    Vector2Int footprint = GetSafeFootprint(definition); // 안전한 점유 크기 계산

                    if (!runState.Board.TryOccupyArea(boardPosition, footprint, runtimePiece)) // 전체 점유 복원 시도
                    {
                        anchorTile.OccupyingPiece = runtimePiece; // 충돌 세이브 기준 칸 복원
                    }
                }
            }

            return runState; // 복원 런 반환
        }

        private static RoundProgressStatus ParseRoundStatus(int rawStatus) // 저장 라운드 상태 안전 변환
        {
            if (rawStatus < (int)RoundProgressStatus.NotStarted || rawStatus > (int)RoundProgressStatus.Failed) return RoundProgressStatus.NotStarted; // 범위 밖 상태 기본값
            return (RoundProgressStatus)rawStatus; // 정상 상태 변환
        }

        private static BattleOutcome ParseBattleOutcome(int rawOutcome) // 저장 전투 결과 안전 변환
        {
            if (rawOutcome < (int)BattleOutcome.None || rawOutcome > (int)BattleOutcome.Defeat) return BattleOutcome.None; // 범위 밖 결과 기본값
            return (BattleOutcome)rawOutcome; // 정상 결과 변환
        }

        private static PieceDefinition FindDefinition(PieceDatabase database, string pieceId) // DB·Resources 기물 정의 호환 조회
        {
            if (string.IsNullOrWhiteSpace(pieceId)) return null; // 빈 PieceId 제외

            var fromDatabase = database != null ? database.FindById(pieceId) : null; // PieceDatabase 우선 조회
            if (fromDatabase != null) return fromDatabase; // 등록 기물 반환

            var resourceDefinitions = Resources.LoadAll<PieceDefinition>(string.Empty); // Resources 독립 기물 전체 조회

            for (int i = 0; i < resourceDefinitions.Length; i++) // 리소스 기물 순회
            {
                var definition = resourceDefinitions[i]; // 현재 정의 조회
                if (definition == null) continue; // 빈 정의 제외
                if (string.Equals(definition.PieceId, pieceId, StringComparison.OrdinalIgnoreCase)) return definition; // 일치 기물 반환
            }

            return null; // 정의 없음 반환
        }

        private static Vector2Int GetSafeFootprint(PieceDefinition definition) // 구버전 점유 크기 안전 보정
        {
            if (definition == null) return Vector2Int.one; // 정의 누락 기본 1x1
            var size = definition.OccupancySize; // 저장 점유 크기 조회
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)); // 최소 1x1 보정
        }

        public int CountOwnedCopies(PieceDefinition definition) // 동일 기물 영구 보유 수 계산
        {
            if (definition == null) return 0; // 기준 정의 누락 처리

            int count = 0; // 누적 수 초기화
            foreach (var card in Deck.OwnedCardPool) if (card == definition) count++; // 정상 보유 풀 포함
            foreach (var card in Deck.DeadCardPile) if (card == definition) count++; // 사망 카드 소유권 포함
            return count; // 총 보유 수 반환
        }

        public int CountDeployedCopies(PieceDefinition definition) // 보드 위 동일 아군 기물 수 계산
        {
            if (definition == null) return 0; // 기준 정의 누락 처리

            var uniquePieces = new HashSet<PieceRuntimeState>(); // 대형 기물 중복 카운트 방지

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var occupyingPiece = Board.GetTile(new Vector2Int(x, y)).OccupyingPiece; // 현재 점유 기물 조회
                    if (occupyingPiece == null || !occupyingPiece.IsPlayerPiece) continue; // 빈 칸·적 제외
                    if (occupyingPiece.Definition == definition) uniquePieces.Add(occupyingPiece); // 동일 정의 런타임 한 번만 등록
                }
            }

            return uniquePieces.Count; // 실제 배치 기물 수 반환
        }
    }
}
