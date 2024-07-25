using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using static PathFinder;

public class PathNodeBase
{
    public PathNodeBase prevConnection { get; private set; }
    public float G { get; private set; } //cost from start point
    public float H { get; private set; } //estimate cheapest cost to the end
    public float W { get; private set; } //extra weight of the node
    public float F => G + H + W;
    public Vector3Int gridPosition { get; private set; }
    public PathNodeBase(PathNodeBase prevNode, Vector3Int gridPos)
    {
        prevConnection = prevNode;
        gridPosition = gridPos;
    }
    public void SetPrevConnection(PathNodeBase node) => prevConnection = node;
    public void SetGValue(float g) => G = g;
    public void SetHValue(float h) => H = h;
    public void SetWValue(float w) => W = w;
    public void SetGridPosition(Vector3Int pos) => gridPosition = pos;
}

public class PathFinder : MonoBehaviour
{
    public static PathFinder instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    //0-up, 1-right, 2-down, 3-left
    private readonly Vector3Int[] neighborOffset = new Vector3Int[] {
        new Vector3Int(0,1,0), new Vector3Int(1,0,0),
        new Vector3Int(0,-1,0), new Vector3Int(-1,0,0)
    };

    [Header("Debug")]
    [SerializeField] private Camera cam;

    //debug
    [Header("Debug")]
    [SerializeField] private GameObject debugPathStart;
    [SerializeField] private Vector2 curVelocityDebug;
    private bool tempCheck;
    private List<PathNodeBase> tempPathCheck = new List<PathNodeBase>();

    // Start is called before the first frame update
    void Start()
    {

    }

