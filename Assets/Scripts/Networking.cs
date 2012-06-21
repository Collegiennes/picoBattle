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
    public bool LocalMode;

    string errorMessage;

    float? sinceAiSawShieldUpdate, aiOffenseReactionTime, aiHueToCounter, aiLastSeenShieldHue, aiLastSeenBulletHue;
    float sinceAiShot, aiShootCooldown, aiBulletSize;
    float aiAssaultHue;

    public GameObject BulletTemplate;

    float enemyHealth, currentEnemyHealth;
    public float EnemyHealth
    {
        get { return currentEnemyHealth; }
        set
        {
            enemyHealth = value;
            if (currentEnemyHealth == 0)
                currentEnemyHealth = value;
        }
    }

    float? enemyShieldHue;
    float currentEnemyShieldHue;
    public float? EnemyShieldHue
    {
        get { return enemyShieldHue.HasValue ? currentEnemyShieldHue : (float?)null; }
        set
        {
            if (!enemyShieldHue.HasValue && value.HasValue)
                currentEnemyShieldHue = value.Value;

            enemyShieldHue = value;
        }
    }

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
        Instance.networkView.RPC("ShootBullet", RPCMode.All, Instance.IsServer, power, hue);
    }

    public static void RpcUpdateShieldHue(float hue)
    {
        Instance.networkView.RPC("UpdateEnemyShieldHue", RPCMode.Others, hue);

        if (Instance.LocalMode)
        {
            if (!Instance.aiLastSeenShieldHue.HasValue || (DotHues(hue, Instance.aiLastSeenShieldHue.Value) < 0.9f))
            {
                Instance.aiOffenseReactionTime = Random.Range(2, 10);
                Debug.Log("[AI] will react to shield change in " + Instance.aiOffenseReactionTime.Value + " seconds");
                Instance.sinceAiSawShieldUpdate = 0;
                Instance.aiHueToCounter = (hue + 180) % 360;

                Instance.aiLastSeenShieldHue = hue;
            }
            else
                Debug.Log("[AI] ignored shield change, too similar to last one");
        }
    }

    static float DotHues(float h1, float h2)
    {
        var v1 = new Vector2(Mathf.Cos(Mathf.Deg2Rad * h1), Mathf.Sin(Mathf.Deg2Rad * h1));
        var v2 = new Vector2(Mathf.Cos(Mathf.Deg2Rad * h2), Mathf.Sin(Mathf.Deg2Rad * h2));

        return Vector2.Dot(v1, v2);
    }

    public static void RpcUpdateHealth(float health)
    {
        Instance.networkView.RPC("UpdateEnemyHealth", RPCMode.Others, health);
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

    void FixedUpdate()
    {
        currentEnemyHealth = Mathf.Lerp(currentEnemyHealth, enemyHealth, 0.1f);

        if (enemyShieldHue.HasValue)
            currentEnemyShieldHue = Mathf.LerpAngle(currentEnemyShieldHue, enemyShieldHue.Value, 0.1f);
    }

    void Update()
    {
        switch (GameFlow.State)
        {
            case GameState.ReadyToConnect:
                if (HostIP.Length == 0)
                    IsServer = true;

                if (LocalMode)
                {
                    EnemyHealth = 500;
                    EnemyShieldHue = null;

                    // Set AI initial state
                    ConditionalBehaviour.KillSwitch();
                    aiShootCooldown = 10;
                    aiAssaultHue = Random.Range(0, 360);
                    aiShootCooldown = Random.Range(7, 15);
                    aiLastSeenShieldHue = null;
                    aiLastSeenBulletHue = null;
                    aiBulletSize = 1;

                    aiHueToCounter = Random.Range(0, 360);
                    aiOffenseReactionTime = Random.Range(5, 15);

                    Debug.Log("[AI] will power up shield in " + aiOffenseReactionTime.Value + " seconds");
                }

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

            case GameState.Gameplay:
                if (LocalMode)
                    AI();

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    GameFlow.State = GameState.Login;
                    ShieldGenerator.Instance.Reset();
                    Cannon.Instance.Reset();
                    Placement.Instance.Reset();
                    MousePicking.Instance.Reset();
                }
                break;
        }
    }

    void AI()
    {
        if (sinceAiSawShieldUpdate.HasValue && aiHueToCounter.HasValue)
        {
            sinceAiSawShieldUpdate += Time.deltaTime;
            if (sinceAiSawShieldUpdate.Value > aiOffenseReactionTime)
            {
                aiAssaultHue = RandomHelper.Between(aiHueToCounter.Value - 90, aiHueToCounter.Value + 90) % 360;
                if (aiAssaultHue < 0) aiAssaultHue += 360;
                Debug.Log("[AI] reacting to shield change with " + aiAssaultHue);
                Instance.aiBulletSize = Random.Range(1, 3);
                Instance.aiShootCooldown = Random.Range(Instance.aiShootCooldown, 10);
                aiHueToCounter = null;
                sinceAiSawShieldUpdate = null;
            }
        }

        sinceAiShot += Time.deltaTime;
        if (sinceAiShot > aiShootCooldown)
        {
            aiBulletSize = Mathf.Clamp(aiBulletSize + Random.value, 1, 4);
            aiShootCooldown = Random.Range(5, aiShootCooldown);

            ShootBullet(false, aiBulletSize, aiAssaultHue);
            sinceAiShot = 0;
        }
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
        if (LocalMode && fromServer)
        {
            if (!aiLastSeenBulletHue.HasValue || (DotHues(hue, aiLastSeenBulletHue.Value) < 0.9f))
            {
                var defenseReactionTime = Random.Range(5, 20);
                Debug.Log("[AI] will react to bullet hue " + hue + " in " + defenseReactionTime);
                Wait.Until(t => t > defenseReactionTime, () =>
                {
                    var newHue = RandomHelper.Between(hue - 30, hue + 30) % 360;
                    if (newHue < 0) newHue += 360;
                    Debug.Log("[AI] reacting to bullet hue " + hue + " with " + newHue);
                    UpdateEnemyShieldHue(newHue);
                }, true);

                aiLastSeenBulletHue = hue;
            }
            else
                Debug.Log("[AI] ignored incoming bullet, too similar to last one");

            // AI gets damaged in a while
            Wait.Until(t => t > (enemyShieldHue.HasValue ? 21 : 12), () =>
            {
                if (enemyShieldHue.HasValue)
                {
                    var esHue = enemyShieldHue.Value;

                    var shieldV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * esHue), Mathf.Sin(Mathf.Deg2Rad * esHue)).normalized;
                    var bulletV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * hue), Mathf.Sin(Mathf.Deg2Rad * hue)).normalized;

                    var malus = (Vector3.Dot(bulletV, shieldV) + 1) / 2;
                    if (malus < 0.25f) malus = 0;

                    power = Mathf.Max(power - malus * 4, 0);
                }

                UpdateEnemyHealth(Math.Max(enemyHealth - power * 25, 0));

                Debug.Log("[AI] received bullet, power = " + power + ", health = " + enemyHealth);

                if (enemyHealth <= 0)
                    EndGame(false);
            });
        }

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
    public void UpdateEnemyShieldHue(float hue)
    {
        EnemyShieldHue = hue;
    }
    [RPC]
    public void UpdateEnemyHealth(float health)
    {
        EnemyHealth = health;
    }
}
