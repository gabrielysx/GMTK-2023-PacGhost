using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacGameManager : MonoBehaviour
{
    public static PacGameManager instance;
    public List<GameObject> ghosts;
    public GameObject Blinky, Pinky, Inky, Clyde;
    public GameObject pacMan;
    public bool ifGhostsCanTurnBack;
    public bool allFrightened;
    private Vector3Int minGrid, maxGrid;
    [SerializeField] private int curControlIndex;
    [SerializeField] public float[,] weightMap;

    //Input
    private bool inputCooldown;
    private float inputCooldownTimer;
    [SerializeField] private float inputCDtime = 1f;

    //gameloop
    private int curPointsLeft, curHPLeft;
    public bool isScattering;
    [SerializeField] private float scatterTime = 7f, chaseTime = 20f;
    private float scatterTimer,chaseTimer;

    //SFX
    [SerializeField] private List<AudioClip> sfxList = new List<AudioClip>();
    [SerializeField] private AudioSource sfxSource;

    private readonly float[] threatWeight = new float[] { 20000, 2000, 500, 100, 50, 40 };
    private readonly Vector3Int[] dirOffset = new Vector3Int[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.cyan;
        //Gizmos.DrawLine(GridInfo.instance.GridToWorldPos(minGrid), GridInfo.instance.GridToWorldPos(maxGrid));
        for (int x = 0; x < weightMap.GetLength(0); x++)
        {
            for (int y = 0; y < weightMap.GetLength(1); y++)
            {
                float t = Remap(weightMap[x, y], -50, 200, 0, 1);
                Gizmos.color = new Color(1, t, 0);
                Gizmos.DrawCube(GridInfo.instance.GridToWorldPos(new Vector3Int(minGrid.x + x, minGrid.y + y, 0)), Vector3.one * 1f);
                
            }
        }
    }
    public float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    // Start is called before the first frame update
    void Start()
    {
        PlaySFX(0);
        GenerateWeightMapBase();
        curControlIndex = 0;
        ControlTargetGhost(curControlIndex);
        curHPLeft = 3;
        UIManager.instance.UpdateHP(curHPLeft);
    }

    // Update is called once per frame
    void Update()
    {
        if (!inputCooldown)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                curControlIndex = curControlIndex - 1;
                if (curControlIndex < 0)
                {
                    curControlIndex = ghosts.Count - 1;
                }
                ControlTargetGhost(curControlIndex);
                inputCooldown = true;
                inputCooldownTimer = 0;
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                curControlIndex = (curControlIndex + 1) % ghosts.Count;
                ControlTargetGhost(curControlIndex);
                inputCooldown = true;
                inputCooldownTimer = 0;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIManager.instance.PauseGame(!UIManager.instance.pausePanel.activeInHierarchy);
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                foreach (var ghost in ghosts)
                {
                    if (GridInfo.instance.WorldToGridPos(ghost.transform.position) == GridInfo.instance.WorldToGridPos(mousePos))
                    {
                        curControlIndex = ghosts.IndexOf(ghost);
                        ControlTargetGhost(curControlIndex);
                        inputCooldown = true;
                        inputCooldownTimer = 0;
                        break;
                    }
                }
            }
        }
        else
        {
            inputCooldownTimer += Time.unscaledDeltaTime;
            if (inputCooldownTimer > inputCDtime)
            {
                inputCooldown = false;
            }
        }
    }

    private void FixedUpdate()
    {
        StrategicUpdate();
    }

    private void StrategicUpdate()
    {
        if (isScattering)
        {
            scatterTimer += Time.fixedDeltaTime;
            if (scatterTimer > scatterTime)
            {
                foreach(var ghost in ghosts)
                {
                    if (ghost.GetComponent<Ghost>().GetCurState() == GhostState.Scatter )
                    {
                        ghost.GetComponent<Ghost>().SetCurState(GhostState.Chase);
                    }
                }
                isScattering = false;
                scatterTimer = 0;
            }
        }
        else
        {
            chaseTimer += Time.fixedDeltaTime;
            if (chaseTimer > chaseTime)
            {
                foreach (var ghost in ghosts)
                {
                    if (ghost.GetComponent<Ghost>().GetCurState() == GhostState.Chase)
                    {
                        ghost.GetComponent<Ghost>().SetCurState(GhostState.Scatter);
                    }
                }
                isScattering = true;
                chaseTimer = 0;
            }
        }
    }

    public void GenerateWeightMapBase()
    {
        Vector2 minP, maxP;
        GridInfo.instance.GetWalkableTilesBound(out minP, out maxP);
        minGrid = GridInfo.instance.WorldToGridPos(minP);
        maxGrid = GridInfo.instance.WorldToGridPos(maxP);
        int xRange = maxGrid.x - minGrid.x;
        int yRange = maxGrid.y - minGrid.y;
        weightMap = new float[xRange, yRange];
        for (int x = 0; x < weightMap.GetLength(0); x++)
        {
            for (int y = 0; y < weightMap.GetLength(1); y++)
            {
                weightMap[x, y] = 0;
            }
        }
    }

    public void UpdateWeightMapValue(Vector3Int gridPos, float valueChange)
    {
        int x = gridPos.x - minGrid.x;
        int y = gridPos.y - minGrid.y;
        weightMap[x, y] += valueChange;
    }

    public float GetWeightMapValue(Vector3Int gridPos)
    {
        int x = gridPos.x - minGrid.x;
        int y = gridPos.y - minGrid.y;
        if (GridInfo.instance.CheckGridCellWalkable(gridPos))
        {
            //Debug.Log("Valid weight! Value: " + weightMap[x, y].ToString());
            return weightMap[x, y];
        }
        else
        {
            return 0;
        }

    }

    public void UpdateThreatWeight()
    {
        foreach (GameObject ghost in ghosts)
        {
            Ghost ghostController = ghost.GetComponent<Ghost>();
            if (ghostController.GetCurState() != GhostState.Dead)
            {
                //when not dead, calculate the threat weight
                //first deal with 4 directions grids within 5 grid range
                Vector3Int ghostGridPos = GridInfo.instance.WorldToGridPos(ghost.transform.position);
                for (int i = 1; i <= 5; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Vector3Int curGrid = ghostGridPos + dirOffset[j] * i;
                        //only update those walkable tiles
                        if (GridInfo.instance.CheckPacManGridCellWalkable(curGrid, pacMan.GetComponent<PacMan>().invincible))
                        {
                            UpdateWeightMapValue(curGrid, threatWeight[i]);
                        }
                    }
                }
                //second deal with next five steps of the cur path
                for (int i = 0; i <= 5; i++)
                {
                    if (ghostController.curPath != null)
                    {
                        if (i < ghostController.curPath.Count)
                        {
                            Vector3Int curGrid = ghostController.curPath[i].gridPosition;
                            UpdateWeightMapValue(curGrid, threatWeight[i]);
                        }
                    }
                }
            }
        }
    }

    public void ControlTargetGhost(int index)
    {
        for (int i = 0; i < ghosts.Count; i++)
        {
            Ghost gController = ghosts[i].GetComponent<Ghost>();
            if (i == index)
            {
                gController.SetInControl(true);
            }
            else
            {
                gController.SetInControl(false);
            }
        }
    }

    public void TransferToNextControll()
    {
        curControlIndex = (curControlIndex + 1) % ghosts.Count;
        ControlTargetGhost(curControlIndex);
    }

    public void PointsUpdate()
    {
        curPointsLeft = CoinSpawn.instance.GetCoinCount();
        UIManager.instance.UpdateScore(curPointsLeft);
        if (curPointsLeft <= 0)
        {
            //game over
            UIManager.instance.GameOver(false);
        }
    }

    public void HPChange(int change)
    {
        curHPLeft += change;
        if (curHPLeft > 3)
        {
            curHPLeft = 3;
        }
        UIManager.instance.UpdateHP(curHPLeft);
        if (curHPLeft <= 0)
        {
            //game over
            UIManager.instance.GameOver(true);
        }
    }

    public void SetFrightened(bool frightened)
    {
        foreach (GameObject ghost in ghosts)
        {
            if (ghost.GetComponent<Ghost>().GetCurState() != GhostState.Dead)
            {
                if (frightened)
                {

                    ghost.GetComponent<Ghost>().SetCurState(GhostState.Frightened);
                    ghost.GetComponent<Ghost>().SetAnimFrightened(true);
                    allFrightened = true;

                }
                else
                {
                    if(isScattering)
                    {
                        ghost.GetComponent<Ghost>().SetCurState(GhostState.Scatter);
                    }
                    else
                    {
                        ghost.GetComponent<Ghost>().SetCurState(GhostState.Chase);
                    }
                    
                    ghost.GetComponent<Ghost>().SetAnimFrightened(false);
                    allFrightened = false;
                }
            }


        }
    }

    public Vector2 GetNearestGhostPos(Vector2 pos, out bool notFound)
    {
        bool valid = false;
        Vector2 nearestPos = Vector2.zero;
        float minDist = float.MaxValue;
        for (int i = 0; i < ghosts.Count; i++)
        {
            if (ghosts[i].GetComponent<Ghost>().GetCurState() == GhostState.Frightened)
            {
                float curDist = Vector2.Distance(pos, ghosts[i].transform.position);
                if (curDist < minDist)
                {
                    minDist = curDist;
                    nearestPos = ghosts[i].transform.position;
                    valid = true;
                }
            }

        }
        notFound = valid;
        return nearestPos;
    }

    public void PlaySFX(int index)
    {
        sfxSource.PlayOneShot(sfxList[index]);
    }

    public bool GetIfPlayingSFX()
    {
        return sfxSource.isPlaying;
    }

}
