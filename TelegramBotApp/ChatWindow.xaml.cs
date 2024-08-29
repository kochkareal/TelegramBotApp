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
        private readonly string _BotId;
        private readonly string _BotName;
        private readonly string _BotUsername;
        private readonly bool   IsCanJoinGroups;
        private readonly bool   IsCanReadAllGroupMessages;
        private readonly bool   IsSupportsInlineQueries;
        private readonly bool   IsCanConnectToBusiness;
        private readonly bool   IsMainWebApp;
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly List<Chat> _chats = new List<Chat>();
        private readonly List<Message> _messages = new List<Message>();

        public ChatWindow(string hashKey, string BotId, string BotName, string BotUsername,
           bool CanJoinGroups, bool CanReadAllGroupMessages, bool SupportsInlineQueries, bool CanConnectToBusiness, bool MainWebApp)
        {
            InitializeComponent();
            _hashKey = hashKey;
            _BotId = BotId;
            _BotName = BotName;
            _BotUsername = "@" + BotUsername;
            IsCanJoinGroups = CanJoinGroups;
            IsCanReadAllGroupMessages = CanReadAllGroupMessages;
            IsSupportsInlineQueries = SupportsInlineQueries;
            IsCanConnectToBusiness = CanConnectToBusiness;
            IsMainWebApp = MainWebApp;

            TitleLable.Content = "Чаты " + _BotUsername;
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
                    string errorResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"Ошибка при подключении к серверу. Ответ: {errorResponse}");
                    }));
                }
            }
            catch (HttpRequestException httpEx)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Ошибка HTTP запроса: {httpEx.Message}\n{httpEx.StackTrace}");
                }));
            }
            catch (Exception ex)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Ошибка загрузки чатов: {ex.Message}\n{ex.StackTrace}");
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

                if (root.TryGetProperty("ok", out JsonElement okElement) && okElement.GetBoolean())
                {
                    if (root.TryGetProperty("result", out JsonElement resultElement) && resultElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var update in resultElement.EnumerateArray())
                        {
                            if (update.TryGetProperty("message", out JsonElement messageElement))
                            {
                                if (messageElement.TryGetProperty("chat", out JsonElement chatElement))
                                {
                                    long chatId = 0;
                                    if (chatElement.TryGetProperty("id", out JsonElement chatIdElement) && chatIdElement.ValueKind == JsonValueKind.Number)
                                    {
                                        chatId = chatIdElement.GetInt64();
                                    }
                                    else
                                    {
                                        continue; 
                                    }

                                    string chatName = chatElement.TryGetProperty("first_name", out JsonElement nameElement) ? nameElement.GetString() : "Unknown";
                                    string messageText = messageElement.TryGetProperty("text", out JsonElement textElement) ? textElement.GetString() : string.Empty;
                                    DateTime messageDate = messageElement.TryGetProperty("date", out JsonElement dateElement)
                                        ? DateTimeOffset.FromUnixTimeSeconds(dateElement.GetInt64()).DateTime
                                        : DateTime.MinValue;

                                    if (!_chats.Any(c => c.Id == chatId))
                                    {
                                        _chats.Add(new Chat { Id = (int)chatId, Name = chatName });
                                    }

                                    _messages.Add(new Message
                                    {
                                        ChatId = chatId,
                                        From = messageElement.TryGetProperty("from", out JsonElement fromElement) ? fromElement.GetProperty("first_name").GetString() : "Unknown",
                                        Text = messageText,
                                        Date = messageDate
                                    });
                                }
                            }
                        }

                        Dispatcher.BeginInvoke(new Action(UpdateUI));
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show("Ошибка: 'result' отсутствует или не является массивом.");
                        }));
                    }
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
            if (_messages.Count == 0) {
                ChatListBox.Visibility = Visibility.Collapsed;
                NoMessagesTextBlock.Visibility = Visibility.Visible;
            }
            else NoMessagesTextBlock.Visibility = Visibility.Collapsed;
        }

        private void OnChatSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatListBox.SelectedItem is Chat selectedChat)
            {
                var messages = _messages.Where(m => m.ChatId == selectedChat.Id).ToList();
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = messages;
                NoMessagesTextBlock.Visibility = messages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
                        From = _BotUsername,
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
                    string errorResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"Ошибка при отправке сообщения. Ответ: {errorResponse}");
                    }));
                }
            }
            catch (HttpRequestException httpEx)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Ошибка HTTP запроса при отправке сообщения: {httpEx.Message}\n{httpEx.StackTrace}");
                }));
            }
            catch (Exception ex)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Ошибка отправки сообщения: {ex.Message}\n{ex.StackTrace}");
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
        public long ChatId { get; set; }
        public string From { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }

        public override string ToString() => $"{Date}: {From}: {Text}";
    }

}