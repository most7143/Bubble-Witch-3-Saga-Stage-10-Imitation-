using System.Collections.Generic;

public interface IHexMap
{
    // HexMapHandler가 필요한 기본 메서드들
    List<(int row, int col)> GetAdjacentCells(int row, int col);
    bool IsEmpty(int row, int col);
    Bubble GetBubble(int row, int col);
    int Rows { get; }
    int Cols { get; }
}

public interface IHexMapHandler
{
    // HexMapHandler가 제공하는 메서드들
    List<(int row, int col)> GetEmptyAdjacentCells(int row, int col);
    List<List<(int r, int c)>> CollectConnectedByLevel(int startRow, int startCol);
    List<(int row, int col)> FindConnectedSameType(int row, int col);
    bool IsBubbleConnectedToTop(int row, int col);
    bool IsBubbleFloating(int row, int col);
    List<(int r, int c)> FindFloatingBubblesAfterRemoval(HashSet<(int r, int c)> toRemove);
    List<(int r, int c)> FindFloatingBubbles();
    Dictionary<(int, int), int> CalculateDepthFromStart(int startRow, int startCol, List<(int r, int c)> targetList);
}

public class HexMapHandler : IHexMapHandler
{
    private IHexMap _hexMap;

    public HexMapHandler(IHexMap hexMap)
    {
        _hexMap = hexMap;
    }

    /// <summary>
    /// 현재 좌표에서 빈 인접 좌표 리스트를 반환
    /// </summary>
    /// <param name="row">현재 row</param>
    /// <param name="col">현재 col</param>
    /// <returns>빈 인접 좌표 리스트</returns>
    public List<(int row, int col)> GetEmptyAdjacentCells(int row, int col)
    {
        List<(int row, int col)> emptyCells = new List<(int row, int col)>();

        if (_hexMap == null)
            return emptyCells;

        // 인접 셀 가져오기
        List<(int row, int col)> adjacentCells = _hexMap.GetAdjacentCells(row, col);

        // 빈 셀만 필터링
        foreach (var (adjRow, adjCol) in adjacentCells)
        {
            if (_hexMap.IsEmpty(adjRow, adjCol))
            {
                emptyCells.Add((adjRow, adjCol));
            }
        }

        return emptyCells;
    }

    /// <summary>
    /// BFS로 같은 타입의 연결된 버블을 레벨별로 수집 (중심에서 퍼져나가는 순서)
    /// </summary>
    /// <param name="startRow">시작 row</param>
    /// <param name="startCol">시작 col</param>
    /// <returns>레벨별로 그룹화된 연결된 버블 좌표 리스트</returns>
    public List<List<(int r, int c)>> CollectConnectedByLevel(int startRow, int startCol)
    {
        List<List<(int r, int c)>> levelList = new List<List<(int r, int c)>>();

        if (_hexMap == null)
            return levelList;

        Bubble start = _hexMap.GetBubble(startRow, startCol);
        if (start == null)
            return levelList;

        BubbleTypes targetType = start.BubbleType;

        Queue<(int r, int c)> q = new Queue<(int r, int c)>();
        HashSet<(int r, int c)> visited = new HashSet<(int r, int c)>();

        q.Enqueue((startRow, startCol));
        visited.Add((startRow, startCol));

        while (q.Count > 0)
        {
            int size = q.Count;
            List<(int r, int c)> currentLevel = new List<(int r, int c)>();

            for (int i = 0; i < size; i++)
            {
                var (r, c) = q.Dequeue();
                currentLevel.Add((r, c));

                foreach (var (nr, nc) in _hexMap.GetAdjacentCells(r, c))
                {
                    if (visited.Contains((nr, nc))) continue;

                    Bubble b = _hexMap.GetBubble(nr, nc);
                    if (b != null && b.BubbleType == targetType)
                    {
                        visited.Add((nr, nc));
                        q.Enqueue((nr, nc));
                    }
                }
            }

            levelList.Add(currentLevel);
        }

        return levelList;
    }

