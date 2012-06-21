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
    public static Networking Instance;

    public static readonly string MyGuid = new Guid().ToString();

    const int Port = 10000;
    const float HostsUpdateRate = 1;
    const string GameType = "PicoBattle 2.0";

    public bool IsServer, IsClient, IsRegistered;
    public bool LocalMode;

    string errorMessage;

    float? sinceAiSawShieldUpdate, aiOffenseReactionTime, aiHueToCounter, aiLastSeenShieldHue, aiLastSeenBulletHue;
    float sinceAiShot, aiShootCooldown, aiBulletSize;
    float aiAssaultHue;

    public HostData ChosenHost;
    public HostData[] Hosts;
    float sinceUpdatedHosts;
    public event Action<IEnumerable<HostData>, IEnumerable<HostData>> HostsUpdated;

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

    void Awake()
    {
        Instance = this;
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
            case GameState.RecreateServer:
                CreateServer();
                GameFlow.State = GameState.WaitingForChallenge;
                break;

            case GameState.WaitingForChallenge:
                sinceUpdatedHosts += Time.deltaTime;
                if (sinceUpdatedHosts > HostsUpdateRate)
                    UpdateHosts();
                break;

            case GameState.Connecting:
                MasterServer.UnregisterHost();
                IsRegistered = false;

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

                    GameFlow.State = GameState.Gameplay;
                }
                else
                {
                    CloseServer();
                    ConnectToServer();
                }
                break;

            case GameState.Gameplay:
                if (LocalMode)
                    AI();

                if (Input.GetKeyDown(KeyCode.Escape))
                    Reset();
                break;
        }
    }

    void OnApplicationQuit()
    {
        if (IsServer) CloseServer();
        if (IsClient) Network.Disconnect();
        if (IsRegistered) MasterServer.UnregisterHost();
    }

    void UpdateHosts()
    {
        MasterServer.RequestHostList(GameType);

        var oldHosts = Hosts;
        var newHosts = MasterServer.PollHostList();

        Hosts = newHosts;

        HostsUpdated(newHosts.Except(oldHosts, HostDataEqualityComparer.Default), oldHosts.Except(newHosts, HostDataEqualityComparer.Default));

        sinceUpdatedHosts = 0;
    }

    public void Reset()
    {
        GameFlow.State = GameState.RecreateServer;
        ShieldGenerator.Instance.Reset();
        Cannon.Instance.Reset();
        Placement.Instance.Reset();
        MousePicking.Instance.Reset();
        LocalMode = false;
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
        if (IsClient)
            Network.Disconnect();

        if (!IsServer)
            Network.InitializeServer(1, Port, true);

        if (!IsRegistered)
            MasterServer.RegisterHost(GameType, MyGuid);

        HostsUpdated(Enumerable.Empty<HostData>(), Hosts ?? Enumerable.Empty<HostData>());
        Hosts = new HostData[0];
        sinceUpdatedHosts = HostsUpdateRate;

        IsServer = IsRegistered = true;
        IsClient = false;
    }

    void CloseServer()
    {
        Network.Disconnect();
        IsServer = false;
    }

    void OnPlayerConnected(NetworkPlayer player)
    {
        GameFlow.State = GameState.Gameplay;
    }

    void OnPlayerDisconnected(NetworkPlayer player)
    {
        Network.RemoveRPCs(player);
        Network.DestroyPlayerObjects(player);

        Reset();
    }

    #endregion

    #region Client

    void ConnectToServer()
    {
        ChosenHost.useNat = true;
        Network.Connect(ChosenHost);

        GameFlow.State = GameState.Connecting;
    }

    void OnFailedToConnect(NetworkConnectionError error)
    {
        Debug.Log("Couldn't connect to server (reason : " + error);
        Reset();
    }

    void OnConnectedToServer()
    {
        GameFlow.State = GameState.Gameplay;
        IsClient = true;
    }

    void OnDisconnectedFromServer(NetworkDisconnection info)
    {
        Reset();
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

public class HostDataEqualityComparer : IEqualityComparer<HostData>
{
    public static HostDataEqualityComparer Default = new HostDataEqualityComparer();

    public bool Equals(HostData x, HostData y)
    {
        if (x == null) return y == null;
        if (y == null) return false;
        return x.guid == y.guid;
    }
    public int GetHashCode(HostData obj)
    {
        return obj.guid.GetHashCode();
    }
}
