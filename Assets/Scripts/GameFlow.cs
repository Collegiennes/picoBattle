public class GameFlow
{
    public static GameState State;
}

public enum GameState
{
    Login, ReadyToConnect, WaitingOrConnecting, ReadyToPlay, Syncing, Gameplay, Won, Lost
}