    private void OnDrawGizmos()
    {
        if (tempPathCheck != null)
        {
            if (tempPathCheck.Count > 0)

            {
                Vector3Int initGridPos = GridInfo.instance.WorldToGridPos(debugPathStart.transform.position);
                Vector3 prev = GridInfo.instance.GridToWorldPos(initGridPos);
                foreach (PathNodeBase node in tempPathCheck)
                {
                    Vector3 cur = GridInfo.instance.grid.GetCellCenterWorld(node.gridPosition);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(prev, cur);
                    prev = cur;
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        ////debug
        //if (Input.GetMouseButtonDown(0))
        //{
        //    tempCheck = true;
        //    return;
        //}
        //if (Input.GetMouseButtonUp(0))
        //{
        //    tempCheck = false;
        //    return;
        //}
        //if (tempCheck)
        //{
        //    Vector3 pos = cam.ScreenToWorldPoint(Input.mousePosition);
        //    Vector3Int gridpos = GridInfo.instance.grid.WorldToCell(pos);
        //    tempPathCheck = NormalFindPath(GridInfo.instance.WorldToGridPos(debugPathStart.transform.position), gridpos, curVelocityDebug);
        //}
    }

    public List<PathNodeBase> NormalFindPath(Vector3Int start, Vector3Int end, Vector2 curMovDir)
    {
        //Find Path from start grid point to end grid point with A* algorithm

        //Check if the end point is walkable
        if (!GridInfo.instance.CheckGridCellWalkable(end))
        {
            Debug.LogWarning("End point is not walkable");
            return null;
        }

        //Initialize the lists
        List<PathNodeBase> openList = new List<PathNodeBase>();
        List<PathNodeBase> closedList = new List<PathNodeBase>();
        //Initialize the start point
        PathNodeBase startPoint = new PathNodeBase(null, start);
        startPoint.SetHValue(Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y));//calculate Mahattan Distance as H value
        startPoint.SetGValue(0);
        openList.Add(startPoint);
        //set start node to be the current node
        PathNodeBase currentNode = openList[0];
        //set start flag for preprocess
        bool isStart = true;
        while (openList.Count > 0)
        {
            //Get the node with lowest F value as current Node
            currentNode = openList[0];
            //Check if this node is the end point
            if (currentNode.gridPosition == end)
            {
                break;
            }
            //Move this node from open list to closed List
            closedList.Add(currentNode);
            openList.Remove(currentNode);
            //Add the available neighbors of the current node to the open list
            //Preprocess with the neighbors
            Vector3Int centerPos = currentNode.gridPosition;
            bool[] reachableFlag = new bool[4];
            {
                //0-up, 1-right, 2-down, 3-left
                for (int i = 0; i < 4; i++)
                {
                    //special process with the start node (ignore the node in the opposite direction of the current move direction)
                    if (isStart)
                    {
                        Vector2 oppoDir = new Vector2(-curMovDir.x, -curMovDir.y).normalized;
                        Vector2 offset = new Vector2(neighborOffset[i].x, neighborOffset[i].y);
                        if (offset == oppoDir)
                        {
                            reachableFlag[i] = false;
                            continue;
                        }
                    }
                    //process with the target neighbor
                    Vector3Int t = centerPos + neighborOffset[i];
                    if (GridInfo.instance.CheckGridCellWalkable(t))
                    {
                        //Reachable then add this node to the queue waiting for process
                        reachableFlag[i] = true;
                        AddNodeToOpenList(t, currentNode.G + 10, currentNode, ref openList, closedList, end);

                    }
                    else { reachableFlag[i] = false; }
                }
                if (isStart)
                {
                    //after processing with the initial point, set the flag to be false
                    isStart = false;
                }
            }
            //sort the open list by F value and H cost in ascending order
            openList.Sort((a, b) =>
            {
                if (a.F.CompareTo(b.F) != 0) return a.F.CompareTo(b.F);
                else
                    return a.H.CompareTo(b.H);
            });
        }

        if (currentNode.gridPosition != end)
        {
            //no available path to the point
            Debug.LogWarning("No reachable Path!!!!");
            return null;
        }
        else
        {
            //trace back the path
            List<PathNodeBase> path = new List<PathNodeBase>();
            TraceBackThePath(path, currentNode);
            return path;
        }

    }

    public List<PathNodeBase> PacManFindPath(Vector3Int start, Vector3Int end, bool isInvincible)
    {
        //Find Path from start grid point to end grid point with A* algorithm

        //Check if the end point is walkable
        if (!GridInfo.instance.CheckGridCellWalkable(end))
        {
            Debug.LogWarning("End point is not walkable");
            return null;
        }

        //Initialize the lists
        List<PathNodeBase> openList = new List<PathNodeBase>();
        List<PathNodeBase> closedList = new List<PathNodeBase>();
        //Initialize the start point
        PathNodeBase startPoint = new PathNodeBase(null, start);
        startPoint.SetHValue(Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y));//calculate Mahattan Distance as H value
        startPoint.SetGValue(0);
        startPoint.SetWValue(PacGameManager.instance.GetWeightMapValue(start));

        openList.Add(startPoint);
        //set start node to be the current node
        PathNodeBase currentNode = openList[0];
        while (openList.Count > 0)
        {
            //Get the node with lowest F value as current Node
            currentNode = openList[0];
            //Check if this node is the end point
            if (currentNode.gridPosition == end)
            {
                break;
            }
            //Move this node from open list to closed List
            closedList.Add(currentNode);
            openList.Remove(currentNode);
            //Add the available neighbors of the current node to the open list
            //Preprocess with the neighbors
            Vector3Int centerPos = currentNode.gridPosition;
            bool[] reachableFlag = new bool[4];
            {
                //0-up, 1-right, 2-down, 3-left
                for (int i = 0; i < 4; i++)
                {
                    //process with the target neighbor
                    Vector3Int t = centerPos + neighborOffset[i];
                    if (GridInfo.instance.CheckPacManGridCellWalkable(t,isInvincible))
                    {
                        //Reachable then add this node to the queue waiting for process
                        reachableFlag[i] = true;
                        PacManAddNodeToOpenList(t, currentNode.G + 10, currentNode, ref openList, closedList, end);

                    }
                    else { reachableFlag[i] = false; }
                }
            }
            //sort the open list by F value and H cost in ascending order
            openList.Sort((a, b) =>
            {
                if (a.F.CompareTo(b.F) != 0) return a.F.CompareTo(b.F);
                else
                    return a.H.CompareTo(b.H);
            });
        }

        if (currentNode.gridPosition != end)
        {
            //no available path to the point
            Debug.LogWarning("No reachable Path!!!!");
            return null;
        }
        else
        {
            //trace back the path
            List<PathNodeBase> path = new List<PathNodeBase>();
            TraceBackThePath(path, currentNode);
            return path;
        }
    }

    public void AddNodeToOpenList(Vector3Int gridPos, float newG, PathNodeBase cameFrom, ref List<PathNodeBase> openList, List<PathNodeBase> closedList, Vector3Int endPos)
    {
        //Initialize this new node
        PathNodeBase procNode = new PathNodeBase(cameFrom, gridPos);
        procNode.SetGValue(newG);
        procNode.SetHValue(Mathf.Abs(endPos.x - gridPos.x) + Mathf.Abs(endPos.y - gridPos.y));//calculate Mahattan Distance as H value
        //Check if this node should be added to the list
        //Firstly, check if this node has already been processed
        foreach (PathNodeBase node in closedList)
        {
            if (node.gridPosition == procNode.gridPosition)
            {
                return;
            }
        }
        //Then check if this one has already been added to the open list
        foreach (PathNodeBase node in openList)
        {
            if (node.gridPosition == procNode.gridPosition)
            {
                //if this node already in the queue waiting for process, check if the G value decreased
                if (procNode.G < node.G)
                {
                    //if G value decreased, replace the node in the queue with this new node
                    node.SetGValue(procNode.G);
                    node.SetPrevConnection(procNode.prevConnection);
                    return;
                }
                else
                {   //if G value increased then it is useless
                    return;
                }
            }
        }
        //Here the node is not in either lists, so we add it to the open list waiting for process
        openList.Add(procNode);

    }


    public void PacManAddNodeToOpenList(Vector3Int gridPos, float newG, PathNodeBase cameFrom, ref List<PathNodeBase> openList, List<PathNodeBase> closedList, Vector3Int endPos)
    {
        //Initialize this new node
        PathNodeBase procNode = new PathNodeBase(cameFrom, gridPos);
        procNode.SetGValue(newG);
        procNode.SetHValue(Mathf.Abs(endPos.x - gridPos.x) + Mathf.Abs(endPos.y - gridPos.y));//calculate Mahattan Distance as H value
        procNode.SetWValue(PacGameManager.instance.GetWeightMapValue(gridPos));
        //Check if this node should be added to the list
        //Firstly, check if this node has already been processed
        foreach (PathNodeBase node in closedList)
        {
            if (node.gridPosition == procNode.gridPosition)
            {
                return;
            }
        }
        //Then check if this one has already been added to the open list
        foreach (PathNodeBase node in openList)
        {
            if (node.gridPosition == procNode.gridPosition)
            {
                //if this node already in the queue waiting for process, check if the G value decreased
                if (procNode.G < node.G)
                {
                    //if G value decreased, replace the node in the queue with this new node
                    node.SetGValue(procNode.G);
                    node.SetPrevConnection(procNode.prevConnection);
                    return;
                }
                else
                {   //if G value increased then it is useless
                    return;
                }
            }
        }
        //Here the node is not in either lists, so we add it to the open list waiting for process
        openList.Add(procNode);

    }
    //Flee
    public struct SearchNode
    {
        public Vector3Int gridPos;
        public float distanceFromStart;
    }

    public void AddSearchNodetoOpenList(Vector3Int pos, float dis, ref List<SearchNode> openList, List<SearchNode> closedList)
    {
        SearchNode t = new SearchNode();
        t.gridPos = pos;
        t.distanceFromStart = dis;
        //Check if this node should be added to the list
        //Firstly, check if this node has already been processed
        foreach (SearchNode node in closedList)
        {
            if (node.gridPos == t.gridPos)
            {
                return;
            }
        }
        //Then check if this one has already been added to the open list
        foreach (SearchNode node in openList)
        {
            if (node.gridPos == t.gridPos)
            {
                //if this node already in the queue waiting for process, check if the G value decreased
                if (t.distanceFromStart < node.distanceFromStart)
                {
                    //if distance decreased, replace the node in the queue with this new node
                    openList.Remove(node);
                    openList.Add(t);
                    return;
                }
                else
                {
                    return;
                }
            }
        }
        //Here the node is not in either lists, so we add it to the open list waiting for process
        openList.Add(t);
    }

    public Vector2 FindFleePoint(Vector2 curPos, Vector2 fleeFromPoint, float fleeRange)
    {
        //Initialize start point to search
        SearchNode startNode = new SearchNode();
        startNode.gridPos = GridInfo.instance.WorldToGridPos(fleeFromPoint + (curPos - fleeFromPoint).normalized * fleeRange);
        startNode.distanceFromStart = 0;

        //Initialize the lists
        List<SearchNode> openList = new List<SearchNode>();
        List<SearchNode> closedList = new List<SearchNode>();
        openList.Add(startNode);
        //set current node as the start node
        SearchNode currentNode;

        while (openList.Count > 0)
        {
            //Set the currentNode as the closest 
            currentNode = openList[0];
            //Check if current node is valid for flee
            float dis = Vector2.Distance(GridInfo.instance.GridToWorldPos(currentNode.gridPos), fleeFromPoint);
            //when the node is walkable and beyond the flee range, the node is a valid fleepoint
            if (GridInfo.instance.CheckGridCellWalkable(currentNode.gridPos) && dis > fleeRange)
            {
                //fleepoint is valid return its position
                return GridInfo.instance.GridToWorldPos(currentNode.gridPos);
            }
            else
            {
                //Move this node from open list to closed List
                closedList.Add(currentNode);
                openList.Remove(currentNode);
                //Not a valid fleepoint, expand the searchlist with the neighbours
                Vector3Int centerPos = currentNode.gridPos;
                for (int i = 0; i < 4; i++)
                {
                    Vector3Int t = centerPos + neighborOffset[i];
                    if (t != GridInfo.instance.WorldToGridPos(fleeFromPoint))
                    {
                        //when not the grid player is in then add this node to the queue waiting for process
                        //process direct neibour
                        AddSearchNodetoOpenList(t, currentNode.distanceFromStart + 10, ref openList, closedList);

                    }
                }
                openList.Sort((a, b) => a.distanceFromStart.CompareTo(b.distanceFromStart));

            }
        }
        //no valid fleepoint
        Debug.Log("No valid flee point, stay position");
        return curPos;

    }

    //retrieve the full path
    public void TraceBackThePath(List<PathNodeBase> pathNodes, PathNodeBase curNode)
    {
        //if not the start node, then just trace back the previous node
        if (curNode.prevConnection != null)
        {
            TraceBackThePath(pathNodes, curNode.prevConnection);
        }
        //if reach the end then stop and add nodes one by one
        pathNodes.Add(curNode);

    }

    /*
    public Vector2 FindFleePoint(Vector2 curPos, Vector2 fleeFromPoint, float fleeRange)
    {
        //Initialize start point to search
        SearchNode startNode = new SearchNode();
        startNode.gridPos = GridInfo.instance.WorldToGridPos(fleeFromPoint + (curPos - fleeFromPoint).normalized * fleeRange);
        startNode.distanceFromStart = 0;

        //Initialize the lists
        List<SearchNode> openList = new List<SearchNode>();
        List<SearchNode> closedList = new List<SearchNode>();
        openList.Add(startNode);
        //set current node as the start node
        SearchNode currentNode;

        while(openList.Count > 0) 
        {
            //Set the currentNode as the closest 
            currentNode = openList[0];
            //Check if current node is valid for flee
            float dis = Vector2.Distance(GridInfo.instance.GridToWorldPos(currentNode.gridPos), fleeFromPoint);
            //when the node is walkable and beyond the flee range, the node is a valid fleepoint
            if(GridInfo.instance.CheckGridCellWalkable(currentNode.gridPos) && dis > fleeRange)
            {
                //fleepoint is valid return its position
                return GridInfo.instance.GridToWorldPos(currentNode.gridPos);
            }
            else
            {
                //Move this node from open list to closed List
                closedList.Add(currentNode);
                openList.Remove(currentNode);
                //Not a valid fleepoint, expand the searchlist with the neighbours
                Vector3Int centerPos = currentNode.gridPos;
                for (int i = 0; i < 8; i++)
                {
                    Vector3Int t = centerPos + neighborOffset[i];
                    if (t != GridInfo.instance.WorldToGridPos(fleeFromPoint))
                    {
                        //when not the grid player is in then add this node to the queue waiting for process
                        //process direct neibour
                        if (i % 2 == 0)
                        {
                            AddSearchNodetoOpenList(t, currentNode.distanceFromStart + 10, ref openList, closedList);
                        }
                        //process diagonal neibours
                        else
                        {
                            AddSearchNodetoOpenList(t, currentNode.distanceFromStart + 14, ref openList, closedList);
                        }
                    }
                }
                openList.Sort((a, b) => a.distanceFromStart.CompareTo(b.distanceFromStart));
                
            }
        }
        //no valid fleepoint
        Debug.Log("No valid flee point, stay position");
        return curPos;

    }
    */
}
