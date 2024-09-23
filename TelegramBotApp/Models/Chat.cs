// Models/Chat.cs
namespace TelegramBotApp.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name;
    }
}
