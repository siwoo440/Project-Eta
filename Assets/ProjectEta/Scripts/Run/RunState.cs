using System; // StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // HashSet<T>와 IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int와 Resources를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // DeckState, HandState를 사용하기 위한 네임스페이스
using ProjectEta.Fusion; // FusionDiscoveryLog를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceDatabase를 사용하기 위한 네임스페이스

namespace ProjectEta.Run // 런 관련 타입을 모아두는 네임스페이스
{
    public class RunState // 런 전체 상태를 담는 최상위 클래스
    {
        private int _kingHp; // 킹 체력 내부 필드

        public BoardState Board { get; } // 이 런이 사용하는 보드 상태
        public DeckState Deck { get; } // 이 런이 사용하는 덱 상태
        public HandState Hand { get; } // 이 런이 사용하는 손패 상태
        public FusionDiscoveryLog FusionDiscovery { get; } // 숨김 합성식 발견 기록
        public int CurrentRound { get; set; } // 현재 라운드 번호
        public int MetaCurrency { get; set; } // 보유 메타 재화
        public bool IsDefeated => _kingHp <= 0; // 킹 체력이 0 이하이면 패배

        public int KingHp // 킹 체력 프로퍼티
        {
            get => _kingHp; // 현재 킹 체력 반환
            set => _kingHp = value < 0 ? 0 : value; // 0 이상으로 제한
        }

        public RunState(int startingKingHp) // 런 상태 생성자
        {
            _kingHp = startingKingHp; // 시작 킹 체력 저장
            Board = new BoardState(); // 새 보드 생성
            Deck = new DeckState(); // 새 덱 생성
            Hand = new HandState(); // 새 손패 생성
            FusionDiscovery = new FusionDiscoveryLog(); // 발견 기록 생성
            CurrentRound = 1; // 첫 라운드로 초기화
        }

        public RunSaveData ToSaveData() // 현재 런을 저장 데이터로 변환
        {
            var data = new RunSaveData // 저장 객체 생성
            {
                kingHp = _kingHp, // 킹 체력 기록
                currentRound = CurrentRound, // 라운드 기록
                metaCurrency = MetaCurrency // 메타 재화 기록
            };

            foreach (var recipeId in FusionDiscovery.DiscoveredRecipeIds) data.discoveredRecipeIds.Add(recipeId); // 발견 합성식 기록
            foreach (var card in Hand.Hand) data.handCardIds.Add(card.PieceId); // 손패 기록
            foreach (var card in Deck.OwnedCardPool) data.ownedCardPoolIds.Add(card.PieceId); // 보유 풀 기록
            foreach (var card in Deck.DrawPile) data.drawPileIds.Add(card.PieceId); // 드로우 순서 기록
            foreach (var card in Deck.DeadCardPile) data.deadCardPileIds.Add(card.PieceId); // 죽은 카드 기록

            var savedPieces = new HashSet<PieceRuntimeState>(); // 2x2 보스처럼 여러 칸을 점유하는 같은 런타임 기물의 중복 저장 방지

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var boardPosition = new Vector2Int(x, y); // 현재 검사 좌표 생성
                    var occupyingPiece = Board.GetTile(boardPosition).OccupyingPiece; // 점유 기물 조회
                    if (occupyingPiece == null || !savedPieces.Add(occupyingPiece)) continue; // 빈 칸·이미 저장한 대형 기물 점유 칸 제외

                    var pieceSaveData = new PieceSaveData // 기물 스냅샷 생성
                    {
                        x = occupyingPiece.BoardPosition.x, // 현재 루프 칸이 아니라 기물 기준 좌표 기록
                        y = occupyingPiece.BoardPosition.y, // 현재 루프 칸이 아니라 기물 기준 좌표 기록
                        pieceId = occupyingPiece.Definition.PieceId, // 기물 id
                        currentHp = occupyingPiece.CurrentHp, // 현재 체력
                        isPlayerPiece = occupyingPiece.IsPlayerPiece, // 진영
                        movementCycleIndex = occupyingPiece.MovementCycleIndex // Chameleon 순환 단계
                    };

                    foreach (var statusEffect in occupyingPiece.StatusEffects) // 현재 상태 이상 순회
                    {
                        pieceSaveData.statusEffects.Add(new StatusEffectSaveData // 상태 이상 스냅샷 추가
                        {
                            statusType = (int)statusEffect.Definition.StatusType, // 상태 종류
                            remainingTurns = statusEffect.RemainingTurns, // 남은 지속 턴
                            stackCount = statusEffect.StackCount // 현재 중첩 수
                        });
                    }

                    data.boardPieces.Add(pieceSaveData); // 기물 한 개당 저장 항목 한 건 등록
                }
            }

