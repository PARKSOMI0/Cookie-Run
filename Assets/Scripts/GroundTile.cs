using System;
using UnityEngine;

/// <summary>
/// 쿠키런 스타일 러닝게임의 개별 그라운드 타일.
/// Inspector에서 Y좌표별로 스폰할 프리팹을 설정할 수 있다.
/// TileManager가 이 타일을 생성할 때 SpawnObjects()를 호출하여
/// 각 Y레인에 프리팹을 랜덤/규칙적으로 배치한다.
/// </summary>
public class GroundTile : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Y좌표(레인)별 프리팹 할당 구조체
    // ──────────────────────────────────────────────

    [Serializable]
    public class LanePrefabEntry
    {
        [Tooltip("이 프리팹이 스폰될 Y좌표 (레인 높이)")]
        public float yPosition;

        [Tooltip("이 레인에 스폰할 프리팹 목록 (랜덤 선택)")]
        public GameObject[] prefabs;

        [Tooltip("이 레인에 프리팹이 스폰될 확률 (0~1)")]
        [Range(0f, 1f)]
        public float spawnChance = 0.5f;
    }

    [Header("=== 레인(Y좌표)별 프리팹 설정 ===")]
    [Tooltip("Y좌표마다 스폰할 프리팹과 확률을 설정합니다.")]
    [SerializeField] private LanePrefabEntry[] lanePrefabs;

    [Header("=== 타일 기본 설정 ===")]
    [Tooltip("타일의 가로 길이 (TileManager에서 다음 타일 위치를 계산할 때 사용)")]
    [SerializeField] private float tileLength = 20f;

    [Tooltip("프리팹 스폰 시 X축 오프셋 범위 (타일 내 랜덤 위치)")]
    [SerializeField] private float spawnXMin = 2f;
    [SerializeField] private float spawnXMax = 18f;

    // 스폰된 오브젝트들을 추적 (타일 재활용 시 정리용)
    private System.Collections.Generic.List<GameObject> spawnedObjects = new();

    /// <summary>
    /// 타일의 가로 길이
    /// </summary>
    public float TileLength => tileLength;

    // ──────────────────────────────────────────────
    //  공개 메서드
    // ──────────────────────────────────────────────

    /// <summary>
    /// TileManager가 호출. 레인별 프리팹을 타일 위에 스폰한다.
    /// </summary>
    public void SpawnObjects()
    {
        if (lanePrefabs == null || lanePrefabs.Length == 0) return;

        foreach (var lane in lanePrefabs)
        {
            if (lane.prefabs == null || lane.prefabs.Length == 0) continue;

            // 확률 체크
            if (UnityEngine.Random.value > lane.spawnChance) continue;

            // 랜덤 프리팹 선택
            GameObject prefab = lane.prefabs[UnityEngine.Random.Range(0, lane.prefabs.Length)];
            if (prefab == null) continue;

            // 타일 내 랜덤 X 위치
            float xOffset = UnityEngine.Random.Range(spawnXMin, spawnXMax);

            // 스폰 위치: 타일의 월드 위치 + (X오프셋, Y레인 높이, 0)
            Vector3 spawnPos = transform.position + new Vector3(xOffset, lane.yPosition, 0f);

            GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            spawnedObjects.Add(spawned);
        }
    }

    /// <summary>
    /// 타일 재활용 시 기존 스폰 오브젝트 정리
    /// </summary>
    public void ClearSpawnedObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    /// <summary>
    /// 타일이 삭제될 때 스폰된 오브젝트도 정리
    /// </summary>
    private void OnDestroy()
    {
        ClearSpawnedObjects();
    }
}
