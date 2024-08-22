// MainWindow.xaml.cs
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TelegramBotApp
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
        }

        private async void InitializeUI()
        {
            string storedHashKey = await LoadStoredHashKeyAsync();
            if (!string.IsNullOrEmpty(storedHashKey))
            {
                HashKeyComboBox.Items.Add(storedHashKey);
            }
        }

        private Task<string> LoadStoredHashKeyAsync()
        {
            return Task.FromResult(Properties.Settings.Default.StoredHashKey);
        }

        private void StoreHashKey(string hashKey)
        {

            if (RememberCheckBox.IsChecked == true)Properties.Settings.Default.StoredHashKey = hashKey;

            Properties.Settings.Default.Save();
        }

        private async void OnLoginButtonClick(object sender, RoutedEventArgs e)
        {
            SetLoadingState(true);

            string hashKey = HashKeyComboBox.Text;

            if (!IsHashKeyValid(hashKey))
            {
                ShowError("Хэш-ключ не должен быть пустым.");
                SetLoadingState(false);
                return;
            }

            try
            {
                bool isValid = await IsValidHashKeyAsync(hashKey, _cts.Token);
                if (isValid)
                {
                    StoreHashKey(hashKey);
                    OpenChatWindow(hashKey);
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
                SetLoadingState(false);
            }
        }


        private async Task<bool> IsValidHashKeyAsync(string hashKey, CancellationToken cancellationToken)
        {
            string url = $"https://api.telegram.org/bot{hashKey}/getUpdates";
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                using (JsonDocument jsonDoc = JsonDocument.Parse(responseBody))
                {
                    // Проверяем наличие свойства "ok" перед попыткой получить его значение
                    if (jsonDoc.RootElement.TryGetProperty("ok", out JsonElement okProperty))
                    {
                        return okProperty.GetBoolean();
                    }
                    else
                    {
                        ShowError("Ошибка в ответе сервера: отсутствует свойство 'ok'.");
                        return false;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                ShowError($"Ошибка сети: {ex.Message}. Проверьте ваше интернет-соединение.");
                return false;
            }
            catch (JsonException ex)
            {
                ShowError($"Ошибка при обработке данных: {ex.Message}. Убедитесь, что ответ сервера в правильном формате.");
                return false;
            }
        }

        private bool IsHashKeyValid(string hashKey)
        {
            return !string.IsNullOrWhiteSpace(hashKey);
        }

        private void OpenChatWindow(string hashKey)
        {
            var chatWindow = new ChatWindow(hashKey);
            chatWindow.Show();
            Close();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void SetLoadingState(bool isLoading)
        {
            EnterButton.IsEnabled = !isLoading;
            LoadingProgressBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts.Cancel();
            _cts.Dispose();
            base.OnClosed(e);
        }
    }

}
