[System.Serializable]
public struct GameData
{
    public int score;
    public int energy;
    public int boosters;

    public GameData(int score, int energy, int boosters)
    {
        this.score = score;
        this.energy = energy;
        this.boosters = boosters;
    }
}