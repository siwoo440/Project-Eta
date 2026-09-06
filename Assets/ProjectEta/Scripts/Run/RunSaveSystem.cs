using System; // Exception 사용
using System.IO; // 파일 읽기·쓰기·삭제 사용
using UnityEngine; // Application·JsonUtility·Resources 사용
using ProjectEta.Pieces; // PieceDatabase·StatusEffectDatabase 사용

namespace ProjectEta.Run
{
    public static class RunSaveSystem
    {
        private const string SaveFileName = "run_save.json"; // 기존 저장 파일 이름 유지

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName); // 실제 저장 경로 계산
        private static string TempPath => SavePath + ".tmp"; // 임시 안전 저장 경로 계산

        public static bool HasSave => File.Exists(SavePath); // 런 세이브 존재 여부
        public static bool CanContinue => TryGetContinueInfo(out _); // 메인 메뉴 이어하기 가능 여부

        public static void Save(RunState runState)
        {
            TrySave(runState); // 기존 호출부 호환 저장 진입점
        }

        public static bool TrySave(RunState runState)
        {
            if (runState == null) return false; // null 런 저장 차단

            try
            {
                RunSaveData data = runState.ToSaveData(); // 런 상태를 저장 DTO로 변환
                string json = JsonUtility.ToJson(data, true); // JSON 문자열 생성
                string directory = Path.GetDirectoryName(SavePath); // 저장 디렉터리 조회

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory); // 저장 디렉터리 생성 보장
                }

