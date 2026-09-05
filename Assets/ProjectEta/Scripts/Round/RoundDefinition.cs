using System; // Serializable 특성을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>와 IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, SerializeField, Vector2Int, Mathf를 사용하기 위한 네임스페이스

namespace ProjectEta.Round // 라운드 데이터 타입을 모아두는 네임스페이스
{
    [Serializable] // Unity 직렬화 대상 지정
    public class EnemySpawnDefinition // 초기 적과 증원 한 건의 스폰 데이터를 담는 타입
    {
        [SerializeField] private string _pieceId; // 생성할 기물 PieceId
        [SerializeField] private Vector2Int _position; // 생성할 보드 좌표
        [SerializeField] private int _spawnTurn; // 등장할 턴 번호

        public string PieceId => _pieceId; // 외부 PieceId 조회
        public Vector2Int Position => _position; // 외부 좌표 조회
        public int SpawnTurn => _spawnTurn; // 외부 등장 턴 조회

        public EnemySpawnDefinition(string pieceId, Vector2Int position, int spawnTurn) // 테스트와 런타임 데이터 생성용 생성자
        {
            _pieceId = pieceId; // PieceId 저장
            _position = position; // 좌표 저장
            _spawnTurn = Mathf.Max(0, spawnTurn); // 음수 턴 방지
        }

        public bool IsDue(int currentTurn) // 현재 턴에 이미 등장 시점이 도달했는지 확인
        {
            return currentTurn >= _spawnTurn; // 지정 턴 이상 여부 반환
        }
    }

    [CreateAssetMenu(fileName = "RoundDefinition", menuName = "ProjectEta/Round Definition")] // 에디터 생성 메뉴 등록
    public class RoundDefinition : ScriptableObject // 라운드의 적·증원·보스 구성을 담는 데이터 에셋
    {
        [SerializeField] private string _displayName = "Round"; // 라운드 표시 이름
        [SerializeField] private int _turnLimit = 30; // 라운드 턴 제한
        [SerializeField] private bool _isBossRound; // 보스 라운드 여부
        [SerializeField] private string _bossResourceName = "PrototypeBoss37"; // 보스 PieceDefinition Resources 이름
        [SerializeField] private Vector2Int _bossAnchor = new Vector2Int(0, 8); // 2x2 보스 기준 좌표
        [SerializeField] private List<EnemySpawnDefinition> _initialEnemies = new List<EnemySpawnDefinition>(); // 시작 적 목록
        [SerializeField] private List<EnemySpawnDefinition> _reinforcements = new List<EnemySpawnDefinition>(); // 턴별 증원 목록

        public string DisplayName => _displayName; // 표시 이름 공개
        public int TurnLimit => Mathf.Max(1, _turnLimit); // 최소 1턴 보정
        public bool IsBossRound => _isBossRound; // 보스 라운드 여부 공개
        public string BossResourceName => _bossResourceName; // 보스 Resources 이름 공개
        public Vector2Int BossAnchor => _bossAnchor; // 보스 기준 좌표 공개
        public bool HasBossConfiguration => _isBossRound && !string.IsNullOrWhiteSpace(_bossResourceName); // 실제 보스 생성 데이터 존재 여부
        public IReadOnlyList<EnemySpawnDefinition> InitialEnemies => _initialEnemies; // 시작 적 읽기 전용 공개
        public IReadOnlyList<EnemySpawnDefinition> Reinforcements => _reinforcements; // 증원 읽기 전용 공개
    }
}
