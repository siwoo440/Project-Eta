using System.Collections.Generic; // List<T>와 IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Material, Shader, Vector3 등을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 2x2 이상 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public sealed class BossTelegraphOverlay : MonoBehaviour // 실제 공격 TargetCells와 동일한 칸에 낮은 경고 타일을 생성하는 런타임 텔레그래프 표시기
    {
        private readonly List<GameObject> _markers = new List<GameObject>(); // 현재 화면에 생성된 위험 칸 표시 오브젝트 목록
        private Material _warningMaterial; // 모든 위험 칸이 공유하는 간단한 런타임 머티리얼

        public int VisibleCellCount => _markers.Count; // 테스트·디버그에서 현재 표시 중인 위험 칸 수를 읽는 프로퍼티

        public void Show(BoardView boardView, IReadOnlyList<Vector2Int> targetCells, BossPatternType patternType) // 위험 칸 목록을 실제 보드 위에 표시하는 메서드
        {
            Clear(); // 이전 예고 표시를 먼저 제거
            if (boardView == null || targetCells == null) return; // 보드 또는 대상 칸이 없으면 표시하지 않음

            _warningMaterial = CreateWarningMaterial(patternType); // 패턴별 경고 색의 공유 머티리얼 생성
            float tileSize = boardView.TileSize; // 현재 보드 칸 크기 읽기

            for (int i = 0; i < targetCells.Count; i++) // 모든 예고 칸 순회
            {
                Vector2Int cell = targetCells[i]; // 현재 위험 칸 좌표
                if (boardView.State == null || !boardView.State.IsInsideBoard(cell)) continue; // 보드 밖 좌표는 표시하지 않음

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube); // 얇은 큐브를 이용해 바닥 경고 타일 생성
                marker.name = $"BossTelegraph_{patternType}_{cell.x}_{cell.y}"; // Hierarchy에서 좌표와 패턴을 바로 확인할 수 있는 이름 지정
                marker.transform.SetParent(boardView.transform, false); // 보드가 움직여도 함께 따라가도록 BoardView 자식으로 연결
                marker.transform.localPosition = BoardView.BoardToLocalPosition(cell, tileSize) + new Vector3(0f, 0.035f, 0f); // 체스판 바로 위에 살짝 띄워 Z-fighting 방지
                marker.transform.localScale = new Vector3(tileSize * 0.88f, 0.035f, tileSize * 0.88f); // 타일 경계가 보이도록 한 칸보다 약간 작은 크기

                var renderer = marker.GetComponent<Renderer>(); // 생성된 큐브 렌더러 조회
                if (renderer != null && _warningMaterial != null) renderer.sharedMaterial = _warningMaterial; // 패턴 경고 머티리얼 적용

                var collider = marker.GetComponent<Collider>(); // 기본 큐브 콜라이더 조회
                if (collider != null) DestroyUnityObject(collider); // 경고 표시가 보드 클릭을 물리적으로 가로채지 않게 제거

                _markers.Add(marker); // 이후 한 번에 지울 수 있도록 목록에 보관
            }
        }

        public void Clear() // 현재 텔레그래프 표시와 런타임 머티리얼을 모두 제거하는 메서드
        {
            for (int i = 0; i < _markers.Count; i++) // 생성된 모든 경고 타일 순회
            {
                if (_markers[i] != null) DestroyUnityObject(_markers[i]); // Play/EditMode에 맞게 제거
            }

            _markers.Clear(); // 내부 표시 목록 비움

            if (_warningMaterial != null) // 생성했던 공유 머티리얼이 있으면
            {
                DestroyUnityObject(_warningMaterial); // 다음 패턴에서 새 색을 만들 수 있도록 제거
                _warningMaterial = null; // 참조 초기화
            }
        }

        private static Material CreateWarningMaterial(BossPatternType patternType) // URP를 우선 사용해 단순 경고 색 머티리얼을 만드는 메서드
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit"); // Unity 6 URP 프로젝트의 Unlit 셰이더 우선 탐색
            if (shader == null) shader = Shader.Find("Unlit/Color"); // URP 셰이더를 찾지 못하면 기본 Unlit 폴백
            if (shader == null) shader = Shader.Find("Standard"); // 마지막으로 Standard 폴백
            if (shader == null) return null; // 사용할 셰이더가 전혀 없으면 표시 머티리얼 없이 종료

            var material = new Material(shader); // 런타임 경고 전용 머티리얼 생성
            Color color = patternType == BossPatternType.KingLane // 패턴 종류에 따라 다른 색을 사용해 형태뿐 아니라 색으로도 구분
                ? new Color(1f, 0.62f, 0.05f, 1f) // King 직선은 주황색
                : new Color(0.95f, 0.08f, 0.08f, 1f); // 주변 강타는 붉은색

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color); // URP Unlit 색상 프로퍼티 설정
            if (material.HasProperty("_Color")) material.SetColor("_Color", color); // Standard/Unlit Color 호환 설정
            return material; // 완성 머티리얼 반환
        }

        private static void DestroyUnityObject(Object target) // Play/EditMode에 맞춰 Unity 오브젝트를 안전하게 제거하는 공통 메서드
        {
            if (target == null) return; // 이미 제거됐으면 종료
            if (Application.isPlaying) Destroy(target); // Play Mode에서는 프레임 종료 시 제거
            else DestroyImmediate(target); // EditMode에서는 즉시 제거
        }

        private void OnDestroy() // 호스트가 제거될 때 남아 있는 경고 오브젝트 정리
        {
            Clear(); // 런타임 생성 오브젝트와 머티리얼 누수 방지
        }
    }
}
