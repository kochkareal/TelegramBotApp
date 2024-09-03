// Models/BotInfo.cs

namespace TelegramBotApp.Models
{
    public class BotInfo
    {
        public string BotId { get; set; }
        public string BotName { get; set; }
        public string BotUsername { get; set; }
        public bool IsCanJoinGroups { get; set; }
        public bool IsCanReadAllGroupMessages { get; set; }
        public bool IsSupportsInlineQueries { get; set; }
        public bool IsCanConnectToBusiness { get; set; }
        public bool IsMainWebApp { get; set; }
    }
}
