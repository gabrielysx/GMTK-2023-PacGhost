using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour
{
    public void MuteSound(Toggle toggle)
    {
        PacGameManager.instance.gameObject.GetComponent<AudioSource>().mute = toggle.isOn;
    }

    public void GhostCanTurnBack(Toggle toggle)
    {
        PacGameManager.instance.ifGhostsCanTurnBack = toggle.isOn;
    }

}
