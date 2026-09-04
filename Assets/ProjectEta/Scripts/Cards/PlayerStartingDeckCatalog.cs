using System.Collections.Generic; // List<T>와 IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 SerializeField를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Cards // 카드·덱 관련 타입을 모아두는 네임스페이스
{
    [CreateAssetMenu(fileName = "PlayerStartingDeck26", menuName = "ProjectEta/Player Starting Deck Catalog")] // 에디터에서도 동일한 시작 덱 카탈로그를 만들 수 있게 등록
    public class PlayerStartingDeckCatalog : ScriptableObject // 플레이어 테스트 시작 덱에 넣을 PieceDefinition 목록을 보관하는 데이터 에셋
    {
        [SerializeField] private List<PieceDefinition> _cards = new List<PieceDefinition>(); // 시작 덱에 한 장씩 들어갈 카드 정의 목록

        public IReadOnlyList<PieceDefinition> Cards => _cards; // 외부에서는 목록을 읽기만 할 수 있도록 노출
    }
}
