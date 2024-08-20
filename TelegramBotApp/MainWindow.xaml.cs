// MainWindow.xaml.cs
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace TelegramBotApp
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public MainWindow() => InitializeComponent();

        private async void OnLoginButtonClick(object sender, RoutedEventArgs e)
        {
            EnterButton.IsEnabled = false;
            string hashKey = HashKeyTextBox.Text;
            

            if (string.IsNullOrWhiteSpace(hashKey))
            {
                MessageBox.Show("Хэш-ключ не должен быть пустым.");
                EnterButton.IsEnabled = true;
                return;
            }

            LoadingProgressBar.Visibility = Visibility.Visible;

            bool isValidKey = false;
            try
            {
                isValidKey = await IsValidHashKeyAsync(hashKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке ключа: {ex.Message}");
                EnterButton.IsEnabled = true;
            }
            finally
            {
                LoadingProgressBar.Visibility = Visibility.Collapsed;
            }

            if (isValidKey)
            {
                ChatWindow chatWindow = new ChatWindow(hashKey);
                chatWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректный ХЭШ-ключ.");
                EnterButton.IsEnabled = true;
            }
        }

        private async Task<bool> IsValidHashKeyAsync(string hashKey)
        {
            try
            {
                string url = $"https://api.telegram.org/bot{hashKey}/getUpdates";
                HttpResponseMessage response = await httpClient.GetAsync(url).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    using (JsonDocument jsonDoc = JsonDocument.Parse(responseBody))
                    {
                        JsonElement root = jsonDoc.RootElement;
                        return root.GetProperty("ok").GetBoolean();
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                // Сетевые ошибки, такие как неправильный URL или нет подключения
                MessageBox.Show($"Ошибка сети: {ex.Message}");
                EnterButton.IsEnabled = true;
                return false;
            }
            catch (JsonException ex)
            {
                // Ошибка при разборе JSON-ответа
                MessageBox.Show($"Ошибка данных: {ex.Message}");
                EnterButton.IsEnabled = true;
                return false;
            }
            catch (Exception ex)
            {
                // Все другие ошибки
                MessageBox.Show($"Неизвестная ошибка: {ex.Message}");
                EnterButton.IsEnabled = true;
                return false;
            }
        }
    }
}
