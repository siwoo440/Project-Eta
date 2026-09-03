using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // DeckState, HandState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceDatabase를 사용하기 위한 네임스페이스

namespace ProjectEta.Run // 런(플레이 세션) 관련 타입을 모아두는 네임스페이스
{
    public class RunState // 런 전체 상태(보드·덱·손패·킹 체력 등)를 담는 최상위 클래스
    {
        private int _kingHp; // 킹 체력을 저장하는 내부 필드

        public BoardState Board { get; } // 이 런이 사용하는 보드 상태
        public DeckState Deck { get; } // 이 런이 사용하는 덱 상태
        public HandState Hand { get; } // 이 런이 사용하는 손패 상태
        public int CurrentRound { get; set; } // 현재 라운드 번호
        public int MetaCurrency { get; set; } // 보유한 메타 재화
        public bool IsDefeated => _kingHp <= 0; // 킹 체력이 0 이하이면 패배로 판정

        public int KingHp // 킹 체력 프로퍼티
        {
            get => _kingHp; // 현재 킹 체력 반환
            set => _kingHp = value < 0 ? 0 : value; // 음수로 내려가지 않도록 0 이상으로 제한해 저장
        }

        public RunState(int startingKingHp) // 런 상태 생성자
        {
            _kingHp = startingKingHp; // 시작 킹 체력 저장
            Board = new BoardState(); // 새 보드 상태 생성
            Deck = new DeckState(); // 새 덱 상태 생성
            Hand = new HandState(); // 새 손패 상태 생성
            CurrentRound = 1; // 첫 라운드로 초기화
        }

        public RunSaveData ToSaveData() // 현재 런 상태를 저장용 데이터로 변환하는 메서드
        {
            var data = new RunSaveData // 저장용 데이터 생성
            {
                kingHp = _kingHp, // 킹 체력 기록
                currentRound = CurrentRound, // 현재 라운드 기록
                metaCurrency = MetaCurrency // 메타 재화 기록
            };

            foreach (var card in Hand.Hand) // 손패의 카드를 순회하며
            {
                data.handCardIds.Add(card.PieceId); // PieceId를 저장 목록에 추가
            }

            foreach (var card in Deck.OwnedCardPool) // 보유 카드 풀을 순회하며
            {
                data.ownedCardPoolIds.Add(card.PieceId); // PieceId를 저장 목록에 추가
            }

            foreach (var card in Deck.DrawPile) // 16일차: 현재 드로우 순서를 순회하며
            {
                data.drawPileIds.Add(card.PieceId); // 드로우 순서를 그대로 PieceId 목록에 저장
            }

            foreach (var card in Deck.DeadCardPile) // 죽은 카드 더미를 순회하며
            {
                data.deadCardPileIds.Add(card.PieceId); // PieceId를 저장 목록에 추가
            }

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 방향으로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 방향으로 순회
                {
                    var boardPosition = new Vector2Int(x, y); // 현재 칸 좌표 생성
                    var occupyingPiece = Board.GetTile(boardPosition).OccupyingPiece; // 해당 칸의 점유 기물 조회
                    if (occupyingPiece == null) // 기물이 없으면
                    {
                        continue; // 이 칸은 건너뜀
                    }

                    data.boardPieces.Add(new PieceSaveData // 기물 스냅샷을 저장 목록에 추가
                    {
                        x = x, // 가로 좌표 기록
                        y = y, // 세로 좌표 기록
                        pieceId = occupyingPiece.Definition.PieceId, // 기물 종류 기록
                        currentHp = occupyingPiece.CurrentHp, // 현재 체력 기록
                        isPlayerPiece = occupyingPiece.IsPlayerPiece // 아군 여부 기록
                    });
                }
            }

            return data; // 완성된 저장용 데이터 반환
        }

        public static RunState FromSaveData(RunSaveData data, PieceDatabase database) // 저장용 데이터로부터 런 상태를 복원하는 정적 메서드
        {
            var runState = new RunState(data.kingHp) // 저장된 킹 체력으로 새 런 상태 생성
            {
                CurrentRound = data.currentRound, // 저장된 라운드 복원
                MetaCurrency = data.metaCurrency // 저장된 메타 재화 복원
            };

            foreach (var pieceId in data.handCardIds) // 저장된 손패 id 목록을 순회하며
            {
                var definition = database.FindById(pieceId); // id로 기물 정의 조회
                if (definition != null) // 정의를 찾았으면
                {
                    runState.Hand.TryAddCard(definition); // 손패에 카드 추가
                }
            }

            foreach (var pieceId in data.ownedCardPoolIds) // 저장된 보유 카드 풀 id 목록을 순회하며
            {
                var definition = database.FindById(pieceId); // id로 기물 정의 조회
                if (definition != null) // 정의를 찾았으면
                {
                    runState.Deck.AddToOwnedPool(definition); // 보유 카드 풀에 추가
                }
            }

            if (data.drawPileIds != null) // 16일차 이전 저장 파일처럼 드로우 목록이 없는 경우도 안전하게 처리
            {
                foreach (var pieceId in data.drawPileIds) // 저장된 드로우 순서를 처음부터 순회하며
                {
                    var definition = database.FindById(pieceId); // id로 기물 정의 조회
                    if (definition != null) // 정의를 찾았으면
                    {
                        runState.Deck.AddToDrawPile(definition); // 저장된 순서대로 드로우 더미 복원
                    }
                }
            }

            foreach (var pieceId in data.deadCardPileIds) // 저장된 죽은 카드 더미 id 목록을 순회하며
            {
                var definition = database.FindById(pieceId); // id로 기물 정의 조회
                if (definition != null) // 정의를 찾았으면
                {
                    runState.Deck.MoveToDeadPile(definition); // 죽은 카드 더미에 추가
                }
            }

            foreach (var pieceData in data.boardPieces) // 저장된 보드 기물 목록을 순회하며
            {
                var definition = database.FindById(pieceData.pieceId); // id로 기물 정의 조회
                if (definition == null) // 정의를 못 찾았으면
                {
                    continue; // 이 기물은 건너뜀
                }

                var boardPosition = new Vector2Int(pieceData.x, pieceData.y); // 저장된 좌표로 Vector2Int 생성
                var runtimePiece = new PieceRuntimeState(definition, boardPosition, pieceData.isPlayerPiece) // 기물 런타임 상태 복원
                {
                    CurrentHp = pieceData.currentHp // 저장된 현재 체력 복원
                };

                runState.Board.GetTile(boardPosition).OccupyingPiece = runtimePiece; // 보드 해당 칸에 기물 배치
            }

            return runState; // 복원된 런 상태 반환
        }
    }
}
