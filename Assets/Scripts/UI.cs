using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class UI : MonoBehaviour
{
    public GUIStyle TitleStyle, NumberStyle;
    public int HudMargin;

    public Texture2D ConnectButton, Glow, IpField, PicoLogo;
    public GUIStyle TextBoxStyle, ButtonStyle, LittleStyle;
    GUIStyle CaretStyle;

    void Start()
    {
        CaretStyle = new GUIStyle(TextBoxStyle);
        CaretStyle.normal.background = null;
        CaretStyle.normal.textColor = new Color(180 / 255f, 178 / 255f, 185 / 255f, 1f);
        CaretStyle.fixedWidth = 0;
    }
    
    void OnGUI()
    {
        if (GameFlow.State >= GameState.Won)
        {
            var text = GameFlow.State == GameState.Won ? "YOU WON" : "YOU LOST";
            var size = NumberStyle.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(Screen.width / 2f - size.x / 2, Screen.height / 2f - size.y / 2, size.x, size.y), text, NumberStyle);
        }
    }

    public void OnDie()
    {
        GameFlow.State = GameState.Lost;

        Wait.Until(t => t >= 5, Networking.Instance.Reset);
    }

    public void OnWin()
    {
        GameFlow.State = GameState.Won;

        Wait.Until(t => t >= 5, Networking.Instance.Reset);
    }
}
