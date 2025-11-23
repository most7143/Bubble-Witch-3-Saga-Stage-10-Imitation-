using UnityEngine;

/// <summary>
/// 개별 스포너 오브젝트에 붙이는 컴포넌트
/// </summary>
public class SpawnerPoint : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private bool isLeftSpawner = true; // true: 왼쪽 스포너, false: 오른쪽 스포너
    
    [Header("Spawner Position")]
    [SerializeField] private int spawnerRow = 0;
    [SerializeField] private int spawnerCol = 3; // 기본값, 에디터에서 설정
    
    [Header("References")]
    [SerializeField] private HexMap hexMap;
    
    public bool IsLeftSpawner => isLeftSpawner;
    public int SpawnerRow => spawnerRow;
    public int SpawnerCol => spawnerCol;
    
    void Start()
    {
        if (hexMap == null)
            hexMap = FindObjectOfType<HexMap>();
        
        // 스포너 위치 자동 조정
        UpdatePosition();
    }
    
    /// <summary>
    /// 스포너 위치를 HexMap 좌표에 맞게 자동 조정
    /// </summary>
    public void UpdatePosition()
    {
        if (hexMap == null) return;
        
        // bool 값에 따라 기본 위치 설정
        if (isLeftSpawner)
        {
            spawnerCol = 3; // 왼쪽 스포너는 col 3
        }
        else
        {
            spawnerCol = 7; // 오른쪽 스포너는 col 7
        }
        
        // Transform 위치 업데이트
        if (hexMap.Positions != null && 
            spawnerRow >= 0 && spawnerRow < hexMap.Rows &&
            spawnerCol >= 0 && spawnerCol < hexMap.Cols)
        {
            Vector3 spawnerPos = hexMap.Positions[spawnerRow, spawnerCol];
            transform.position = spawnerPos;
        }
    }
    
    /// <summary>
    /// 스포너 위치를 외부에서 설정
    /// </summary>
    public void SetSpawnerPosition(int row, int col)
    {
        spawnerRow = row;
        spawnerCol = col;
        UpdatePosition();
    }
    
    void OnValidate()
    {
        // 에디터에서 값이 변경되면 위치 업데이트
        if (Application.isPlaying)
        {
            UpdatePosition();
        }
    }
}
