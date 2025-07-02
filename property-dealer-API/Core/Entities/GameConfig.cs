namespace property_dealer_API.Core.Entities
{
    public class GameConfig
    {
        public string? MaxNumPlayers { get; set; }
        public bool IsPublic { get; set; } = true;
        public string? GamePassword { get; set; }
        public string? LobbyOwner { get; set; }
    };
}
