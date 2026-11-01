namespace RichRich.Domain.Models
{
    /// <summary>
    /// Game difficulty settings affecting starting cash and initial financial headroom.
    /// </summary>
    public enum GameDifficulty
    {
        Hard,
        Standard,
        Comfortable
    }

    /// <summary>
    /// Configurable starting conditions for a new life simulation.
    /// </summary>
    public class GameStartConfig
    {
        public GameDifficulty Difficulty { get; }
        public Currency StartingCash { get; }
        public int StartingTickets { get; }

        public GameStartConfig(GameDifficulty difficulty)
        {
            Difficulty = difficulty;
            StartingCash = GetStartingCash(difficulty);
            StartingTickets = 3;
        }

        public GameStartConfig(GameDifficulty difficulty, Currency customStartingCash, int startingTickets = 3)
        {
            Difficulty = difficulty;
            StartingCash = customStartingCash;
            StartingTickets = startingTickets;
        }

        public static Currency GetStartingCash(GameDifficulty difficulty)
        {
            return difficulty switch
            {
                GameDifficulty.Hard => Currency.FromDollars(1000.00),         // $1,000 (100,000 cents)
                GameDifficulty.Standard => Currency.FromDollars(3000.00),     // $3,000 (300,000 cents) [Default]
                GameDifficulty.Comfortable => Currency.FromDollars(5000.00),  // $5,000 (500,000 cents)
                _ => Currency.FromDollars(3000.00)
            };
        }

        public static GameStartConfig Default => new GameStartConfig(GameDifficulty.Standard);
    }
}
