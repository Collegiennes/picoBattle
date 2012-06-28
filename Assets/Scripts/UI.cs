using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class UI : MonoBehaviour
{
    public GameObject WinEffect, LoseEffect;

    public void OnDie()
    {
        GameFlow.State = GameState.Lost;

        var go = Instantiate(LoseEffect);
        
        Wait.Until(t => t >= 5, Networking.Instance.Reset);
    }

    public void OnWin()
    {
        GameFlow.State = GameState.Won;

        Instantiate(WinEffect);

        Wait.Until(t => t >= 5, Networking.Instance.Reset);
    }
}