    /// <summary>
    /// BFS로 같은 타입의 연결된 버블 찾기 (레벨 구분 없이)
    /// </summary>
    /// <param name="row">시작 row</param>
    /// <param name="col">시작 col</param>
    /// <returns>연결된 버블 좌표 리스트</returns>
    public List<(int row, int col)> FindConnectedSameType(int row, int col)
    {
        List<(int, int)> result = new List<(int, int)>();

        if (_hexMap == null)
            return result;

        Bubble start = _hexMap.GetBubble(row, col);
        if (start == null)
            return result;

        BubbleTypes targetType = start.BubbleType;

        Queue<(int, int)> q = new Queue<(int, int)>();
        HashSet<(int, int)> visited = new HashSet<(int, int)>();

        q.Enqueue((row, col));
        visited.Add((row, col));

        while (q.Count > 0)
        {
            var (cr, cc) = q.Dequeue();
            result.Add((cr, cc));

            foreach (var (nr, nc) in _hexMap.GetAdjacentCells(cr, cc))
            {
                if (!visited.Contains((nr, nc)))
                {
                    Bubble nb = _hexMap.GetBubble(nr, nc);
                    if (nb != null && nb.BubbleType == targetType)
                    {
                        visited.Add((nr, nc));
                        q.Enqueue((nr, nc));
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 특정 버블이 천장(0번째 row)에 연결되어 있는지 체크
    /// </summary>
    /// <param name="row">체크할 버블의 row</param>
    /// <param name="col">체크할 버블의 col</param>
    /// <returns>천장에 연결되어 있으면 true, 고립되어 있으면 false</returns>
    public bool IsBubbleConnectedToTop(int row, int col)
    {
        if (_hexMap == null)
            return false;

        // 이미 천장에 있으면 연결됨
        if (row == 0)
        {
            Bubble b = _hexMap.GetBubble(row, col);
            return b != null;
        }

        int rows = _hexMap.Rows;
        int cols = _hexMap.Cols;
        bool[,] visited = new bool[rows, cols];
        Queue<(int, int)> q = new Queue<(int, int)>();

        // 천장(0번째 row)에 붙은 버블을 시작점으로 설정
        for (int c = 0; c < cols; c++)
        {
            Bubble b = _hexMap.GetBubble(0, c);
            if (b != null)
            {
                visited[0, c] = true;
                q.Enqueue((0, c));
            }
        }

        // BFS로 천장에 연결된 버블 추적
        while (q.Count > 0)
        {
            var (r, c) = q.Dequeue();

            // 목표 버블에 도달했으면 연결됨
            if (r == row && c == col)
                return true;

            foreach (var (nr, nc) in _hexMap.GetAdjacentCells(r, c))
            {
                if (!visited[nr, nc])
                {
                    Bubble nb = _hexMap.GetBubble(nr, nc);
                    if (nb != null)
                    {
                        visited[nr, nc] = true;
                        q.Enqueue((nr, nc));
                    }
                }
            }
        }

        // 목표 버블에 도달하지 못했으면 고립됨
        return false;
    }

    /// <summary>
    /// 특정 버블이 고립되어 있는지 체크
    /// </summary>
    /// <param name="row">체크할 버블의 row</param>
    /// <param name="col">체크할 버블의 col</param>
    /// <returns>고립되어 있으면 true, 천장에 연결되어 있으면 false</returns>
    public bool IsBubbleFloating(int row, int col)
    {
        if (_hexMap == null)
            return false;

        Bubble bubble = _hexMap.GetBubble(row, col);
        if (bubble == null)
            return false;

        // 천장에 연결되어 있지 않으면 고립됨
        return !IsBubbleConnectedToTop(row, col);
    }

    /// <summary>
    /// 특정 버블들을 제거한 상태에서 고립 버블 찾기 (시뮬레이션)
    /// </summary>
    /// <param name="toRemove">제거할 버블 좌표 집합</param>
    /// <returns>고립된 버블 좌표 리스트</returns>
    public List<(int r, int c)> FindFloatingBubblesAfterRemoval(HashSet<(int r, int c)> toRemove)
    {
        List<(int, int)> floating = new List<(int, int)>();

        if (_hexMap == null)
            return floating;

        int rows = _hexMap.Rows;
        int cols = _hexMap.Cols;
        bool[,] visited = new bool[rows, cols];
        Queue<(int, int)> q = new Queue<(int, int)>();

        // 천장(0번째 row)에 붙은 버블을 시작점으로 설정 (제거 대상이 아닌 것만)
        for (int c = 0; c < cols; c++)
        {
            if (toRemove.Contains((0, c)))
                continue;

            Bubble b = _hexMap.GetBubble(0, c);
            if (b != null)
            {
                visited[0, c] = true;
                q.Enqueue((0, c));
            }
        }

        // BFS로 천장에 연결된 버블 추적 (제거 대상 제외)
        while (q.Count > 0)
        {
            var (r, c) = q.Dequeue();

            foreach (var (nr, nc) in _hexMap.GetAdjacentCells(r, c))
            {
                if (!visited[nr, nc] && !toRemove.Contains((nr, nc)))
                {
                    Bubble nb = _hexMap.GetBubble(nr, nc);
                    if (nb != null)
                    {
                        visited[nr, nc] = true;
                        q.Enqueue((nr, nc));
                    }
                }
            }
        }

        // visited되지 않고 제거 대상이 아닌 버블 = 고립 버블
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!visited[r, c] && !toRemove.Contains((r, c)) && _hexMap.GetBubble(r, c) != null)
                    floating.Add((r, c));
            }
        }

        return floating;
    }

    /// <summary>
    /// 천장(0번째 row)에 연결되지 않은 고립 버블 찾기
    /// </summary>
    /// <returns>고립된 버블 좌표 리스트</returns>
    public List<(int r, int c)> FindFloatingBubbles()
    {
        List<(int, int)> floating = new List<(int, int)>();

        if (_hexMap == null)
            return floating;

        int rows = _hexMap.Rows;
        int cols = _hexMap.Cols;
        bool[,] visited = new bool[rows, cols];
        Queue<(int, int)> q = new Queue<(int, int)>();

        // 천장(0번째 row)에 붙은 버블을 시작점으로 설정
        for (int c = 0; c < cols; c++)
        {
            Bubble b = _hexMap.GetBubble(0, c);
            if (b != null)
            {
                visited[0, c] = true;
                q.Enqueue((0, c));
            }
        }

        // BFS로 천장에 연결된 버블 추적
        while (q.Count > 0)
        {
            var (r, c) = q.Dequeue();

            foreach (var (nr, nc) in _hexMap.GetAdjacentCells(r, c))
            {
                if (!visited[nr, nc])
                {
                    Bubble nb = _hexMap.GetBubble(nr, nc);
                    if (nb != null)
                    {
                        visited[nr, nc] = true;
                        q.Enqueue((nr, nc));
                    }
                }
            }
        }

        // visited되지 않은 버블 = 고립 버블
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!visited[r, c] && _hexMap.GetBubble(r, c) != null)
                    floating.Add((r, c));
            }
        }

