// ViewModels/MainWindowViewModel.cs

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TelegramBotApp.Models;

namespace TelegramBotApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private BotInfo Bot = new BotInfo();
        private bool _isLoading;


        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private const string KeySeparator = ";";

        public ObservableCollection<string> HashKeys { get; } = new ObservableCollection<string>();

        public string SelectedHashKey
        {
            get => Bot.Token;
            set => Bot.Token = value;
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoginCommand { get; }

        public MainWindowViewModel()
        {
            LoginCommand = new RelayCommand(async _ => await OnLoginAsync(), _ => !IsLoading);
            InitializeUI();
        }

        private async void InitializeUI()
        {
            var storedHashKeys = await LoadStoredHashKeysAsync();
            foreach (var key in storedHashKeys)
            {
                HashKeys.Add(key);
            }
        }

        private Task<string[]> LoadStoredHashKeysAsync()
        {
            var storedKeys = Properties.Settings.Default.StoredHashKey;
            return Task.FromResult(storedKeys.Split(new[] { KeySeparator }, StringSplitOptions.RemoveEmptyEntries));
        }

        private void StoreHashKey(string hashKey)
        {
            if (hashKey == null) return;

            var keysList = Properties.Settings.Default.StoredHashKey
                .Split(new[] { KeySeparator }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (keysList.Contains(hashKey)) return;

            keysList.Add(hashKey);
            Properties.Settings.Default.StoredHashKey = string.Join(KeySeparator, keysList);
            Properties.Settings.Default.Save();
        }

        private async Task OnLoginAsync()
        {
            IsLoading = true;

            if (!IsHashKeyValid(SelectedHashKey))
            {
                ShowError("Хэш-ключ не должен быть пустым.");
                IsLoading = false;
                return;
            }

            try
            {
                if (await IsValidHashKeyAsync(Bot.Token, _cts.Token))
                {
                    StoreHashKey(SelectedHashKey);
                    OpenChatWindow(Bot);
                }
                else
                {
                    ShowError("Пожалуйста, введите корректный ХЭШ-ключ.");
                }
            }
            catch (OperationCanceledException)
            {
                ShowError("Операция была отменена.");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при входе: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<bool> IsValidHashKeyAsync(string hashKey, CancellationToken cancellationToken)
        {
            string url = $"https://api.telegram.org/bot{hashKey}/getMe";
            try
            {
                var response = await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                using (var jsonDoc = JsonDocument.Parse(responseBody))
                {
                    if (jsonDoc.RootElement.TryGetProperty("ok", out var okProperty) && okProperty.GetBoolean())
                    {
                        var resultProperty = jsonDoc.RootElement.GetProperty("result");
                        Bot.Id = resultProperty.GetProperty("id").GetInt64().ToString();
                        Bot.Name = resultProperty.GetProperty("first_name").GetString();
                        Bot.Username = resultProperty.GetProperty("username").GetString();
                        Bot.CanJoinGroups = resultProperty.GetProperty("can_join_groups").GetBoolean();
                        Bot.CanReadAllGroupMessages = resultProperty.GetProperty("can_read_all_group_messages").GetBoolean();
                        Bot.SupportsInlineQueries = resultProperty.GetProperty("supports_inline_queries").GetBoolean();
                        Bot.CanConnectToBusiness = resultProperty.GetProperty("can_connect_to_business").GetBoolean();
                        Bot.MainWebApp = resultProperty.GetProperty("has_main_web_app").GetBoolean();

                        return true;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                ShowError($"Ошибка сети: {ex.Message}. Проверьте ваше интернет-соединение.");
            }
            catch (JsonException ex)
            {
                ShowError($"Ошибка при обработке данных: {ex.Message}. Убедитесь, что ответ сервера в правильном формате.");
            }

            return false;
        }

        private bool IsHashKeyValid(string hashKey) => !string.IsNullOrWhiteSpace(hashKey);

        private void OpenChatWindow(BotInfo Bot)
        {
            var chatWindow = new ChatWindow(Bot);
            chatWindow.Show();
            Application.Current.MainWindow.Close();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            OnPropertyChanged(propertyName);
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
