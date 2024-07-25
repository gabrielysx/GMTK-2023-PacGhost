using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldCoin : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PacMan"))
        {
            if (!PacGameManager.instance.GetIfPlayingSFX())
            {
                PacGameManager.instance.PlaySFX(1);
            }
            CoinSpawn.instance.RemoveCoinFromTheList(gameObject);
            PacGameManager.instance.PointsUpdate();
            //disable the collider and destroy the game object
            gameObject.GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject);
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
}
