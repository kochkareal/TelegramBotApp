// ChatWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TelegramBotApp
{
    public partial class ChatWindow : Window
    {
        private readonly string _hashKey;
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly List<Chat> _chats = new List<Chat>();
        private readonly List<Message> _messages = new List<Message>();

        public ChatWindow(string hashKey)
        {
            InitializeComponent();
            _hashKey = hashKey;

            LoadChatsAsync();
        }

        private async void LoadChatsAsync()
        {
            try
            {
                await Dispatcher.BeginInvoke(new Action(() => LoadingProgressBar.Visibility = Visibility.Visible));

                string url = $"https://api.telegram.org/bot{_hashKey}/getUpdates";
                HttpResponseMessage response = await httpClient.GetAsync(url).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    ParseResponse(responseBody);
                }
                else
                {
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("Ошибка при подключении к серверу. Попробуйте позже.");
                    }));
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Ошибка загрузки чатов: {ex.Message}");
                }));
            }
            finally
            {
                await Dispatcher.BeginInvoke(new Action(() => LoadingProgressBar.Visibility = Visibility.Collapsed));
            }
        }

        private void ParseResponse(string responseBody)
        {
            using (JsonDocument jsonDoc = JsonDocument.Parse(responseBody))
            {
                JsonElement root = jsonDoc.RootElement;

                if (root.GetProperty("ok").GetBoolean())
                {
                    var updates = root.GetProperty("result").EnumerateArray();
                    foreach (var update in updates)
                    {
                        var message = update.GetProperty("message");
                        var chat = message.GetProperty("chat");

                        int chatId = chat.GetProperty("id").GetInt32();
                        string chatName = chat.GetProperty("first_name").GetString();
                        string messageText = message.GetProperty("text").GetString();
                        DateTime messageDate = DateTimeOffset.FromUnixTimeSeconds(message.GetProperty("date").GetInt64()).DateTime;

                        if (!_chats.Any(c => c.Id == chatId))
                        {
                            _chats.Add(new Chat { Id = chatId, Name = chatName });
                        }

                        _messages.Add(new Message
                        {
                            ChatId = chatId,
                            From = message.GetProperty("from").GetProperty("first_name").GetString(),
                            Text = messageText,
                            Date = messageDate
                        });
                    }

                    Dispatcher.BeginInvoke(new Action(UpdateUI));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("Ошибка при получении данных.");
                    }));
                }
            }
        }

        private void UpdateUI()
        {
            ChatListBox.ItemsSource = null;
            ChatListBox.ItemsSource = _chats;
        }

        private void OnChatSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatListBox.SelectedItem is Chat selectedChat)
            {
                var messages = _messages.Where(m => m.ChatId == selectedChat.Id).ToList();
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = messages;
            }
        }

        private async void OnSendMessageClick(object sender, RoutedEventArgs e)
        {
            if (ChatListBox.SelectedItem is Chat selectedChat)
            {
                string messageText = MessageTextBox.Text;
                if (!string.IsNullOrEmpty(messageText))
                {
                    await SendMessageAsync(selectedChat.Id.ToString(), messageText);
                    MessageTextBox.Clear();

                    _messages.Add(new Message
                    {
                        ChatId = selectedChat.Id,
                        From = "Вы",
                        Text = messageText,
                        Date = DateTime.Now
                    });

                    await Dispatcher.BeginInvoke(new Action(() => OnChatSelectionChanged(null, null)));
                }
            }
        }

        private async Task SendMessageAsync(string chatId, string messageText)
        {
            try
            {
                string url = $"https://api.telegram.org/bot{_hashKey}/sendMessage?chat_id={chatId}&text={messageText}";
                HttpResponseMessage response = await httpClient.GetAsync(url).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                   await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("Ошибка при отправке сообщения.");
                    }));
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Ошибка отправки сообщения: {ex.Message}");
                }));
            }
        }
    }


public class Chat
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name;
    }

    public class Message
    {
        public int ChatId { get; set; }
        public string From { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }

        public override string ToString() => $"{Date}: {From}: {Text}";
    }
}