using UnityEngine;

public class BubbleTileController : MonoBehaviour
{
    public ObjectPool ObjectPool;

    [Header("Grid Settings")]
    [SerializeField] int rows = 10;
    [SerializeField] int cols = 8;
    [SerializeField] float bubbleRadius = 0.5f;

    [Header("Start Position")]
    [SerializeField] Vector2 startPosition = new Vector2(0, 5);

    [Header("Bubble Types")]
    [SerializeField] bool useRandomTypes = true;
    [SerializeField] BubbleTypes[] availableTypes;

    private Bubble[,] grid;
    private Vector3[,] positions; 

    float hexWidth;
    float hexHeight;

    void Start()
    {
        InitializeGrid();
        positions = BuildHexGridPositions();
        SpawnGridBubbles();
    }


    void InitializeGrid()
    {
        grid = new Bubble[rows, cols];

        hexWidth  = bubbleRadius * 2f;
        hexHeight = Mathf.Sqrt(3f) * bubbleRadius;
    }


    Vector3[,] BuildHexGridPositions()
    {
        Vector3[,] pos = new Vector3[rows, cols];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                pos[row, col] = CalculateHexPosition(row, col);
            }
        }

        return pos;
    }

    Vector3 CalculateHexPosition(int row, int col)
    {
        float offset = (row % 2 == 0) ? 0f : hexWidth * 0.5f;

        float x = startPosition.x + (col * hexWidth) + offset;
        float y = startPosition.y - (row * hexHeight * 0.75f);

        return new Vector3(x, y, 0);
    }


    void SpawnGridBubbles()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Bubble bubble = ObjectPool.SpawnBubble();
                if (bubble == null)
                    continue;

                // 위치 배치
                bubble.transform.position = positions[row, col];

                // 타입 설정
                if (useRandomTypes && availableTypes.Length > 0)
                    bubble.SetBubbleType(GetRandomType());

                grid[row, col] = bubble;
            }
        }
    }


    BubbleTypes GetRandomType()
    {
        int index = Random.Range(0, availableTypes.Length);
        return availableTypes[index];
    }

}
