using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GhostState
{
    Chase,
    Scatter,
    Frightened,
    Dead
}

[Serializable]
public class ScatterPositionSet
{
    public List<GameObject> posAnchors;
}

public class Ghost : MonoBehaviour
{
    public int GhostID;
    public Vector2 curDir,nextDir;
    [SerializeField] private GhostState curState;public GhostState GetCurState() { return curState; }
    public void SetCurState(GhostState newState) 
    { curState = newState; }
    public List<PathNodeBase> curPath;
    private bool inControl;
    public bool controlable;
    private Vector2 initPos;
    private bool isReviving;
    private Vector2 targetPos,scatterDestination;
    private Vector2 movementNextGrid;
    private bool reachTargetGrid, reachScatterDestination;
    [SerializeField] private GameObject selectIndicator, dirIndicatior;
    [SerializeField] private GameObject pacMan;
    [SerializeField] private int patternID;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Animator anim;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float fleeRange = 5f;
    [SerializeField] private List<ScatterPositionSet> scatterPos = new List<ScatterPositionSet>(4);

    //timers
    [SerializeField] private float scatterTime = 7f;
    [SerializeField] private float chaseTime = 20f;
    private float scatterTimer,chaseTimer;

    //speed up
    [SerializeField] private float speedUpTime = 10f;
    private bool isSpeedUp;
    private float speedUpTimer;
    //debug
    private List<PathNodeBase> tempPathCheck = new List<PathNodeBase>();
    private readonly Color[] colors = new Color[] { Color.red, new Color(255, 192, 203, 1), Color.cyan, new Color(255, 165, 0, 1) };
    private void OnDrawGizmos()
    {
        if (tempPathCheck != null)
        {
            if (tempPathCheck.Count > 0)

            {
                Vector3Int initGridPos = GridInfo.instance.WorldToGridPos(transform.position);
                Vector3 prev = GridInfo.instance.GridToWorldPos(initGridPos);
                foreach (PathNodeBase node in tempPathCheck)
                {
                    Vector3 cur = GridInfo.instance.grid.GetCellCenterWorld(node.gridPosition);
                    Gizmos.color = colors[patternID];
                    Gizmos.DrawLine(prev, cur);
                    prev = cur;
                }
            }
        }
        else
        {
            Vector3 prev = GridInfo.instance.GetTargetPosGridCenter(transform.position);
            Gizmos.DrawLine(prev,prev);
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("PacMan"))
        {
            if (curState == GhostState.Frightened)
            {
                //eat ghost
                PacGameManager.instance.PlaySFX(2);
                if (inControl)
                {
                    PacGameManager.instance.TransferToNextControll();
                }
                curState = GhostState.Dead;
                anim.SetBool("isFrightened", false);
                sr.color = new Color(1, 1, 1, 0.5f);
                inControl = false;
                controlable = false;

                //set speed to lower speed
                speed = 1f;
                transform.position = GridInfo.instance.GetTargetPosGridCenter(transform.position);
            }
            else if (!collision.gameObject.GetComponent<PacMan>().invincible && curState != GhostState.Dead)
            {
                //pacman dead
                collision.GetComponent<PacMan>().Dead();
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        controlable = true;
        initPos= GridInfo.instance.GetTargetPosGridCenter(transform.position);
        curState = GhostState.Scatter;
        reachTargetGrid = true;
        reachScatterDestination = true;
        GhostAILogic();
    }

    // Update is called once per frame
    void Update()
    {
        if(inControl)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                nextDir = Vector2Int.up;
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                nextDir = Vector2Int.down;
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                nextDir = Vector2Int.left;
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                nextDir = Vector2Int.right;
            }
        }
        float angle = Mathf.Atan2(nextDir.y, nextDir.x) * Mathf.Rad2Deg;
        Quaternion rot = new Quaternion();
        rot.eulerAngles = new Vector3(0, 0, angle - 90f);
        dirIndicatior.transform.rotation = rot;
    }

    private void FixedUpdate()
    {
        //debug
        tempPathCheck = curPath;

        //speedUP timer
        if(isSpeedUp)
        {
            speedUpTimer += Time.fixedDeltaTime;
            if(speedUpTimer > speedUpTime)
            {
                speedUpTimer = 0;
                isSpeedUp = false;
                //reset speed to normal
                speed = 2f;
            }
        }

        //override other logic when reviving
        if(isReviving)
        {
            curState = GhostState.Dead;
        }

        if (inControl && controlable)
        {
            CalculateMoveDirection();
        }
        else
        {
            GhostAILogic();
        }
        MoveTowardsTargetPosition(movementNextGrid, speed);

    }

    

