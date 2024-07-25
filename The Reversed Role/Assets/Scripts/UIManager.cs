using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public List<GameObject> heartIcon;
    public TMP_Text pointsText;
    public GameObject defeatPanel, victoryPanel,pausePanel;

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

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateHP(int curHP)
    {
        for (int i = 0; i < heartIcon.Count; i++)
        {
            if (i < curHP)
            {
                heartIcon[i].SetActive(true);
            }
            else
            {
                heartIcon[i].SetActive(false);
            }
        }
    }

    public void UpdateScore(int curScore)
    {
        pointsText.text = curScore.ToString();
    }

    public void GameOver(bool ifWin)
    {
        Time.timeScale = 0;
        if (ifWin)
        {
            victoryPanel.SetActive(true);
        }
        else
        {
            defeatPanel.SetActive(true);
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void PauseGame(bool ifPause)
    {
        if (ifPause)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
        pausePanel.SetActive(ifPause);
    }

}
