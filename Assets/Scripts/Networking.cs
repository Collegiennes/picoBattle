using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Nat;
using UnityEngine;
using Random = UnityEngine.Random;

class Networking : MonoBehaviour
{
    public static Networking Instance;

    public static readonly string MyGuid = Guid.NewGuid().ToString();

    static Networking()
    {
        Debug.Log("My guid is " + MyGuid);
    }

    const int Port = 10000;
    const float HostsUpdateRate = 2;
    const string GameType = "PicoBattle 2.0";

    public bool IsServer, IsClient, IsRegistered;
    public bool LocalMode;

    INatDevice natDevice;
    Mapping udpMapping, tcpMapping;
    bool shouldTestConnection;

    float? sinceAiSawShieldUpdate, aiOffenseReactionTime, aiHueToCounter, aiLastSeenShieldHue, aiLastSeenBulletHue;
    float sinceAiShot, aiShootCooldown, aiBulletSize;
    float aiAssaultHue;

    public HostData ChosenHost;
    public HostData[] Hosts;
    float sinceUpdatedHosts, sinceUpdatedHue;
    bool useNat;
    public event Action<HostData[], HostData[]> HostsUpdated;

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

    void Start()
    {
        NatUtility.DeviceFound += (s, ea) =>
        {
            natDevice = ea.Device;
            MapPort();
        };
        NatUtility.DeviceLost += (s, ea) => { natDevice = null; };
        NatUtility.StartDiscovery();
        shouldTestConnection = true;
    }

    public static void RpcShootBullet(float power, float hue)
    {
        if (Instance.IsServer || Instance.IsClient)
            Instance.networkView.RPC("ShootBullet", RPCMode.All, Instance.IsServer, power, hue);
    }

    bool hostHueUpdateRequired;