    //controll
    private void CalculateMoveDirection()
    {
        if (reachTargetGrid)
        {
            Vector3Int curGridPos = GridInfo.instance.WorldToGridPos(transform.position);
            Vector3Int predictedNextGridPos = curGridPos + new Vector3Int(Mathf.RoundToInt(nextDir.x), Mathf.RoundToInt(nextDir.y), 0);
            Vector3Int nextGridPosInCurDirection = curGridPos + new Vector3Int(Mathf.RoundToInt(curDir.x), Mathf.RoundToInt(curDir.y), 0);
            if (GridInfo.instance.CheckGridCellWalkable(predictedNextGridPos) && (-curDir != nextDir || PacGameManager.instance.ifGhostsCanTurnBack))
            {
                curDir = nextDir;
                movementNextGrid = GridInfo.instance.GridToWorldPos(predictedNextGridPos);
            }
            else if (!GridInfo.instance.CheckGridCellWalkable(nextGridPosInCurDirection))
            {
                movementNextGrid = GridInfo.instance.GridToWorldPos(curGridPos);
            }
            else
            {
                movementNextGrid = GridInfo.instance.GridToWorldPos(nextGridPosInCurDirection);
            }
            reachTargetGrid = false;
        }

    }

    public void MoveTowardsTargetPosition(Vector2 targetPos, float speed)
    {
        Vector2 currentPos = gameObject.transform.position;
        Vector2 step = speed * Time.fixedDeltaTime * curDir.normalized;

        //When close to the target position, let the enemy stop at the target position
        if (step.magnitude >= (targetPos - currentPos).magnitude || step.magnitude == 0)
        {
            Debug.Log("reach target!");
            transform.position = targetPos;
            reachTargetGrid = true;
            if ((curState == GhostState.Scatter||curState == GhostState.Frightened) && scatterDestination == targetPos)
            {
                reachScatterDestination = true;
            }else if(curState == GhostState.Dead && initPos == targetPos) 
            {
                Revive();
            }
        }
        else
        {
            rb.MovePosition(currentPos + step);
            
        }
    }

    private Vector2 GetNextWaypoint()
    {
        if (curPath.Count <= 1)
        {
            //no path then return current position
            Vector3Int curGridPos = GridInfo.instance.WorldToGridPos(transform.position);
            return GridInfo.instance.GridToWorldPos(curGridPos);
        }
        else
        {
            return GridInfo.instance.GridToWorldPos(curPath[1].gridPosition);
        }
    }

    public void GhostAILogic()
    {
        if (reachTargetGrid)
        {
            //reach to a new grid, process with the AI logic
            if(curState == GhostState.Chase)
            {
                switch (patternID)
                {
                    case 0:
                        BlinkyPattern();
                        break;
                    case 1:
                        PinkyPattern();
                        break;
                    case 2:
                        InkyPattern();
                        break;
                    case 3:
                        ClydePattern();
                        break;

                }
            }
            else if(curState == GhostState.Scatter)
            {
                if(reachScatterDestination)
                {
                    ScatterPositionSet posSet = scatterPos[patternID];
                    int randIndex = UnityEngine.Random.Range(0, posSet.posAnchors.Count); 
                    targetPos = posSet.posAnchors[randIndex].transform.position;
                    scatterDestination = GridInfo.instance.GetTargetPosGridCenter(targetPos);
                    reachScatterDestination = false;
                }
                else
                {
                    targetPos = scatterDestination;
                }
                
            }
            else if (curState == GhostState.Frightened)
            {
                Vector2 curPos = GridInfo.instance.GetTargetPosGridCenter(transform.position);
                Vector2 fleePos = GridInfo.instance.GetTargetPosGridCenter(pacMan.transform.position);
                if (Vector2.Distance(curPos, fleePos) < fleeRange)
                {
                    //within range then flee
                    targetPos = PathFinder.instance.FindFleePoint(curPos, fleePos, fleeRange);
                }
                else
                {
                    if (reachScatterDestination)
                    {
                        ScatterPositionSet posSet = scatterPos[patternID];
                        int randIndex = UnityEngine.Random.Range(0, posSet.posAnchors.Count);
                        targetPos = posSet.posAnchors[randIndex].transform.position;
                        scatterDestination = GridInfo.instance.GetTargetPosGridCenter(targetPos);
                        reachScatterDestination = false;
                    }
                    else
                    {
                        targetPos = scatterDestination;
                    }
                }
            }
            else if(curState == GhostState.Dead)
            {
                isReviving = true;
                targetPos = initPos;
            }

            //process the target position to align with the grid center
            targetPos = GridInfo.instance.GetTargetPosGridCenter(targetPos);
            //Find Path with the new target position
            Vector3Int targetGridPos = GridInfo.instance.WorldToGridPos(targetPos);
            Vector3Int curGridPos = GridInfo.instance.WorldToGridPos(transform.position);
            curPath = PathFinder.instance.NormalFindPath(curGridPos, targetGridPos, curDir);
            //Set the new target grid with the path calculated above
            movementNextGrid = GetNextWaypoint();
            //Set the new move direction
            curDir = (GridInfo.instance.GetTargetPosGridCenter(movementNextGrid) - GridInfo.instance.GetTargetPosGridCenter(transform.position)).normalized;
            //Set the flag to false after process
            reachTargetGrid = false;
        }
    }

