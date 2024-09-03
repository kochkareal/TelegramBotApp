// Models/Message.cs
using System;

namespace TelegramBotApp.Models
{
    public class Message
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public long ChatId { get; set; }
        public DateTime Date { get; set; }
    }
}