// Models/BotInfo.cs

namespace TelegramBotApp.Models
{
    public class BotInfo
    {
        public string Token{ get; set;}
        public string Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public bool CanJoinGroups { get; set; }
        public bool CanReadAllGroupMessages { get; set; }
        public bool SupportsInlineQueries { get; set; }
        public bool CanConnectToBusiness { get; set; }
        public bool MainWebApp { get; set; }
    }
}
