using System.Reflection; // BindingFlags를 사용하기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Vector2Int 등을 사용하기 위한 네임스페이스
using ProjectEta.Boss; // LargePieceVisualUtility를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceView를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day37LargeBossVisualTests // 37일차 수평형 보스 모델 외곽을 검증하는 테스트 모음
    {
        [Test] // 2x2 보스 적용 시 현재 절반 버전보다 1.5배 커진 수평형 외곽 모델이 생성되는지 검증
        public void ApplyFootprint_CreatesOnePointFiveTimesCurrentHorizontalBossShell()
        {
            var host = new GameObject("BossViewTest"); // 테스트용 루트 오브젝트 생성
            var pieceView = host.AddComponent<PieceView>(); // 실제 게임과 같은 PieceView 추가
            var definition = CreateDefinition(); // 2x2 프로토타입 보스 정의 생성
            var runtimeState = new PieceRuntimeState(definition, new Vector2Int(4, 7), false); // 적 보스 런타임 상태 생성

            pieceView.Initialize(runtimeState, 1f); // 기본 모델 생성
            LargePieceVisualUtility.ApplyFootprint(pieceView, 1f); // 0.75배 수평형 2x2 보스 외곽 적용

            var shell = pieceView.transform.Find("Model/LargeBossShell"); // 새로 생성된 수평형 외곽 루트 조회
            Assert.IsNotNull(shell); // 외곽 모델이 반드시 생성돼야 함
            Assert.GreaterOrEqual(shell.childCount, 10); // 여러 파츠로 구성된 넓은 차체형 외곽이어야 함

            var model = pieceView.transform.Find("Model"); // 기본 모델 루트 조회
            Assert.IsNotNull(model); // 모델 루트가 있어야 함
            Assert.Greater(model.localScale.x, model.localScale.y); // 수직보다 가로가 더 넓어야 함
            Assert.Greater(model.localScale.z, model.localScale.y); // 깊이 방향도 높이보다 더 넓어야 함
            Assert.Greater(model.localScale.x, 1.3f); // 절반 버전보다 커졌으므로 1.3 이상이어야 함
            Assert.Greater(model.localScale.z, 1.3f); // 절반 버전보다 커졌으므로 1.3 이상이어야 함
            Assert.Less(model.localScale.x, 1.5f); // 원본 큰 버전보다는 작아야 함
            Assert.Less(model.localScale.z, 1.5f); // 원본 큰 버전보다는 작아야 함

            var collider = pieceView.GetComponent<BoxCollider>(); // 최종 선택 콜라이더 조회
            Assert.IsNotNull(collider); // 선택 콜라이더가 유지돼야 함
            Assert.Greater(collider.size.x, 1.8f); // 절반 버전보다 넓어진 선택 범위여야 함
            Assert.Greater(collider.size.z, 1.8f); // 절반 버전보다 넓어진 선택 범위여야 함
            Assert.Less(collider.size.x, 2.2f); // 원본 큰 버전보다는 작은 선택 범위여야 함
            Assert.Less(collider.size.z, 2.2f); // 원본 큰 버전보다는 작은 선택 범위여야 함

            Object.DestroyImmediate(host); // 테스트 오브젝트 정리
        }

        private static PieceDefinition CreateDefinition() // 2x2 보스 테스트용 정의 생성 메서드
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 메모리 안에 임시 PieceDefinition 생성
            SetPrivateField(definition, "_pieceId", "prototype_boss_37"); // PieceId 설정
            SetPrivateField(definition, "_displayName", "2x2 프로토타입 보스"); // 표시 이름 설정
            SetPrivateField(definition, "_category", PieceCategory.Boss); // 보스 분류 설정
            SetPrivateField(definition, "_grade", PieceGrade.FiveStar); // 5성 설정
            SetPrivateField(definition, "_movementType", PieceMovementType.Custom); // 커스텀 이동 타입 설정
            SetPrivateField(definition, "_baseHp", 30); // 기본 HP 설정
            SetPrivateField(definition, "_baseAtk", 4); // 기본 ATK 설정
            SetPrivateField(definition, "_occupancySize", new Vector2Int(2, 2)); // 2x2 점유 크기 설정
            return definition; // 완성된 정의 반환
        }

        private static void SetPrivateField<T>(PieceDefinition target, string fieldName, T value) // private 직렬화 필드 주입 공통 도우미
        {
            var field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 필드 탐색
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조 변경 시 명확히 실패
            field.SetValue(target, value); // 테스트 값 주입
        }
    }
}
