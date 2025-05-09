namespace BattleFleet.Models
{
    public class DatabaseSettings
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }

        public string GetConnectionString()
        {
            return $"Server={Server};Database={Database};Uid={UserId};Pwd={Password};";
        }
    }
} 