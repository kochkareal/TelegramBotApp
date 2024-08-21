// MainWindow.xaml.cs
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


namespace TelegramBotApp
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string StoredHashKeySettingName = "StoredHashKey";

        public MainWindow()
        {
            InitializeComponent();
            LoadStoredHashKey();
        }
        
        private void LoadStoredHashKey()
        {
            // Используем Dispatcher, чтобы убедиться, что выполнение идет на UI-потоке
            Dispatcher.Invoke(() =>
            {
                string storedHashKey = Properties.Settings.Default.StoredHashKey.ToString();
                if (!string.IsNullOrEmpty(storedHashKey))
                {
                    HashKeyComboBox.Items.Add(storedHashKey);
                }
            });
        }

        private void StoreHashKeyInMemory(string hashKey)
        {
            // Используем Dispatcher, чтобы убедиться, что выполнение идет на UI-потоке
            Dispatcher.Invoke(() =>
            {
                if (RememberCheckBox.IsChecked == true)
                {
                    Properties.Settings.Default.StoredHashKey = hashKey;
                }
                else
                {
                    Properties.Settings.Default.StoredHashKey = string.Empty;
                }
                Properties.Settings.Default.Save(); // Сохраняем изменения в настройках
            });
        }

        private async void OnLoginButtonClick(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => EnterButton.IsEnabled = false);
            string hashKey = HashKeyComboBox.Text;

            if (string.IsNullOrWhiteSpace(hashKey))
            {
                MessageBox.Show("Хэш-ключ не должен быть пустым.");
                Dispatcher.Invoke(() => EnterButton.IsEnabled = true);
                return;
            }

            Dispatcher.Invoke(() => LoadingProgressBar.Visibility = Visibility.Visible);

            try
            {
                bool isValidKey = await IsValidHashKeyAsync(hashKey);

                if (isValidKey)
                {
                    StoreHashKeyInMemory(hashKey);
                    await Task.Delay(500);
                    Dispatcher.Invoke(() =>
                    {
                        ChatWindow chatWindow = new ChatWindow(hashKey);
                        chatWindow.Show();
                        this.Close();
                    });
                }
                else
                {
                    MessageBox.Show("Пожалуйста, введите корректный ХЭШ-ключ.");
                    Dispatcher.Invoke(() => EnterButton.IsEnabled = true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}");
                Dispatcher.Invoke(() => EnterButton.IsEnabled = true);
            }
            finally
            {
                Dispatcher.Invoke(() => LoadingProgressBar.Visibility = Visibility.Collapsed);
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
                Dispatcher.Invoke(() => MessageBox.Show($"Ошибка сети: {ex.Message}"));
                return false;
            }
            catch (JsonException ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Ошибка данных: {ex.Message}"));
                return false;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Неизвестная ошибка: {ex.Message}"));
                return false;
            }
            finally
            {
                Dispatcher.Invoke(() => EnterButton.IsEnabled = true);
            }
        }
    }
}
