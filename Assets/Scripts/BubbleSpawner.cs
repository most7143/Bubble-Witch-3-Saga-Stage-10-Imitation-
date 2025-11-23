using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BubbleSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private float spawnInterval = 2f; // 버블 생성 간격
    
    [Header("Spawner Points")]
    [SerializeField] private List<SpawnerPoint> spawnerPoints = new List<SpawnerPoint>();
    
    [Header("References")]
    [SerializeField] private HexMap hexMap;
    [SerializeField] private ObjectPool objectPool;
    
    private bool isSpawning = false;
    
    // 각 스포너별 현재 생성 위치 추적
    private Dictionary<SpawnerPoint, int> spawnerCurrentCols = new Dictionary<SpawnerPoint, int>();
    
    void Start()
    {
        InitializeSpawners();
        StartSpawning();
    }
    
    private void InitializeSpawners()
    {
        foreach (var spawner in spawnerPoints)
        {
            if (spawner != null)
            {
                spawner.UpdatePosition();
                int clampedCol = Mathf.Clamp(spawner.SpawnerCol, 0, hexMap.Cols - 1);
                spawnerCurrentCols[spawner] = clampedCol;
            }
        }
    }
    
    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;
        
        foreach (var spawner in spawnerPoints)
        {
            if (spawner != null)
            {
                StartCoroutine(SpawnBubblesRoutine(spawner));
            }
        }
    }
    
    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }
    
    private IEnumerator SpawnBubblesRoutine(SpawnerPoint spawner)
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnSingleBubble(spawner);
        }
    }
    
    private void SpawnSingleBubble(SpawnerPoint spawner)
    {
        if (hexMap == null || spawner == null) return;
        
        int spawnerRow = spawner.SpawnerRow;
        int spawnerCol = spawner.SpawnerCol;
        bool isLeft = spawner.IsLeftSpawner;
        int currentCol = spawnerCurrentCols[spawner];
        int direction = isLeft ? -1 : 1;  // 왼쪽: -1 (왼쪽으로), 오른쪽: 1 (오른쪽으로)
        int targetCol = currentCol + direction;

        if (targetCol < 0 || targetCol >= hexMap.Cols)
        {
            targetCol = spawnerCol; // 범위를 벗어나면 스포너 위치로 리셋
        }

        // 스포너 위치에서부터 targetCol까지 버블들을 밀어내기
        ShiftBubblesInRow(spawnerRow, spawnerCol, targetCol, isLeft);

        if (hexMap.IsEmpty(spawnerRow, targetCol))
        {
            BubbleTypes type = GetRandomBubbleType();
            Bubble bubble = objectPool.SpawnBubble(type);

            if (bubble != null)
            {
                Vector3 spawnPos = hexMap.Positions[spawnerRow, targetCol];
                bubble.transform.position = spawnPos;
                hexMap.RegisterBubble(spawnerRow, targetCol, bubble);
                spawnerCurrentCols[spawner] = targetCol;
            }
            else
            {
                Debug.LogWarning("ObjectPool에서 버블을 가져오지 못했습니다.");
            }
        }
    }
    
    private void ShiftBubblesInRow(int row, int spawnerCol, int targetCol, bool isLeft)
    {
        int cols = hexMap.Cols;

        if (isLeft)
        {
            // 왼쪽 스포너: 스포너 위치부터 targetCol까지 왼쪽으로 밀기
            // spawnerCol에서 targetCol 방향(왼쪽)으로 있는 버블들을 왼쪽으로 한 칸씩 이동
            int startCol = Mathf.Min(spawnerCol, targetCol);
            int endCol = Mathf.Max(spawnerCol, targetCol);
            
            // 오른쪽에서 왼쪽으로 순회 (밀어내기 위해)
            for (int col = endCol; col >= startCol; col--)
            {
                Bubble bubble = hexMap.GetBubble(row, col);
                if (bubble != null)
                {
                    int newCol = col - 1;
                    if (newCol >= 0 && hexMap.IsEmpty(row, newCol))
                    {
                        hexMap.UnregisterBubble(row, col);
                        hexMap.RegisterBubble(row, newCol, bubble);
                        Vector3 newPos = hexMap.Positions[row, newCol];
                        StartCoroutine(MoveBubbleToPosition(bubble, newPos));
                    }
                }
            }
        }
        else
        {
            // 오른쪽 스포너: 스포너 위치부터 targetCol까지 오른쪽으로 밀기
            // spawnerCol에서 targetCol 방향(오른쪽)으로 있는 버블들을 오른쪽으로 한 칸씩 이동
            int startCol = Mathf.Min(spawnerCol, targetCol);
            int endCol = Mathf.Max(spawnerCol, targetCol);
            
            // 왼쪽에서 오른쪽으로 순회 (밀어내기 위해)
            for (int col = startCol; col <= endCol; col++)
            {
                Bubble bubble = hexMap.GetBubble(row, col);
                if (bubble != null)
                {
                    int newCol = col + 1;
                    if (newCol < cols && hexMap.IsEmpty(row, newCol))
                    {
                        hexMap.UnregisterBubble(row, col);
                        hexMap.RegisterBubble(row, newCol, bubble);
                        Vector3 newPos = hexMap.Positions[row, newCol];
                        StartCoroutine(MoveBubbleToPosition(bubble, newPos));
                    }
                }
            }
        }
    }
    
    private BubbleTypes GetRandomBubbleType()
    {
        BubbleTypes[] types = { BubbleTypes.Red, BubbleTypes.Blue, BubbleTypes.Yellow };
        return types[Random.Range(0, types.Length)];
    }
    
    private IEnumerator MoveBubbleToPosition(Bubble bubble, Vector3 targetPos)
    {
        Vector3 startPos = bubble.transform.position;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bubble.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        bubble.transform.position = targetPos;
    }
}
