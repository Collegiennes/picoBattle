public class GameFlow
{
    public static GameState State;
}

public enum GameState
{
    RecreateServer, WaitingForChallenge, Connecting, Gameplay, Won, Lost
}
