public class GameFlow
{
    public static GameState State;
}

public enum GameState
{
    RecreateServer, WaitingForChallenge, ReadyToConnect, Connecting, Gameplay, Won, Lost
}