    private void BlinkyPattern()
    {
        targetPos = pacMan.transform.position;
    }

    private void PinkyPattern()
    {
        Vector2 pacManDir = pacMan.GetComponent<PacMan>().curDir;
        Vector3Int pacManGridPos = GridInfo.instance.WorldToGridPos(pacMan.transform.position);
        //Check within the range of 4 grids in front of PacMan (from far to close)
        for(int i=4;i>=0;i--)
        {
            Vector3Int offset = i * new Vector3Int(Mathf.RoundToInt(pacManDir.x), Mathf.RoundToInt(pacManDir.y), 0);
            Vector3Int predictGridPos = pacManGridPos + offset;
            if (GridInfo.instance.CheckGridCellWalkable(predictGridPos))
            {
                targetPos = GridInfo.instance.GridToWorldPos(predictGridPos);
                break; //find the first walkable grid and stop
            }
        }
    }

    private void InkyPattern()
    {
        Vector2 pacManDir = pacMan.GetComponent<PacMan>().curDir;
        Vector3Int pacManGridPos = GridInfo.instance.WorldToGridPos(pacMan.transform.position);
        Vector3Int blinkyGridPos = GridInfo.instance.WorldToGridPos(PacGameManager.instance.Blinky.transform.position);
        Vector3Int predictGridPos = pacManGridPos + 2 * new Vector3Int(Mathf.RoundToInt(pacManDir.x), Mathf.RoundToInt(pacManDir.y), 0);
        int offsetX = (predictGridPos - blinkyGridPos).x;
        int offsetY = (predictGridPos - blinkyGridPos).y;
        int xStep = 0;
        int yStep = 0;
        if(offsetX > 0)
        {
            xStep = 1;
        }
        else if(offsetX < 0)
        {
            xStep = -1;
        }
        if(offsetY > 0)
        {
            yStep = 1;
        }
        else if(offsetY < 0)
        {
            yStep = -1;
        }
        bool reachable = false;
        //check if there is reachable grid cell within the offset range
        while(offsetX != 0 && offsetY != 0)
        {
            //get the reachable grid pos
            Vector3Int curGridPos = predictGridPos + new Vector3Int(offsetX, offsetY, 0);
            if (GridInfo.instance.CheckGridCellWalkable(curGridPos))
            {
                targetPos = GridInfo.instance.GridToWorldPos(curGridPos);
                reachable = true;
                break;
            }
            else
            {
                if(offsetX!= 0)
                {
                    offsetX -= xStep;
                }
                if(offsetY!= 0)
                {
                    offsetY -= yStep;
                }
            }
        }
        if(!reachable)
        {
            //when no reachable grid cells with the offset, check the front 2 cell of pacman
            if (GridInfo.instance.CheckGridCellWalkable(predictGridPos))
            {
                targetPos = GridInfo.instance.GridToWorldPos(predictGridPos);
            }
            else
            {
                //otherwise just chase the pacman
                targetPos = GridInfo.instance.GridToWorldPos(pacManGridPos);
            }
        }
    }

    private void ClydePattern()
    {
        Vector2 curPos = GridInfo.instance.GetTargetPosGridCenter(transform.position);
        Vector2 fleePos = GridInfo.instance.GetTargetPosGridCenter(pacMan.transform.position);
        if(Vector2.Distance(curPos, fleePos) < fleeRange)
        {
            //within range then flee
            targetPos = PathFinder.instance.FindFleePoint(curPos, fleePos, fleeRange);
        }
        else
        {
            //outside range then chase
            targetPos = fleePos;
        }
        
    }

    private void Revive()
    {
        //reset the speed
        speed = 2f;
        sr.color = new Color(1, 1, 1, 1);
        controlable = true;
        //reset state to current strategy
        if (PacGameManager.instance.isScattering)
        {
            curState = GhostState.Scatter;
        }
        else
        {
            curState = GhostState.Chase;
        }
        
        isReviving = false;
    }

    public void SetInControl(bool control)
    {
        inControl = control;
        dirIndicatior.SetActive(control);
        selectIndicator.SetActive(control);
        curPath = null;
    }

    public void SetAnimFrightened(bool isFrightened)
    {
        anim.SetBool("isFrightened", isFrightened);
    }

    public void SpeedUp()
    {
        speed = 4f;
        speedUpTimer = 0;
        isSpeedUp = true;
    }

}