        return floating;
    }

    /// <summary>
    /// BFS로 시작점부터 각 좌표까지의 depth(거리) 계산
    /// </summary>
    /// <param name="startRow">시작 row</param>
    /// <param name="startCol">시작 col</param>
    /// <param name="targetList">depth를 계산할 좌표 리스트</param>
    /// <returns>좌표별 depth Dictionary</returns>
    public Dictionary<(int, int), int> CalculateDepthFromStart(int startRow, int startCol, List<(int r, int c)> targetList)
    {
        Dictionary<(int, int), int> depth = new Dictionary<(int, int), int>();

        if (_hexMap == null || targetList == null || targetList.Count == 0)
            return depth;

        var start = (startRow, startCol);
        Queue<(int, int)> q = new Queue<(int, int)>();
        HashSet<(int, int)> visited = new HashSet<(int, int)>();

        q.Enqueue(start);
        visited.Add(start);
        depth[start] = 0;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int cr = cur.Item1;
            int cc = cur.Item2;

            foreach (var (nr, nc) in _hexMap.GetAdjacentCells(cr, cc))
            {
                var key = (nr, nc);
                if (!visited.Contains(key) && targetList.Contains(key))
                {
                    visited.Add(key);
                    depth[key] = depth[cur] + 1;
                    q.Enqueue(key);
                }
            }
        }

        return depth;
    }
}
