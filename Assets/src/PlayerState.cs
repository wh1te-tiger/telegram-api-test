public class PlayerState
{
    public PlayerState(int score, int energy, int boosters)
    {
        Score = score;
        Energy = energy;
        Boosters = boosters;
    }

    public PlayerState(GameData gameData)
    {
        Score = gameData.score;
        Energy = gameData.energy;
        Boosters = gameData.boosters;
    }

    public int Score { get; private set; }
    public int Energy { get; private set; }
    public int Boosters { get; private set; }
    public bool IsBoosterActivated { get; set; }

    private const int BoosterModifier = 2;
    
    public void IncreaseScore(int n)
    {
        Score += IsBoosterActivated ? n * BoosterModifier : n;
        Energy--;
    }

    public GameData GetGameData()
    {
        return new GameData(Score, Energy, Boosters);
    }
}