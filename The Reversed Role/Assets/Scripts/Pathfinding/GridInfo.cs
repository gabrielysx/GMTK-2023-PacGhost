using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridInfo : MonoBehaviour
{
    public Grid grid;
    [SerializeField] private List<Tilemap> walkableTileLayers = new List<Tilemap>();
    [SerializeField] private List<Tilemap> unwalkableTileLayers = new List<Tilemap>();
    [SerializeField] private Tilemap pacManNotWalkableTile;
    public static GridInfo instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector2 minPoint = Vector2.zero;
        Vector2 maxPoint = Vector2.zero;
        GetWalkableTilesBound(out minPoint, out maxPoint);
        Vector2 c0 = minPoint;
        Vector2 c1 = new Vector2(minPoint.x, maxPoint.y);
        Vector2 c2 = maxPoint;
        Vector2 c3 = new Vector2(maxPoint.x, minPoint.y);
        Gizmos.DrawLine(c0, c1);
        Gizmos.DrawLine(c1, c2);
        Gizmos.DrawLine(c2, c3);
        Gizmos.DrawLine(c3, c0);
    }

    public void GetWalkableTilesBound(out Vector2 minPoint,out Vector2 maxPoint)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        foreach (Tilemap tilemap in walkableTileLayers)
        {
            BoundsInt bounds = tilemap.cellBounds;
            if (minX > bounds.xMin)
            {
                minX = bounds.xMin;
            }
            if (minY > bounds.yMin)
            {
                minY = bounds.yMin;
            }
            if (maxX < bounds.xMax)
            {
                maxX = bounds.xMax;
            }
            if (maxY < bounds.yMax)
            {
                maxY = bounds.yMax;
            }
        }
        minPoint = new Vector2(minX, minY);
        maxPoint = new Vector2(maxX, maxY);
    }

    public bool CheckGridCellWalkable(Vector3Int gridPosition)
    {
        bool result = false;
        //check if thats at least a walkable area rather than a void area
        foreach (Tilemap tilemap in walkableTileLayers)
        {
            if (tilemap.GetTile(gridPosition) != null)
            {
                result = true;
                break;
            }
        }
        //If void area then just return false
        if (result == false)
        {
            return false;
        }
        //If within walkable area, start checking walls and obstacles
        else
        {
            foreach (Tilemap blockedTilemap in unwalkableTileLayers)
            {
                //when there is a obstacle in the grid then return false
                if (blockedTilemap.GetTile(gridPosition) != null)
                {
                    return false;
                }
            }
            //If there is no walls or obstacles at all layers then return true
            return true;
        }
    }

    public bool CheckPacManGridCellWalkable(Vector3Int gridPosition,bool isInvincible)
    {
        bool result = false;
        //check if thats at least a walkable area rather than a void area
        foreach (Tilemap tilemap in walkableTileLayers)
        {
            if (tilemap.GetTile(gridPosition) != null)
            {
                result = true;
                break;
            }
        }
        //If void area then just return false
        if (result == false)
        {
            return false;
        }
        //If within walkable area, start checking walls and obstacles
        else
        {
            foreach (Tilemap blockedTilemap in unwalkableTileLayers)
            {
                //when there is a obstacle in the grid then return false
                if (blockedTilemap.GetTile(gridPosition) != null)
                {
                    return false;
                }
            }
            if(pacManNotWalkableTile.GetTile(gridPosition) != null)
            {
                return false;
            }
            //check the ghost position when not invincible
            if (!isInvincible)
            {
                if (PacGameManager.instance.ghosts.Count > 0)
                {
                    foreach (GameObject ghost in PacGameManager.instance.ghosts)
                    {
                        Ghost ghostController = ghost.GetComponent<Ghost>();
                        //when ghost not dead and in the target grid then return false (not walkable for pacman)
                        if (ghostController.GetCurState() != GhostState.Dead)
                        {
                            if (WorldToGridPos(ghost.transform.position) == gridPosition)
                            {
                                return false;
                            }
                            if (ghostController.curPath != null)
                            {
                                //check the ghost next 2 position on the path
                                if (ghostController.curPath.Count > 2)
                                {
                                    if (ghostController.curPath[2].gridPosition == gridPosition)
                                    {
                                        return false;
                                    }
                                    if (ghostController.curPath[1].gridPosition == gridPosition)
                                    {
                                        return false;
                                    }
                                }
                                else if (ghostController.curPath.Count > 1)
                                {
                                    if (ghostController.curPath[1].gridPosition == gridPosition)
                                    {
                                        return false;
                                    }
                                }
                            }
                            //check the ghost next position in current direction
                            Vector3Int frontPos = WorldToGridPos(ghost.transform.position) + new Vector3Int(Mathf.RoundToInt(ghostController.curDir.x), Mathf.RoundToInt(ghostController.curDir.y), 0);
                            if (frontPos == gridPosition)
                            {
                                return false;
                            }
                            //Vector3Int backPos = WorldToGridPos(ghost.transform.position) - new Vector3Int(Mathf.RoundToInt(ghostController.curDir.x), Mathf.RoundToInt(ghostController.curDir.y), 0);
                            //if (backPos == gridPosition)
                            //{
                            //    return false;
                            //}
                        }
                    }
                }
            }
            //If there is no walls or obstacles at all layers then return true
            return true;
        }
    }

    public Vector3Int WorldToGridPos(Vector3 pos)
    {
        return grid.WorldToCell(pos);
    }

    public Vector3 GridToWorldPos(Vector3Int pos)
    {
        return grid.GetCellCenterWorld(pos);
    }

    public Vector3 GetTargetPosGridCenter(Vector3 targetWorldPos)
    {
        Vector3Int targetGridPos = WorldToGridPos(targetWorldPos);
        return GridToWorldPos(targetGridPos);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