    public static void RpcUpdateShieldHue(float? hue)
    {
        if (GameFlow.State == GameState.WaitingForTest) // Tearing down, don't mind it
            return;

        if (Instance.IsServer || Instance.IsClient)
            Instance.networkView.RPC("UpdateEnemyShieldHue", RPCMode.Others, hue.HasValue, hue.HasValue ? hue.Value : 0);

        if (GameFlow.State <= GameState.WaitingForChallenge)
        {
            Debug.Log("Will update the host's hue for the master server");
            Instance.hostHueUpdateRequired = true;
        }

        if (Instance.LocalMode && hue.HasValue)
        {
            if (!Instance.aiLastSeenShieldHue.HasValue || (DotHues(hue.Value, Instance.aiLastSeenShieldHue.Value) < 0.9f))
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
        Debug.Log("Sending new health to enemy : " + health);
            if (Instance.IsServer || Instance.IsClient)
        Instance.networkView.RPC("UpdateEnemyHealth", RPCMode.Others, health);
    }

    void FixedUpdate()
    {
        currentEnemyHealth = Mathf.Lerp(currentEnemyHealth, enemyHealth, 0.1f);

        if (enemyShieldHue.HasValue)
        {
            currentEnemyShieldHue = Mathf.LerpAngle(currentEnemyShieldHue, enemyShieldHue.Value, 0.1f);
            while (currentEnemyShieldHue < 0) currentEnemyShieldHue += 360;
            while (currentEnemyShieldHue > 360) currentEnemyShieldHue -= 360;
        }
    }

    string testMessage, shouldEnableNatMessage, testStatus;
    bool doneTesting, retestAfterServerInit;
    bool probingPublicIp;
    float sinceStartedProbing;

    void TestConnection()
    {
        // Start/Poll the connection test, report the results in a label and 
        // react to the results accordingly
        var connectionTestResult = probingPublicIp ? Network.TestConnectionNAT() : Network.TestConnection();

        switch (connectionTestResult)
        {
            case ConnectionTesterStatus.Error:
                testMessage = "Problem determining NAT capabilities";
                doneTesting = true;
                retestAfterServerInit = false;
                break;

            case ConnectionTesterStatus.Undetermined:
                testMessage = "Undetermined NAT capabilities";
                doneTesting = false;
                break;

            case ConnectionTesterStatus.PublicIPIsConnectable:
                testMessage = "Directly connectable public IP address.";
                useNat = false;
                doneTesting = true;
                retestAfterServerInit = false;
                break;

            // This case is a bit special as we now need to check if we can 
            // circumvent the blocking by using NAT punchthrough
            case ConnectionTesterStatus.PublicIPPortBlocked:
                testMessage = "Non-connectable public IP address (port blocked), running a server is impossible.";
                useNat = false;
                // If no NAT punchthrough test has been performed on this public 
                // IP, force a test
                if (!probingPublicIp)
                {
                    probingPublicIp = true;
                    testStatus = "Testing if blocked public IP can be circumvented";
                    sinceStartedProbing = 0;
                }
                // NAT punchthrough test was performed but we still get blocked
                else
                {
                    sinceStartedProbing += Time.deltaTime;
                    if (sinceStartedProbing > 10)
                    {
                        probingPublicIp = false;         // reset
                        useNat = true;
                        retestAfterServerInit = false;
                        doneTesting = true;
                    }
                }
                break;
            case ConnectionTesterStatus.PublicIPNoServerStarted:
                testMessage = "Public IP address but server not initialized, " +
                    "it must be started to check server accessibility. Restart " +
                    "connection test when ready.";
                retestAfterServerInit = true;
                doneTesting = true;
                break;

            case ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted:
                testMessage = "Limited NAT punchthrough capabilities. Cannot " +
                    "connect to all types of NAT servers. Running a server " +
                    "is ill advised as not everyone can connect.";
                useNat = true;
                retestAfterServerInit = false;
                doneTesting = true;
                break;

            case ConnectionTesterStatus.LimitedNATPunchthroughSymmetric:
                testMessage = "Limited NAT punchthrough capabilities. Cannot " +
                    "connect to all types of NAT servers. Running a server " +
                    "is ill advised as not everyone can connect.";
                useNat = true;
                retestAfterServerInit = false;
                doneTesting = true;
                break;

            case ConnectionTesterStatus.NATpunchthroughAddressRestrictedCone:
            case ConnectionTesterStatus.NATpunchthroughFullCone:
                testMessage = "NAT punchthrough capable. Can connect to all " +
                    "servers and receive connections from all clients. Enabling " +
                    "NAT punchthrough functionality.";
                useNat = true;
                retestAfterServerInit = false;
                doneTesting = true;
                break;
        }

        if (doneTesting)
        {
            if (useNat)
                shouldEnableNatMessage = "When starting a server the NAT " +
                    "punchthrough feature should be enabled (useNat parameter)";
            else
                shouldEnableNatMessage = "NAT punchthrough not needed";
            testStatus = "Done testing";
        }

        if (connectionTestResult != ConnectionTesterStatus.Undetermined && !(connectionTestResult == ConnectionTesterStatus.PublicIPPortBlocked && sinceStartedProbing > 0))
            Debug.Log(testStatus + " : " + testMessage + " | " + shouldEnableNatMessage);
    }

    string lastComment;

    void Update()
    {
        if (shouldTestConnection && !doneTesting)
            TestConnection();

        switch (GameFlow.State)
        {
            case GameState.WaitingForTest:
                if (doneTesting)
                    GameFlow.State = GameState.RecreateServer;
                break;

            case GameState.RecreateServer:
                CreateServer();
                GameFlow.State = GameState.WaitingForChallenge;
                break;

            case GameState.WaitingForChallenge:
                if (retestAfterServerInit)
                    TestConnection();

                if (hostHueUpdateRequired)
                {
                    var comment = ShieldGenerator.Instance.IsPowered
                                      ? Mathf.RoundToInt(ShieldGenerator.Instance.Hue).ToString()
                                      : "NotReady";
                    if (lastComment != comment)
                        Debug.Log("Updated the hue on the master sever for this host : " + comment);
                    MasterServer.RegisterHost(GameType, MyGuid, comment);
                    hostHueUpdateRequired = false;
                    lastComment = comment;
                }

                sinceUpdatedHosts += Time.deltaTime;
                if (sinceUpdatedHosts > HostsUpdateRate)
                    UpdateHosts();

                sinceUpdatedHue += Time.deltaTime;
                if (sinceUpdatedHue > 3)
                {
                    hostHueUpdateRequired = true;
                    sinceUpdatedHue = 0;
                }
                break;

            case GameState.ReadyToConnect:
                if (IsRegistered)
                {
                    Network.maxConnections = -1;
                    MasterServer.RegisterHost(GameType, MyGuid, "Closed");
                    IsRegistered = false;
                }

                Placement.Instance.Reset();
                MousePicking.Instance.Reset();
                ShieldGenerator.Instance.Reset();

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

            case GameState.Connecting:
                // ?
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

        if (natDevice != null)
        {
            try
            {
                if (udpMapping != null)
                    natDevice.DeletePortMap(udpMapping);
                if (tcpMapping != null)
                    natDevice.DeletePortMap(tcpMapping);
                tcpMapping = udpMapping = null;
                Debug.Log("Deleted port mapping");
            }
            catch (Exception ex)
            {
                Debug.Log("Failed to delete port mapping");
            }
        }
        NatUtility.StopDiscovery();
    }

    void UpdateHosts()
    {
        MasterServer.RequestHostList(GameType);

        var oldHosts = Hosts;
        var newHosts = MasterServer.PollHostList();

        Hosts = newHosts;

        HostsUpdated(newHosts, oldHosts);

        sinceUpdatedHosts = 0;
    }

    void MapPort()
    {
        try
        {
            Debug.Log("Mapping port...");

            bool udpDone = false; //, tcpDone = false;

            udpMapping = new Mapping(Protocol.Udp, Port, Port) { Description = "Pico Battle (UDP)" };
            natDevice.BeginCreatePortMap(udpMapping, state =>
            {
                if (state.IsCompleted)
                {
                    Debug.Log("UDP Mapping complete!");
//                    Debug.Log("UDP Mapping complete! Testing...");
//                    try
//                    {
//                        var m = natDevice.GetSpecificMapping(Protocol.Udp, Port);
//                        if (m == null)
//                            throw new InvalidOperationException("Mapping not found");
//                        if (m.PrivatePort != Port || m.PublicPort != Port)
//                            throw new InvalidOperationException("Mapping invalid");
//
//                        Debug.Log("Success!");
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.Log("Failed to validate UDP mapping :\n" + ex.ToString());
//                    }

                    udpDone = true;
//                    if (tcpDone)
                }
            }, null);

//            tcpMapping = new Mapping(Protocol.Tcp, Port, Port) { Description = "Pico Battle (TCP)" };
//            natDevice.BeginCreatePortMap(tcpMapping, state =>
//            {
//                if (state.IsCompleted)
//                {
//                    Debug.Log("TCP Mapping complete!");
//                    Debug.Log("TCP Mapping complete! Testing...");
//                    try
//                    {
//                        var m = natDevice.GetSpecificMapping(Protocol.Tcp, Port);
//                        if (m == null)
//                            throw new InvalidOperationException("Mapping not found");
//                        if (m.PrivatePort != Port || m.PublicPort != Port)
//                            throw new InvalidOperationException("Mapping invalid");
//
//                        Debug.Log("Success!");
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.Log("Failed to validate TCP mapping :\n" + ex.ToString());
//                    }
//
//                    tcpDone = true;
//                    if (udpDone)
//                        shouldTestConnection = true;
//                }
//            }, null);
        }
        catch (Exception ex)
        {
            Debug.Log("Failed to map port :\n" + ex.ToString());
        }
    }

    public void Reset()
    {
        GameFlow.State = GameState.WaitingForTest;
        ShieldGenerator.Instance.Reset();
        Cannon.Instance.Reset();
        Placement.Instance.Reset();
        MousePicking.Instance.Reset();
        Camera.main.GetComponent<CameraOrbit>().Reset();
        EnemyHealth = 500;
        EnemyShieldHue = null;
        LocalMode = false;
        sinceUpdatedHue = sinceUpdatedHosts = 0;
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
            Network.InitializeServer(1, Port, useNat);

        if (!IsRegistered)
        {
            Network.maxConnections = 1;
            MasterServer.RegisterHost(GameType, MyGuid, ShieldGenerator.Instance.IsPowered
                                                            ? Mathf.RoundToInt(ShieldGenerator.Instance.Hue).ToString()
                                                            : "NotReady");
            hostHueUpdateRequired = false;
        }

        HostsUpdated(new HostData[0], Hosts ?? new HostData[0]);
        Hosts = new HostData[0];
        sinceUpdatedHosts = 0;

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
        if (IsRegistered)
        {
            Network.maxConnections = -1;
            MasterServer.RegisterHost(GameType, MyGuid, "Closed");
            IsRegistered = false;
        }
        IsClient = false;
        IsServer = true;
        ChosenHost = Hosts.First(x => x.guid == player.guid);

        Placement.Instance.Reset();
        MousePicking.Instance.Reset();
        ShieldGenerator.Instance.Reset();
        EnemyHealth = 500;

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
        var h = ChosenHost;
        Debug.Log("Will attempt connection to : useNat = " + h.useNat + " guid = " + h.guid + ", ip = " +
                  h.ip.Aggregate("", (a, b) => a + (a == "" ? "" : ".") + b) + ", gameName = " + h.gameName);

        ChosenHost.useNat = useNat;
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
        EnemyHealth = 500;
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
                    UpdateEnemyShieldHue(true, newHue);
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
    public void UpdateEnemyShieldHue(bool isPowered, float hue)
    {
        EnemyShieldHue = isPowered ? hue : (float?) null;
    }
    [RPC]
    public void UpdateEnemyHealth(float health)
    {
        Debug.Log("RPC updated enemy health to " + health);
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
