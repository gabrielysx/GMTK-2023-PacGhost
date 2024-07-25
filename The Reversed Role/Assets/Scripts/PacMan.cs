using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PacMan : MonoBehaviour
{
    public Vector2 curDir, nextDir;
    [SerializeField] private Vector2 targetPos;
    private Vector2 initPos;

    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool reachTargetGrid;
    public List<PathNodeBase> curPath;
    private Vector2 movementNextGrid;

    //invincible
    public bool invincible;
    [SerializeField] private List<float> invincibleTimeList;
    [SerializeField] private float invincibleTime = 5f;
    private float invincibleTimer;

    //debug
    private List<PathNodeBase> tempPathCheck = new List<PathNodeBase>();

    //debug
    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.blue;
        //Gizmos.DrawLine(transform.position, (Vector2)transform.position + (Vector2)nextDir);
        //Gizmos.color = Color.green;
        //Gizmos.DrawLine(transform.position, (Vector2)transform.position  +(Vector2)curDir);
        Gizmos.color = Color.green;
        if (tempPathCheck != null)
        {
            if (tempPathCheck.Count > 0)

            {
                Vector3Int initGridPos = GridInfo.instance.WorldToGridPos(transform.position);
                Vector3 prev = GridInfo.instance.GridToWorldPos(initGridPos);
                foreach (PathNodeBase node in tempPathCheck)
                {
                    Vector3 cur = GridInfo.instance.grid.GetCellCenterWorld(node.gridPosition);
                    Gizmos.DrawLine(prev, cur);
                    prev = cur;
                }
            }
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        targetPos = transform.position;
        initPos = transform.position;
        reachTargetGrid = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        //debug
        tempPathCheck = curPath;

        if(invincible)
        {
            anim.SetBool("isInvincible", true);
            invincibleTimer += Time.fixedDeltaTime;
            if(invincibleTimer >= invincibleTime)
            {
                invincible = false;
                anim.SetBool("isInvincible", false);
                PacGameManager.instance.SetFrightened(false);
                invincibleTimer = 0;
            }
        }

        PacManAILogic();
        MoveTowardsTargetPosition(movementNextGrid, speed);
        float angle = Mathf.Atan2(curDir.y, curDir.x) * Mathf.Rad2Deg;
        if(curDir.x <0)
        {
            angle = 0;
        }
        FaceToDirection(curDir.x);
        Quaternion rot = new Quaternion();
        rot.eulerAngles = new Vector3(0, 0, angle);
        transform.rotation = rot;
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
        }
        else
        {
            rb.MovePosition(currentPos + step);

        }
    }

    public void PacManAILogic()
    {
        if(reachTargetGrid)
        {
            //update Weight Map
            PacGameManager.instance.GenerateWeightMapBase();
            if (!invincible)
            {
                PacGameManager.instance.UpdateThreatWeight();
            }
            CoinSpawn.instance.UpdateItemWeight();

            //if invincible then set new target position to nearest ghost
            if (invincible && PacGameManager.instance.allFrightened)
            {
                bool found = false;
                targetPos = PacGameManager.instance.GetNearestGhostPos(transform.position,out found);
                if(!found)
                {
                    //set new target position to nearest coin
                    targetPos = CoinSpawn.instance.GetNearestCoinPos(transform.position);
                }
            }
            else
            {
                //set new target position to nearest coin
                targetPos = CoinSpawn.instance.GetNearestCoinPos(transform.position);
            }

            //process the target position to align with the grid center
            targetPos = GridInfo.instance.GetTargetPosGridCenter(targetPos);
            //Find Path with the new target position
            Vector3Int targetGridPos = GridInfo.instance.WorldToGridPos(targetPos);
            Vector3Int curGridPos = GridInfo.instance.WorldToGridPos(transform.position);
            curPath = PathFinder.instance.PacManFindPath(curGridPos, targetGridPos,invincible);
            //Special process when no available path for current target
            if (curPath == null)
            {
                Debug.Log("No available path for current target");
                //set new target position to farthest coin
                targetPos = CoinSpawn.instance.GetFarthestCoinPos(transform.position);
                //process the target position to align with the grid center
                targetPos = GridInfo.instance.GetTargetPosGridCenter(targetPos);
                //Find Path with the new target position
                targetGridPos = GridInfo.instance.WorldToGridPos(targetPos);
                curPath = PathFinder.instance.PacManFindPath(curGridPos, targetGridPos,invincible);
            }
            //Set the new target grid with the path calculated above
            movementNextGrid = GetNextWaypoint();
            //Set the new move direction
            curDir = (GridInfo.instance.GetTargetPosGridCenter(movementNextGrid) - GridInfo.instance.GetTargetPosGridCenter(transform.position));
            //Set the flag to false after process
            reachTargetGrid = false;
        }
    }

    private Vector2 GetNextWaypoint()
    {
        if(curPath == null)
        {
            //no path then return current position
            return GridInfo.instance.GetTargetPosGridCenter(transform.position);
        }
        if (curPath.Count <= 1)
        {
            //no path then return current position
            return GridInfo.instance.GetTargetPosGridCenter(transform.position);
        }
        else
        {
            return GridInfo.instance.GridToWorldPos(curPath[1].gridPosition);
        }
    }

    public void Dead()
    {
        PacGameManager.instance.PlaySFX(3);
        invincible = true;
        invincibleTimer = 5;
        PacGameManager.instance.HPChange(-1);
        transform.position = initPos;
        reachTargetGrid = true;
    }

    public void StartInvincible()
    {
        invincible = true;
        invincibleTimer = 0;
    }
    

    public void FaceToDirection(float xValue)
    {
        if (xValue >= 0)
        {
            sr.flipX = false;
        }
        else if (xValue < 0)
        {
            sr.flipX = true;
        }
    }
}
