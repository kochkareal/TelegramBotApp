// Models/Message.cs
using System;

namespace TelegramBotApp.Models
{
    public class Message
    {
        public long ChatId { get; set; }
        public string From { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }

        public override string ToString() => $"{Date}: {From}: {Text}";
    }
}