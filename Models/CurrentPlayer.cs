namespace BattleFleet.Models
{
    public static class CurrentPlayer
    {
        public static int Id { get; set; }
        public static string Name { get; set; }
        public static bool IsLoggedIn { get; set; }

        public static void Login(PlayerStats stats)
        {
            Id = stats.Id;
            Name = stats.PlayerName;
            IsLoggedIn = true;
        }

        public static void Logout()
        {
            Id = 0;
            Name = null;
            IsLoggedIn = false;
        }
    }
} 