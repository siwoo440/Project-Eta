using System; // [Serializable] 속성을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>와 IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, SerializeField, Vector2Int를 사용하기 위한 네임스페이스

namespace ProjectEta.Round // 라운드 구성·증원 관련 타입을 모아두는 네임스페이스
{
    [Serializable] // RoundDefinition 내부 목록에 Unity 직렬화될 수 있게 지정
    public class EnemySpawnDefinition // 적 기물 1기의 종류·좌표·등장 턴을 담는 순수 데이터
    {
        [SerializeField] private string _pieceId; // PlayerStartingDeck26 카탈로그에서 찾을 PieceId
        [SerializeField] private Vector2Int _position; // 보드에 배치할 목표 좌표
        [SerializeField] private int _spawnTurn; // 0은 시작 적, 1 이상은 해당 일반 턴부터 증원 가능

        public string PieceId => _pieceId; // 외부에서 읽는 기물 id
        public Vector2Int Position => _position; // 외부에서 읽는 배치 좌표
        public int SpawnTurn => _spawnTurn; // 외부에서 읽는 등장 턴

        public EnemySpawnDefinition(string pieceId, Vector2Int position, int spawnTurn) // 테스트·런타임에서 직접 데이터를 만들 수 있는 생성자
        {
            _pieceId = pieceId; // 기물 id 저장
            _position = position; // 배치 좌표 저장
            _spawnTurn = Mathf.Max(0, spawnTurn); // 음수 턴이 들어오지 않도록 0 이상으로 보정
        }

        public bool IsDue(int currentTurn) // 아직 처리되지 않은 증원이 현재 턴에 실행 가능한지 확인하는 메서드
        {
            return currentTurn >= _spawnTurn; // 지정 턴에 도달했거나 지났으면 실행 대상
        }
    }

    [CreateAssetMenu(fileName = "RoundDefinition", menuName = "ProjectEta/Round Definition")] // 에디터에서도 라운드 에셋을 만들 수 있게 메뉴 등록
    public class RoundDefinition : ScriptableObject // 한 라운드의 턴 제한·시작 적·증원 구성을 보관하는 데이터 에셋
    {
        [SerializeField] private string _displayName = "Round"; // 개발 로그와 디버그에서 표시할 라운드 이름
        [SerializeField] private int _turnLimit = 30; // 일반 라운드 기본 턴 제한
        [SerializeField] private bool _isBossRound; // 보스 라운드 여부
        [SerializeField] private List<EnemySpawnDefinition> _initialEnemies = new List<EnemySpawnDefinition>(); // 전투 시작 시 배치할 적 목록
        [SerializeField] private List<EnemySpawnDefinition> _reinforcements = new List<EnemySpawnDefinition>(); // 지정 턴에 등장할 증원 목록

        public string DisplayName => _displayName; // 외부에서 읽는 라운드 이름
        public int TurnLimit => Mathf.Max(1, _turnLimit); // 최소 1턴을 보장한 턴 제한
        public bool IsBossRound => _isBossRound; // 외부에서 읽는 보스 라운드 여부
        public IReadOnlyList<EnemySpawnDefinition> InitialEnemies => _initialEnemies; // 시작 적 목록을 읽기 전용으로 제공
        public IReadOnlyList<EnemySpawnDefinition> Reinforcements => _reinforcements; // 증원 목록을 읽기 전용으로 제공
    }
}
