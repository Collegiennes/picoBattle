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
        if (GameFlow.State == GameState.Gameplay)
        {
            string numberText, titleText;
            Vector2 numberSize, titleSize;
            float baseOffset = HudMargin;

            // ASSAULT HUE
            //numberText = ShieldGenerator.Instance.AssaultHue.HasValue ? Mathf.RoundToInt(ShieldGenerator.Instance.AssaultHue.Value).ToString() : "-";
            //numberSize = NumberStyle.CalcSize(new GUIContent(numberText));
            //titleText = "ASSAULT HUE";
            //titleSize = TitleStyle.CalcSize(new GUIContent(titleText));
            //GUI.color = ShieldGenerator.Instance.AssaultHue.HasValue ? ColorHelper.ColorFromHSV(ShieldGenerator.Instance.AssaultHue.Value, 1, 1) : Color.white;
            //GUI.Label(new Rect(Screen.width - titleSize.x - baseOffset, HudMargin, numberSize.x, numberSize.y), numberText, NumberStyle);
            //GUI.color = Color.white;
            //GUI.Label(new Rect(Screen.width - titleSize.x - baseOffset, HudMargin, titleSize.x, titleSize.y), titleText, TitleStyle);
            //baseOffset += titleSize.x + HudMargin * 1.5f;

            //// SHIELD HUE
            //numberText = ShieldGenerator.Instance.IsPowered ? Mathf.RoundToInt(ShieldGenerator.Instance.CurrentHue).ToString() : "-";
            //numberSize = NumberStyle.CalcSize(new GUIContent(numberText));
            //titleText = "SHIELD HUE";
            //titleSize = TitleStyle.CalcSize(new GUIContent(titleText));
            //GUI.color = ShieldGenerator.Instance.IsPowered ? ColorHelper.ColorFromHSV(ShieldGenerator.Instance.CurrentHue, 1, 1) : Color.white;
            //GUI.Label(new Rect(Screen.width - titleSize.x - baseOffset, HudMargin, numberSize.x, numberSize.y), numberText, NumberStyle);
            //GUI.color = Color.white;
            //GUI.Label(new Rect(Screen.width - titleSize.x - baseOffset, HudMargin, titleSize.x, titleSize.y), titleText, TitleStyle);
            //baseOffset += titleSize.x + HudMargin * 1.5f;

            // HEALTH
            //numberText = Mathf.RoundToInt(ShieldGenerator.Instance.CurrentHealth).ToString();
            //numberSize = NumberStyle.CalcSize(new GUIContent(numberText));
            //titleText = "YOUR HEALTH";
            //titleSize = TitleStyle.CalcSize(new GUIContent(titleText));
            //GUI.Label(new Rect(Screen.width - titleSize.x - baseOffset, HudMargin, numberSize.x, numberSize.y), numberText, NumberStyle);
            //GUI.Label(new Rect(Screen.width - titleSize.x - baseOffset, HudMargin, titleSize.x, titleSize.y), titleText, TitleStyle);
            //baseOffset += titleSize.x + HudMargin * 1.5f;

            // ENEMY HEALTH
            //numberText = Mathf.RoundToInt(ShieldGenerator.Instance.CurrentHealth).ToString();
            //numberSize = NumberStyle.CalcSize(new GUIContent(numberText));
            //titleText = "ENEMY HEALTH";
            //titleSize = TitleStyle.CalcSize(new GUIContent(titleText));
            //GUI.Label(new Rect(Screen.width - titleSize.x - baseOffset, HudMargin, numberSize.x, numberSize.y), numberText, NumberStyle);
            //GUI.Label(new Rect(Screen.width - titleSize.x - baseOffset, HudMargin, titleSize.x, titleSize.y), titleText, TitleStyle);

            return;
        }

        if (GameFlow.State >= GameState.Login && GameFlow.State <= GameState.Syncing)
        {
            // Connection UI
            //GUI.DrawTexture(new Rect(0, Screen.height / 2f - 100, PicoLogo.width, PicoLogo.height), PicoLogo);

            //var hostIpRect = new Rect(0, Screen.height / 2f + 25, IpField.width, IpField.height);
            //if (GameFlow.State == GameState.Login)
            //{
            //    GUI.SetNextControlName("TextBox");
            //    Networking.Instance.HostIP = GUI.TextField(new Rect(0, Screen.height / 2f + 25, IpField.width, IpField.height), Networking.Instance.HostIP, TextBoxStyle).Trim();
            //    GUI.FocusControl("TextBox");
            //}
            //else
            //    GUI.Label(new Rect(0, Screen.height / 2f + 25, IpField.width, IpField.height), Networking.Instance.HostIP, TextBoxStyle);
            //Networking.Instance.HostIP = new Regex(@"[^0-9\.]").Replace(Networking.Instance.HostIP, "");
            //var size = CaretStyle.CalcSize(new GUIContent(Networking.Instance.HostIP));

            //if (Mathf.RoundToInt(Time.timeSinceLevelLoad * 2) % 2 == 0 && GameFlow.State == GameState.Login)
            //    GUI.Label(new Rect(hostIpRect.left + size.x - 20, hostIpRect.top, 100, 100), "|", CaretStyle);

            //var hostText = "HOST";
            //if (Networking.Instance.HostIP.Length != 0)
            //    hostText = "CONNECT";

            //if (GameFlow.State == GameState.Login && GUI.Button(new Rect(0, Screen.height / 2f + 80, ConnectButton.width, ConnectButton.height), hostText, ButtonStyle) || Event.current.character == '\n')
            //    GameFlow.State = GameState.ReadyToConnect;
            //else
            //{
            //    switch (GameFlow.State)
            //    {
            //        case GameState.WaitingOrConnecting:
            //            hostText = Networking.Instance.IsServer ? "WAITING..." : "CANCEL";
            //            if (Networking.Instance.IsServer)
            //                GUI.Label(new Rect(0, Screen.height / 2f + 80, ConnectButton.width, ConnectButton.height), hostText, ButtonStyle);
            //            else
            //            {
            //                if (GUI.Button(new Rect(0, Screen.height / 2f + 80, ConnectButton.width, ConnectButton.height), "CANCEL", ButtonStyle))
            //                    GameFlow.State = GameState.Login;
            //            }
            //            break;
            //        case GameState.ReadyToPlay:
            //            if (GUI.Button(new Rect(0, Screen.height / 2f + 80, ConnectButton.width, ConnectButton.height), "CLICK TO START", ButtonStyle) || Event.current.character == '\n')
            //            {
            //                Networking.TellReady();
            //                GameFlow.State = GameState.Syncing;
            //            }
            //            break;
            //        case GameState.Syncing:
            //            hostText = "WAITING...";
            //            GUI.Label(new Rect(0, Screen.height / 2f + 80, ConnectButton.width, ConnectButton.height), hostText, ButtonStyle);
            //            break;
            //    }
            //}

            //var connectText = "PRESS ENTER\nTO CONNECT";
            //if (Networking.Instance.HostIP.Length == 0)
            //    connectText = "HOST OR\nTYPE AN IP";
            //if (GameFlow.State == GameState.WaitingOrConnecting)
            //    connectText = Networking.Instance.IsServer ? "LOCAL : " + Networking.Instance.LanIP + "\nINTERNET : " + Networking.Instance.WanIP : "CONNECTING...";

            //GUI.Label(new Rect(-25, Screen.height / 2f + 25, IpField.width, IpField.height), connectText, LittleStyle);
        }

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

        Wait.Until(t => t >= 5, () =>
        {
            // TODO : Reset playfield
        });
    }

    public void OnWin()
    {
        GameFlow.State = GameState.Won;

        Wait.Until(t => t >= 5, () =>
        {
            // TODO : Reset playfield
        });
    }
}