            return data; // 완성된 저장 데이터 반환
        }

        public static RunState FromSaveData(RunSaveData data, PieceDatabase database, StatusEffectDatabase statusEffectDatabase = null) // 저장 데이터로 런 상태 복원
        {
            var runState = new RunState(data.kingHp) // 저장 킹 체력으로 런 생성
            {
                CurrentRound = data.currentRound, // 라운드 복원
                MetaCurrency = data.metaCurrency // 메타 재화 복원
            };

            runState.FusionDiscovery.Restore(data.discoveredRecipeIds); // 숨김 합성식 기록 복원

            foreach (var pieceId in data.handCardIds) // 손패 복원
            {
                var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                if (definition != null) runState.Hand.TryAddCard(definition); // 손패 추가
            }

            foreach (var pieceId in data.ownedCardPoolIds) // 보유 풀 복원
            {
                var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                if (definition != null) runState.Deck.AddToOwnedPool(definition); // 보유 풀 추가
            }

            if (data.drawPileIds != null) // 구버전 저장 호환
            {
                foreach (var pieceId in data.drawPileIds) // 드로우 순서 복원
                {
                    var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                    if (definition != null) runState.Deck.AddToDrawPile(definition); // 드로우 더미 추가
                }
            }

            foreach (var pieceId in data.deadCardPileIds) // 죽은 카드 복원
            {
                var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                if (definition != null) runState.Deck.MoveToDeadPile(definition); // 죽은 카드 더미 추가
            }

            if (data.boardPieces != null) // 보드 저장 데이터 존재 여부
            {
                foreach (var pieceData in data.boardPieces) // 보드 기물 복원
                {
                    if (pieceData == null) continue; // 잘못된 항목 제외

                    var definition = FindDefinition(database, pieceData.pieceId); // PieceDatabase 또는 Resources에서 정의 조회
                    if (definition == null) continue; // 정의를 찾지 못하면 건너뜀

                    var boardPosition = new Vector2Int(pieceData.x, pieceData.y); // 저장 기준 좌표 생성
                    var anchorTile = runState.Board.GetTile(boardPosition); // 기준 좌표 타일 조회
                    if (anchorTile == null || anchorTile.OccupyingPiece != null) continue; // 범위 밖 또는 구버전 2x2 중복 저장 항목 제외

                    var runtimePiece = new PieceRuntimeState(definition, boardPosition, pieceData.isPlayerPiece) // 런타임 기물 생성
                    {
                        CurrentHp = pieceData.currentHp // 저장 체력 복원
                    };

                    runtimePiece.RestoreMovementCycleIndex(pieceData.movementCycleIndex); // Chameleon 순환 단계 복원

                    if (statusEffectDatabase != null && pieceData.statusEffects != null) // 상태 이상 DB가 있을 때 상태 복원
                    {
                        foreach (var statusData in pieceData.statusEffects) // 저장 상태 이상 순회
                        {
                            var statusDefinition = statusEffectDatabase.FindByType((StatusEffectType)statusData.statusType); // 종류로 상태 정의 조회

                            if (statusDefinition != null) // 정의를 찾았으면
                            {
                                runtimePiece.RestoreStatusEffect(statusDefinition, statusData.remainingTurns, statusData.stackCount); // 지속 턴·중첩 복원
                            }
                        }
                    }

                    Vector2Int footprint = GetSafeFootprint(definition); // 1x1 또는 대형 점유 크기 계산

                    if (!runState.Board.TryOccupyArea(boardPosition, footprint, runtimePiece)) // 전체 점유 복원 시도
                    {
                        anchorTile.OccupyingPiece = runtimePiece; // 구버전·충돌 세이브는 기준 칸이라도 복원해 데이터 유실 방지
                    }
                }
            }

            return runState; // 복원된 런 반환
        }

        private static PieceDefinition FindDefinition(PieceDatabase database, string pieceId) // PieceDatabase에 없는 보스 Resources까지 포함해 기물 정의를 찾는 호환 조회
        {
            if (string.IsNullOrWhiteSpace(pieceId)) return null; // 빈 PieceId 제외

            var fromDatabase = database != null ? database.FindById(pieceId) : null; // 기존 PieceDatabase 우선 조회
            if (fromDatabase != null) return fromDatabase; // 기존 등록 기물 즉시 반환

            var resourceDefinitions = Resources.LoadAll<PieceDefinition>(string.Empty); // Resources에 있는 독립 PieceDefinition 전체 조회

            for (int i = 0; i < resourceDefinitions.Length; i++) // 리소스 기물 순회
            {
                var definition = resourceDefinitions[i]; // 현재 정의 조회
                if (definition == null) continue; // 빈 항목 제외

                if (string.Equals(definition.PieceId, pieceId, StringComparison.OrdinalIgnoreCase)) // PieceId 일치 여부
                {
                    return definition; // 보스 등 독립 Resources 기물 반환
                }
            }

            return null; // 정의 없음
        }

        private static Vector2Int GetSafeFootprint(PieceDefinition definition) // 잘못된 구버전 OccupancySize까지 안전하게 복구하는 점유 크기 계산
        {
            if (definition == null) return Vector2Int.one; // 정의가 없으면 1x1
            var size = definition.OccupancySize; // 저장된 점유 크기 읽기
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)); // 최소 1x1 보정
        }

        public int CountOwnedCopies(PieceDefinition definition) // 동일 기물의 영구 보유 수 계산
        {
            if (definition == null) return 0; // 기준이 없으면 0

            int count = 0; // 누적 수
            foreach (var card in Deck.OwnedCardPool) if (card == definition) count++; // 정상 보유 풀 포함
            foreach (var card in Deck.DeadCardPile) if (card == definition) count++; // 사망 카드도 소유권 유지
            return count; // 총 보유 수 반환
        }

        public int CountDeployedCopies(PieceDefinition definition) // 보드 위 동일 아군 기물 수 계산
        {
            if (definition == null) return 0; // 기준이 없으면 0

            var uniquePieces = new HashSet<PieceRuntimeState>(); // 대형 아군 기물의 다중 점유 중복 카운트 방지

            for (int x = 0; x < BoardState.Width; x++) // 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 세로 순회
                {
                    var occupyingPiece = Board.GetTile(new Vector2Int(x, y)).OccupyingPiece; // 점유 기물 조회
                    if (occupyingPiece == null || !occupyingPiece.IsPlayerPiece) continue; // 빈 칸·적 제외
                    if (occupyingPiece.Definition == definition) uniquePieces.Add(occupyingPiece); // 동일 정의 런타임을 한 번만 등록
                }
            }

            return uniquePieces.Count; // 실제 배치 기물 수 반환
        }
    }
}