                File.WriteAllText(TempPath, json); // 임시 파일에 먼저 전체 JSON 기록
                File.Copy(TempPath, SavePath, true); // 임시 파일을 실제 세이브 경로에 덮어쓰기
                File.Delete(TempPath); // 정상 복사 후 임시 파일 정리
                return true; // 저장 성공 반환
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"51일차 런 저장 실패: {exception.Message}"); // 저장 오류 개발 로그 출력
                TryDeleteTemp(); // 실패한 임시 파일 정리
                return false; // 저장 실패 반환
            }
        }

        public static bool TryLoad(PieceDatabase database, out RunState runState, StatusEffectDatabase statusEffectDatabase = null)
        {
            runState = null; // 기본 불러오기 결과 초기화
            if (!TryReadData(out RunSaveData data)) return false; // JSON 저장 DTO 읽기 실패 처리

            try
            {
                runState = RunState.FromSaveData(data, database, statusEffectDatabase); // 저장 DTO 기반 런 복원
                return runState != null; // 실제 복원 성공 여부 반환
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"51일차 런 복원 실패: {exception.Message}"); // 런 객체 복원 오류 출력
                runState = null; // 부분 복원 객체 노출 차단
                return false; // 복원 실패 반환
            }
        }

        public static bool TryLoadSafe(out RunState runState)
        {
            runState = null; // 기본 자동 복원 결과 초기화
            if (!TryReadData(out RunSaveData data)) return false; // 저장 DTO 읽기 실패 처리
            if (!IsContinueDataValid(data)) return false; // MainMenu와 동일한 안전 세이브 규칙 적용

            PieceDatabase pieceDatabase = LoadFirstResource<PieceDatabase>(); // Resources PieceDatabase 자동 탐색
            StatusEffectDatabase statusEffectDatabase = LoadFirstResource<StatusEffectDatabase>(); // Resources 상태 이상 DB 자동 탐색

            try
            {
                RunState candidate = RunState.FromSaveData(data, pieceDatabase, statusEffectDatabase); // 저장 DTO 기반 자동 복원 후보 생성

                if (!IsSafeCheckpoint(candidate))
                {
                    if (candidate != null && candidate.Flow.IsRunFinished) DeleteSave(); // 종료 세이브는 다음 런을 위해 제거
                    return false; // 전투 중·전환 중 세이브 자동 복원 차단
                }

                runState = candidate; // 안전 지점 런 복원 확정
                return true; // 자동 복원 성공 반환
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"51일차 안전 지점 복원 실패: {exception.Message}"); // 자동 복원 오류 출력
                return false; // 신규 런 fallback 허용
            }
        }

        public static bool TryGetContinueInfo(out RunContinueInfo info)
        {
            info = null; // 기본 이어하기 요약 초기화
            if (!TryReadData(out RunSaveData data)) return false; // 실제 저장 데이터 읽기 실패 처리
            return TryCreateContinueInfo(data, out info); // 안전 검증 후 메뉴 요약 생성
        }

        public static bool TryCreateContinueInfo(RunSaveData data, out RunContinueInfo info)
        {
            info = null; // 기본 이어하기 요약 초기화
            if (!IsContinueDataValid(data)) return false; // 안전 지점 저장 데이터만 메뉴 노출 허용

            RunFlowPhase phase = ParseFlowPhase(data.flowPhase); // 저장 상위 흐름 변환
            int visitedNodeCount = data.routeMap.visitedNodeIds != null ? data.routeMap.visitedNodeIds.Count : 0; // 방문 경로 수 계산
            info = new RunContinueInfo(data.currentRound, phase, data.runCurrency, data.kingHp, visitedNodeCount); // MainMenu 표시용 요약 생성
            return true; // 이어하기 요약 생성 성공 반환
        }

        public static bool IsContinueDataValid(RunSaveData data)
        {
            if (data == null) return false; // 저장 DTO 누락 차단
            if (data.saveVersion != RunSaveData.CurrentVersion) return false; // 현재 포맷 외 자동 이어하기 차단
            if (string.IsNullOrWhiteSpace(data.runId)) return false; // 런 고유 ID 누락 차단
            if (data.currentRound < RoundState.FirstRound || data.currentRound > RoundState.FinalRound) return false; // 1~10 범위 밖 스테이지 차단
            if (data.routeMap == null || data.routeMap.nodes == null || data.routeMap.nodes.Count == 0) return false; // 지도 스냅샷 누락 차단
            if (string.IsNullOrWhiteSpace(data.routeMap.currentNodeId)) return false; // 현재 지도 노드 누락 차단

            RouteNodeSaveData currentNode = FindRouteNode(data.routeMap, data.routeMap.currentNodeId); // 현재 노드 저장 항목 조회
            if (currentNode == null) return false; // 현재 노드가 전체 지도에 없으면 차단
            if (currentNode.depth != data.routeMap.currentDepth) return false; // 현재 노드 깊이와 지도 깊이 불일치 차단
            if (data.currentRound != data.routeMap.currentDepth) return false; // RunState 스테이지와 지도 깊이 불일치 차단

            RunFlowPhase phase = ParseFlowPhase(data.flowPhase); // 저장 상위 흐름 검증
            if (phase == RunFlowPhase.Map)
            {
                return string.IsNullOrWhiteSpace(data.routeMap.selectedNodeId); // 선택 전 Map 안전 지점만 허용
            }

            if (phase == RunFlowPhase.Reward || phase == RunFlowPhase.Shop || phase == RunFlowPhase.Event)
            {
                if (string.IsNullOrWhiteSpace(data.routeMap.selectedNodeId)) return false; // 비전투 진입 노드 누락 차단
                if (!string.Equals(data.routeMap.currentNodeId, data.routeMap.selectedNodeId, StringComparison.Ordinal)) return false; // 선택 노드와 King 현재 노드 불일치 차단
                return FindRouteNode(data.routeMap, data.routeMap.selectedNodeId) != null; // 선택 노드가 지도에 존재하는 경우만 허용
            }

            return false; // Battle·Completed·Failed 자동 이어하기 차단
        }

        public static bool IsSafeCheckpoint(RunState runState)
        {
            if (runState == null || runState.Flow.IsRunFinished) return false; // null·종료 런 저장 차단

            if (runState.CurrentFlowPhase == RunFlowPhase.Map)
            {
                return runState.RouteMap.HasPreparedRoute && !runState.RouteMap.HasSelectedNode; // 다음 노드 선택 전 지도만 안전 저장
            }

            if (runState.CurrentFlowPhase == RunFlowPhase.Reward)
            {
                return runState.RouteMap.HasSelectedNode; // Reward 노드 진입만 자동 복원 가능한 안전 지점
            }

            return runState.CurrentFlowPhase == RunFlowPhase.Shop || runState.CurrentFlowPhase == RunFlowPhase.Event; // 상점·이벤트 선택 화면 안전 저장
        }

        public static bool TryDeleteForNewRun()
        {
            if (!File.Exists(SavePath) && !File.Exists(TempPath)) return true; // 기존 런 파일이 없으면 즉시 새 게임 허용

            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath); // 이전 런 세이브 제거
                if (File.Exists(TempPath)) File.Delete(TempPath); // 남은 임시 런 세이브 제거
                return !File.Exists(SavePath) && !File.Exists(TempPath); // 실제 정리 완료 여부 반환
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"54일차 새 게임 Run Save 정리 실패: {exception.Message}"); // 새 게임 정리 오류 출력
                return false; // 이전 런 잔존 시 새 게임 차단
            }
        }

        public static bool DeleteSave()
        {
            try
            {
                bool deleted = false; // 실제 삭제 여부 초기화

                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath); // 현재 런 세이브 삭제
                    deleted = true; // 삭제 결과 기록
                }

                if (File.Exists(TempPath))
                {
                    File.Delete(TempPath); // 남은 임시 저장 파일 삭제
                }

                return deleted; // 실제 세이브 삭제 여부 반환
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"51일차 런 세이브 삭제 실패: {exception.Message}"); // 삭제 오류 출력
                return false; // 삭제 실패 반환
            }
        }

        private static bool TryReadData(out RunSaveData data)
        {
            data = null; // 기본 DTO 결과 초기화
            if (!File.Exists(SavePath)) return false; // 저장 파일 없음 처리

            try
            {
                string json = File.ReadAllText(SavePath); // 저장 파일 JSON 읽기
                if (string.IsNullOrWhiteSpace(json)) return false; // 빈 저장 파일 차단

                data = JsonUtility.FromJson<RunSaveData>(json); // JSON 저장 DTO 역직렬화
                return data != null; // DTO 생성 성공 여부 반환
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"51일차 런 세이브 읽기 실패: {exception.Message}"); // 손상·읽기 오류 출력
                return false; // 읽기 실패 반환
            }
        }

        private static RunFlowPhase ParseFlowPhase(int rawPhase)
        {
            if (rawPhase < (int)RunFlowPhase.Battle || rawPhase > (int)RunFlowPhase.Failed) return RunFlowPhase.Battle; // 범위 밖 저장값 Battle fallback
            return (RunFlowPhase)rawPhase; // 정상 저장 흐름 반환
        }

        private static RouteNodeSaveData FindRouteNode(RouteMapSaveData routeMap, string nodeId)
        {
            if (routeMap == null || routeMap.nodes == null || string.IsNullOrWhiteSpace(nodeId)) return null; // 잘못된 지도 조회 차단

            for (int i = 0; i < routeMap.nodes.Count; i++)
            {
                RouteNodeSaveData node = routeMap.nodes[i]; // 현재 저장 노드 조회
                if (node == null) continue; // 빈 저장 노드 제외
                if (string.Equals(node.nodeId, nodeId, StringComparison.Ordinal)) return node; // 동일 노드 ID 반환
            }

            return null; // 일치 저장 노드 없음
        }

        private static T LoadFirstResource<T>() where T : UnityEngine.Object
        {
            T[] resources = Resources.LoadAll<T>(string.Empty); // Resources 전체에서 대상 데이터 탐색
            return resources != null && resources.Length > 0 ? resources[0] : null; // 첫 데이터 또는 null 반환
        }

        private static void TryDeleteTemp()
        {
            try
            {
                if (File.Exists(TempPath)) File.Delete(TempPath); // 실패한 임시 저장 파일 제거
            }
            catch
            {
            }
        }
    }
}
