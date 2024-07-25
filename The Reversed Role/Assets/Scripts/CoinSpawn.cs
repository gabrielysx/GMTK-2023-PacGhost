using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CoinSpawn : MonoBehaviour
{
    public static CoinSpawn instance;
    [SerializeField] private GameObject coinPrefab, specialPropPrefab, fruitPrefab;
    [SerializeField] private Tilemap notSpawnableTiles;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float fruitInterval = 15f;
    [SerializeField] private int spawnNumbers = 50;
    [SerializeField] private List<GameObject> specialAnchors;
    [SerializeField] private List<GameObject> fruitSpawnPosition;
    [SerializeField] private Transform fruitHolder;

    private List<bool> isSpawnedFruit = new List<bool>();
    private List<Vector3Int> specialAnchorsPos;
    private List<Vector3Int> spawnableGridPos;
    private List<GameObject> itemInGrid;
    private List<bool> haveItem, isSpecial;
    private float spawnTimer,fruitTimer;
    private int spawnCount;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        //initialize variables
        spawnableGridPos = new List<Vector3Int>();
        specialAnchorsPos = new List<Vector3Int>();
        itemInGrid = new List<GameObject>();
        haveItem = new List<bool>();
        isSpecial = new List<bool>();
        //get the walkable tiles bound
        Vector2 minPoint, maxPoint;
        GridInfo.instance.GetWalkableTilesBound(out minPoint, out maxPoint);
        Vector3Int gridMin = GridInfo.instance.WorldToGridPos(minPoint);
        Vector3Int gridMax = GridInfo.instance.WorldToGridPos(maxPoint);
        //calculate the special anchors grid position
        foreach (GameObject anchor in specialAnchors)
        {
            Vector3Int gridPos = GridInfo.instance.WorldToGridPos(anchor.transform.position);
            specialAnchorsPos.Add(gridPos);
        }

        //get all the spawnable grid pos, and spawn different items
        for (int i = gridMin.x; i <= gridMax.x; i++)
        {
            for (int j = gridMin.y; j <= gridMax.y; j++)
            {
                Vector3Int gridPos = new Vector3Int(i, j, 0);
                if (GridInfo.instance.CheckGridCellWalkable(gridPos))
                {
                    if (notSpawnableTiles.GetTile(gridPos) == null)
                    {
                        //add spawnable position to the list
                        spawnableGridPos.Add(gridPos);
                        //check if the position for special spawns
                        if (specialAnchorsPos.Contains(gridPos))
                        {
                            //spawn special prop
                            itemInGrid.Add(Instantiate(specialPropPrefab, GridInfo.instance.GridToWorldPos(gridPos), Quaternion.identity, gameObject.transform));
                            haveItem.Add(true);
                            isSpecial.Add(true);
                        }
                        else
                        {
                            //spawn coins with 80% chance
                            float rand = Random.value;
                            if (rand >= 0.2f)
                            {
                                itemInGrid.Add(Instantiate(coinPrefab, GridInfo.instance.GridToWorldPos(gridPos), Quaternion.identity, gameObject.transform));
                                haveItem.Add(true);
                                isSpecial.Add(false);
                            }
                            else
                            {
                                itemInGrid.Add(null);
                                haveItem.Add(false);
                                isSpecial.Add(false);
                            }
                        }
                    }
                }
            }
        }

        //set up isSpawnedFruit
        for (int i = 0; i < fruitSpawnPosition.Count; i++)
        {
            isSpawnedFruit.Add(false);
        }

        //refresh the coin number
        PacGameManager.instance.PointsUpdate();

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        fruitTimer += Time.fixedDeltaTime;
        if (fruitTimer > fruitInterval)
        {
            fruitTimer = 0;
            SpawnFruit();
        }

        if (spawnCount < spawnNumbers)
        {
            spawnTimer += Time.fixedDeltaTime;
            if (spawnTimer > spawnInterval)
            {
                spawnTimer = 0;
                SpawnCoin();
                spawnCount++;
            }
        }

    }

    private void SpawnCoin()
    {
        List<int> notFullIndexes = new List<int>();
        for (int i = 0; i < haveItem.Count; i++)
        {
            //get the not full slots
            if (!haveItem[i])
            {
                //when a slot not full then add to the list
                notFullIndexes.Add(i);
            }
        }
        //when no slots available just return
        if (notFullIndexes.Count == 0)
        {
            return;
        }
        else
        {
            //spawn in a random available slot
            //shuffle the list
            int rand = Random.Range(0, notFullIndexes.Count);
            int targetIndex = notFullIndexes[rand];
            //spawn the coin
            itemInGrid[targetIndex] = Instantiate(coinPrefab,
                GridInfo.instance.GridToWorldPos(spawnableGridPos[targetIndex]),
                Quaternion.identity, gameObject.transform);
            haveItem[targetIndex] = true;
            isSpecial[targetIndex] = false;
        }
        PacGameManager.instance.PointsUpdate();
    }

    private void SpawnFruit()
    {
        List<int> availableSlot = new List<int>();
        for (int i = 0; i < isSpawnedFruit.Count; i++)
        {
            if (!isSpawnedFruit[i])
            {
                availableSlot.Add(i);
            }
        }

        if (availableSlot.Count == 0)
        {
            return;
        }
        else
        {
            int rand = Random.Range(0, availableSlot.Count);
            int targetIndex = availableSlot[rand];
            Vector2 spawnPos = fruitSpawnPosition[targetIndex].transform.position;
            spawnPos = GridInfo.instance.GetTargetPosGridCenter(spawnPos);
            GameObject fruit = Instantiate(fruitPrefab,
                               spawnPos,Quaternion.identity, fruitHolder);
            isSpawnedFruit[targetIndex] = true;
            fruit.GetComponent<FruitProp>().SetRandomFruitIcon();
            fruit.GetComponent<FruitProp>().fruitID = targetIndex;
        }
    }

    public void RemoveFruit(int fruitID)
    {
        isSpawnedFruit[fruitID] = false;
    }

    public void UpdateItemWeight()
    {
        for (int i = 0; i < spawnableGridPos.Count; i++)
        {
            if (haveItem[i])
            {
                if (isSpecial[i])
                {
                    //special props reduce more weight to attract pacman
                    PacGameManager.instance.UpdateWeightMapValue(spawnableGridPos[i], -40);
                }
                else
                {
                    //coins reduce a bit weight to attract pacman
                    PacGameManager.instance.UpdateWeightMapValue(spawnableGridPos[i], -5);
                }
            }
        }
        for (int i = 0;i < isSpawnedFruit.Count;i++)
        {
            if (isSpawnedFruit[i])
            {
                //fruit reduce some weight to attract pacman
                Vector3Int pos = GridInfo.instance.WorldToGridPos(fruitSpawnPosition[i].transform.position);
                PacGameManager.instance.UpdateWeightMapValue(pos, -30);
            }
        }
    }

    public Vector2 GetNearestCoinPos(Vector2 startPos)
    {
        float minDis = float.MaxValue;
        Vector2 output = startPos;
        for (int i = 0; i < spawnableGridPos.Count; i++)
        {
            if (haveItem[i])
            {
                float temp = Vector2.Distance(startPos, GridInfo.instance.GridToWorldPos(spawnableGridPos[i]));
                if (temp < minDis)
                {
                    minDis = temp;
                    output = GridInfo.instance.GridToWorldPos(spawnableGridPos[i]);
                }
                else if(temp == minDis)
                {
                    //if the distance is the same, choose with the current weight map
                    float oldWeight = PacGameManager.instance.GetWeightMapValue(GridInfo.instance.WorldToGridPos(output));
                    float newWeight = PacGameManager.instance.GetWeightMapValue(spawnableGridPos[i]);
                    if (oldWeight > newWeight) //choose the one with lower weight
                    {
                        output = GridInfo.instance.GridToWorldPos(spawnableGridPos[i]);
                    }
                }
            }
        }
        return output;
    }

    public Vector2 GetFarthestCoinPos(Vector2 startPos)
    {
        float maxDis = float.MinValue;
        Vector2 output = startPos;
        for (int i = 0; i < spawnableGridPos.Count; i++)
        {
            if (haveItem[i])
            {
                float temp = Vector2.Distance(startPos, GridInfo.instance.GridToWorldPos(spawnableGridPos[i]));
                if (temp >= maxDis)
                {
                    maxDis = temp;
                    output = GridInfo.instance.GridToWorldPos(spawnableGridPos[i]);
                }
            }
        }
        return output;
    }

    public int GetCoinCount()
    {
        int count = 0;
        for (int i = 0; i < haveItem.Count; i++)
        {
            if (haveItem[i] && !isSpecial[i])
            {
                count++;
            }
        }
        return count;
    }

    public void RemoveCoinFromTheList(GameObject coin)
    {
        int i = itemInGrid.IndexOf(coin);
        itemInGrid[i] = null;
        haveItem[i] = false;
        isSpecial[i] = false;
    }

    public void RemovePropFromTheList(GameObject prop)
    {
        int i = itemInGrid.IndexOf(prop);
        itemInGrid[i] = null;
        haveItem[i] = false;
        isSpecial[i] = false;
    }
}
