using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Abilities; // 90일차 RedirectDamage 보드 탐색 등록
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class BoardState // 10x10 보드 전체 상태를 담는 클래스
    {
        public const int Width = 10; // 보드 가로 칸 수
        public const int Height = 10; // 보드 세로 칸 수

        private readonly TileState[,] _tiles = new TileState[Width, Height]; // 칸별 상태를 담는 2차원 배열

        public BoardState() // 보드 상태 생성자
        {
            for (int x = 0; x < Width; x++) // 가로 방향으로 순회
            {
                for (int y = 0; y < Height; y++) // 세로 방향으로 순회
                {
                    var position = new Vector2Int(x, y); // 현재 칸 좌표 생성
                    _tiles[x, y] = new TileState(position) // 좌표로 타일 상태 생성
                    {
                        IsPlayerPlacementArea = y < Height / 2, // 아래쪽 절반은 아군 배치 영역
                        IsEnemyPlacementArea = y >= Height / 2 // 위쪽 절반은 적군 배치 영역
                    };
                }
            }

            AbilityBoardRegistry.Register(this); // 90일차: 피해 대상이 속한 실제 보드를 RedirectDamage가 찾을 수 있도록 등록
        }

        public bool IsInsideBoard(Vector2Int position)
        {
            return position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height;
        }

        public TileState GetTile(Vector2Int position)
        {
            return IsInsideBoard(position) ? _tiles[position.x, position.y] : null;
        }

        public bool CanOccupyArea(Vector2Int anchor, Vector2Int size, PieceRuntimeState ignorePiece = null)
        {
            if (size.x <= 0 || size.y <= 0) return false;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var position = anchor + new Vector2Int(x, y);
                    if (!IsInsideBoard(position)) return false;

                    var tile = GetTile(position);
                    if (tile == null || tile.IsBlockedByObstacle) return false;

                    if (tile.OccupyingPiece != null && tile.OccupyingPiece != ignorePiece) return false;
                }
            }

            return true;
        }

        public bool TryOccupyArea(Vector2Int anchor, Vector2Int size, PieceRuntimeState piece)
        {
            if (piece == null) return false;
            if (!CanOccupyArea(anchor, size, piece)) return false;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var position = anchor + new Vector2Int(x, y);
                    GetTile(position).OccupyingPiece = piece;
                }
            }

            return true;
        }

        public void ClearArea(Vector2Int anchor, Vector2Int size)
        {
            if (size.x <= 0 || size.y <= 0) return;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var tile = GetTile(anchor + new Vector2Int(x, y));
                    if (tile != null) tile.OccupyingPiece = null;
                }
            }
        }

        public int ClearPiece(PieceRuntimeState piece)
        {
            if (piece == null) return 0;

            int clearedCount = 0;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_tiles[x, y].OccupyingPiece != piece) continue;
                    _tiles[x, y].OccupyingPiece = null;
                    clearedCount++;
                }
            }

            return clearedCount;
        }

        public int CountPieces(bool isPlayerPiece)
        {
            var uniquePieces = new HashSet<PieceRuntimeState>();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var piece = _tiles[x, y].OccupyingPiece;
                    if (piece == null) continue;

                    bool hasConfiguredHp = piece.Definition != null && piece.Definition.BaseHp > 0;
                    if (piece.IsDead && hasConfiguredHp) continue;

                    if (piece.IsPlayerPiece != isPlayerPiece) continue;
                    uniquePieces.Add(piece);
                }
            }

            return uniquePieces.Count;
        }
    }
}
