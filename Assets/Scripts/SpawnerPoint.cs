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

    public Sprite IdleSprite;

    public Animator Animator;

    public SpriteRenderer Renderer;
    
    
    public bool IsLeftSpawner => isLeftSpawner;
    public int SpawnerRow => spawnerRow;
    public int SpawnerCol => spawnerCol;
    
    /// <summary>
    /// HexMap 참조 및 위치 초기화
    /// </summary>
    void Start()
    {
        if (hexMap == null)
            hexMap = FindObjectOfType<HexMap>();
        
        UpdatePosition();
    }
    
    /// <summary>
    /// 스포너 위치를 HexMap 좌표에 맞게 자동 조정
    /// </summary>
    public void UpdatePosition()
    {
        if (hexMap == null) return;
        
        if (isLeftSpawner)
        {
            spawnerCol = 3;
        }
        else
        {
            spawnerCol = 7;
        }
        
        if (spawnerRow >= 0 && spawnerRow < hexMap.Rows &&
            spawnerCol >= 0 && spawnerCol < hexMap.Cols)
        {
            Vector3 spawnerPos = hexMap.GetWorldPosition(spawnerRow, spawnerCol);
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
    
    /// <summary>
    /// 에디터에서 값 변경 시 위치 업데이트
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// 스포너 애니메이터 활성화
    /// </summary>
    public void ActivateSpawner()
    {
         if (Animator != null && !Animator.enabled)
            {
                Animator.enabled = true;
            }
    }

    /// <summary>
    /// 스포너 애니메이터 비활성화 및 아이들 스프라이트 설정
    /// </summary>
    public void DeactivateSpawner()
    {
        if (Animator != null && Animator.enabled)
        {
            Animator.enabled = false;
        }
        Renderer.sprite = IdleSprite;
    }
}
