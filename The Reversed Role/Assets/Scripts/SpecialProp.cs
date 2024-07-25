using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialProp : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PacMan"))
        {
            CoinSpawn.instance.RemovePropFromTheList(gameObject);
            collision.gameObject.GetComponent<PacMan>().StartInvincible();
            PacGameManager.instance.SetFrightened(true);
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
