using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class MovementResult // 이동 가능 칸과 공격 가능 칸을 구분해 담는 결과 클래스
    {
        public List<Vector2Int> MoveTiles { get; } = new List<Vector2Int>(); // 빈 칸이라 이동만 가능한 좌표 목록
        public List<Vector2Int> AttackTiles { get; } = new List<Vector2Int>(); // 적이 있어 공격 가능한 좌표 목록

        public void AddMove(Vector2Int position) // 이동 가능 좌표를 추가하는 메서드
        {
            if (!MoveTiles.Contains(position)) // 중복이 아니면
            {
                MoveTiles.Add(position); // 목록에 추가
            }
        }

        public void AddAttack(Vector2Int position) // 공격 가능 좌표를 추가하는 메서드
        {
            if (!AttackTiles.Contains(position)) // 중복이 아니면
            {
                AttackTiles.Add(position); // 목록에 추가
            }
        }

        public void MergeFrom(MovementResult other) // 다른 결과의 내용을 이 결과에 합치는 메서드(페어리 합성 이동용)
        {
            foreach (var move in other.MoveTiles) // 상대 결과의 이동 칸을 순회하며
            {
                AddMove(move); // 이동 목록에 합침
            }

            foreach (var attack in other.AttackTiles) // 상대 결과의 공격 칸을 순회하며
            {
                AddAttack(attack); // 공격 목록에 합침
            }
        }
    }
}
