using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

class Networking : MonoBehaviour
{
    public const int Port = 10000;

    public static Networking Instance;

    public string WanIP;
    public string LanIP;
    public bool IsServer;
    public string HostIP;
    public bool ServerReady, ClientReady;

    string errorMessage;

    public GameObject BulletTemplate;

    void Start()
    {
        Instance = this;
        //WanIP = GetIP();
        LanIP = Dns.GetHostAddresses(Dns.GetHostName()).First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();
    }

    static string GetIP()
    {
        try
        {
            string strIP;
            using (var wc = new WebClient())
            {
                strIP = wc.DownloadString("http://checkip.dyndns.org");
                strIP = (new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")).Match(strIP).Value;
            }
            return strIP;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void RpcShootBullet(float power, float hue)
    {
        Instance.networkView.RPC("ShootBullet", RPCMode.Others, Instance.IsServer, power, hue);
    }

    public static void RpcUpdateShield(float hue, float health)
    {
        Instance.networkView.RPC("UpdateShield", RPCMode.Others, Instance.IsServer, hue, health);
    }

    public static void TellReady()
    {
        if (Instance.IsServer) Instance.ServerReady = true; else Instance.ClientReady = true;
        Instance.networkView.RPC("MarkReady", RPCMode.Others, Instance.IsServer);
    }

    [RPC]
    public void MarkReady(bool server)
    {
        if (server) ServerReady = true; else ClientReady = true;
    }

    float time;

    public void Update()
    {
        switch (GameFlow.State)
        {
            case GameState.ReadyToConnect:
                if (HostIP.Length == 0)
                    IsServer = true;

                if (IsServer)
                {
                    CreateServer();
                    //GameFlow.State = GameState.WaitingOrConnecting;
                    GameFlow.State = GameState.Gameplay;
                }
                else
                {
                    ConnectToServer();
                }
                break;

            case GameState.Syncing:
                if (ServerReady && ClientReady)
                {
                    GameFlow.State = GameState.Gameplay;
                }
                break;
        }

        if (GameFlow.State == GameState.Gameplay)
        {
            if (ShieldGenerator.Instance.IsAI)
            {
                time += Time.deltaTime;
                if (time > 5)
                {
                    AI();
                    time = 0;
                }
            }
        }
    }

    void AI()
    {
        Debug.Log("Shot bullet");
        ShootBullet(false, Random.value * 3 + 1, Random.value * 360);
    }

    #region Server

    void CreateServer()
    {
        var result = Network.InitializeServer(2, Port, false);
        if (result == NetworkConnectionError.NoError)
        {
            //PeerType = NetworkPeerType.Server;
        }
        else
        {
            //PeerType = NetworkPeerType.Disconnected;
            errorMessage = "Couldn't create server (reason : " + result + ")";
            Debug.Log(errorMessage);
        }
    }

    void OnPlayerConnected(NetworkPlayer player)
    {
        GameFlow.State = GameState.ReadyToPlay;

        Debug.Log("Player connected : game can start!");
    }

    void OnPlayerDisconnected(NetworkPlayer player)
    {
        Network.RemoveRPCs(player);
        Network.DestroyPlayerObjects(player);

        Application.Quit();
    }

    #endregion

    #region Client

    void ConnectToServer()
    {
        Debug.Log("connection attempt will start");

        var result = Network.Connect(HostIP, Port);
        if (result == NetworkConnectionError.NoError)
        {
            //PeerType = NetworkPeerType.Connecting;
        }
        else
        {
            //PeerType = NetworkPeerType.Disconnected;
            errorMessage = "Couldn't connect to server (reason : " + result + ") -- will retry in 2 seconds...";
            Wait.Until(t => t >= 2, () => { if (GameFlow.State == GameState.WaitingOrConnecting) GameFlow.State = GameState.ReadyToConnect; });
            Debug.Log(errorMessage);
        }

        GameFlow.State = GameState.WaitingOrConnecting;
    }

    void OnFailedToConnect(NetworkConnectionError error)
    {
        //PeerType = NetworkPeerType.Disconnected;
        errorMessage = "Couldn't connect to server (reason : " + error + ") -- will retry in 2 seconds...";
        Debug.Log(errorMessage);

        Wait.Until(t => t >= 2, () => { if (GameFlow.State == GameState.WaitingOrConnecting) GameFlow.State = GameState.ReadyToConnect; });
    }

    void OnConnectedToServer()
    {
        //PeerType = NetworkPeerType.Client;
        GameFlow.State = GameState.ReadyToPlay;
        Debug.Log("Connection successful : game starting!");
    }

    void OnDisconnectedFromServer(NetworkDisconnection info)
    {
        //PeerType = NetworkPeerType.Disconnected;
        errorMessage = "Disconnected from server (reason : " + info + ")";
        // TODO : prompt for reconnection?

        Application.Quit();
    }

    #endregion

    [RPC]
    public void ShootBullet(bool fromServer, float power, float hue)
    {
        if (fromServer == IsServer)
            return;

        var go = Instantiate(BulletTemplate) as GameObject;
        var enemy = go.GetComponent<EnemyBullet>();
        enemy.Hue = hue;
        enemy.Power = power;

        ShieldGenerator.Instance.DefendingAgainst.Add(enemy);
    }

    public static void RpcEndGame()
    {
        Instance.networkView.RPC("EndGame", RPCMode.Others, Instance.IsServer);
    }

    [RPC]
    public void EndGame(bool fromServer)
    {
        if (fromServer == IsServer)
            return;

        BroadcastMessage("OnWin", SendMessageOptions.DontRequireReceiver);
    }

    [RPC]
    public void UpdateShield(float hue, float health)
    {
        BroadcastMessage("UpdateEnemyShield", new Vector2(hue, health), SendMessageOptions.DontRequireReceiver);
    }
}
