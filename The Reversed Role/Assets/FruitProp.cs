using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitProp : MonoBehaviour
{
    [SerializeField] private List<Sprite> fruitsIcon;
    [SerializeField] private SpriteRenderer sr;

    public int fruitID;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PacMan"))
        {
            CoinSpawn.instance.RemoveFruit(fruitID);
            PacGameManager.instance.PlaySFX(4);
            //Add HP to PacMan
            PacGameManager.instance.HPChange(1);
            //disable the collider and destroy the game object
            gameObject.GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Ghost"))
        {
            if(collision.gameObject.GetComponent<Ghost>().GetCurState() != GhostState.Dead)
            {
                CoinSpawn.instance.RemoveFruit(fruitID);
                PacGameManager.instance.PlaySFX(4);
                //speed up the ghost
                collision.gameObject.GetComponent<Ghost>().SpeedUp();
                //disable the collider and destroy the game object
                gameObject.GetComponent<Collider2D>().enabled = false;
                Destroy(gameObject);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetRandomFruitIcon()
    {
        int rand = Random.Range(0,fruitsIcon.Count);
        sr.sprite = fruitsIcon[rand];
    }
}
