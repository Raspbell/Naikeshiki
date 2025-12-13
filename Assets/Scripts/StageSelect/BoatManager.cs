using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class BoatManager : MonoBehaviour
{
    [SerializeField] private GameObject boatPrefab;
    [SerializeField] private Collider spawnArea;
    [SerializeField] private Quaternion boatDirection;
    [SerializeField] private int maxTryCount = 30;
    [SerializeField] private int maxSpawnCount = 2;
    [SerializeField] private float firstBoatInterval = 1f;
    [SerializeField] private float minSpawnInterval = 2f;
    [SerializeField] private float maxSpawnInterval = 5f;
    [SerializeField] private bool autoSpawnOnStart = true;

    [SerializeField] private float viewportMargin = 0.05f;

    private readonly List<GameObject> spawnedBoats = new();

    private void Start()
    {
        if (!autoSpawnOnStart)
        {
            return;
        }
        SpawnLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid SpawnLoopAsync(System.Threading.CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            for (int i = spawnedBoats.Count - 1; i >= 0; i--)
            {
                if (spawnedBoats[i] == null)
                {
                    spawnedBoats.RemoveAt(i);
                }
            }

            if (spawnedBoats.Count < maxSpawnCount)
            {
                GameObject newBoat = SpawnOne(boatPrefab, Camera.main);
                if (newBoat != null)
                {
                    newBoat.transform.rotation = boatDirection;
                    spawnedBoats.Add(newBoat);
                }
            }

            float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
            await UniTask.Delay(System.TimeSpan.FromSeconds(wait), cancellationToken: ct);
        }
    }

    public GameObject SpawnOne(GameObject prefab, Camera cam)
    {
        if (spawnArea == null || prefab == null)
        {
            return null;
        }

        BoxCollider box = spawnArea as BoxCollider;
        if (box == null)
        {
            return null;
        }

        for (int i = 0; i < maxTryCount; i++)
        {
            Vector3 p = GetRandomPointInsideBoxCollider(box);

            if (IsPointVisibleInCamera(cam, p, viewportMargin))
            {
                continue;
            }

            return Instantiate(prefab, p, Quaternion.identity);
        }

        return null;
    }

    private static Vector3 GetRandomPointInsideBoxCollider(BoxCollider box)
    {
        Vector3 half = box.size * 0.5f;

        Vector3 local = box.center + new Vector3(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z)
        );

        return box.transform.TransformPoint(local);
    }

    private static bool IsPointVisibleInCamera(Camera cam, Vector3 worldPoint, float margin)
    {
        if (cam == null)
        {
            return false;
        }

        Vector3 vp = cam.WorldToViewportPoint(worldPoint);

        if (vp.z <= 0f)
        {
            return false;
        }

        if (vp.x >= -margin && vp.x <= 1f + margin && vp.y >= -margin && vp.y <= 1f + margin)
        {
            return true;
        }

        return false;
    }
}
