using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AuditoryGenerator
{
    public partial class RoomsPage : Page
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiUrl = "http://auditoryhelperapi.somee.com/api/Rooms";
        public List<RoomViewModel> RoomsList { get; set; }
        public RoomsPage()
        {
            InitializeComponent();
            LoadRooms();
            LoadCampuses();
        }
        private async void LoadRooms()
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                RoomsList = JsonSerializer.Deserialize<List<RoomViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                RoomsDataGrid.ItemsSource = RoomsList;
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private async void LoadCampuses()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://auditoryhelperapi.somee.com/api/Campus");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var campuses = JsonSerializer.Deserialize<List<Campus>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                CampusComboBox.ItemsSource = campuses;
                CampusComboBox.DisplayMemberPath = "Name";
                CampusComboBox.SelectedValuePath = "Id";
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка при загрузке корпусов: {ex.Message}");
            }
        }
        private async void AddRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RoomNumberTextBox.Text) || CampusComboBox.SelectedValue == null)
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }
            try
            {
                var room = new RoomViewModel
                {
                    RoomNumber = RoomNumberTextBox.Text,
                    CampusId = (int)CampusComboBox.SelectedValue
                };

                var json = JsonConvert.SerializeObject(room);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                response.EnsureSuccessStatusCode();

                MessageBox.Show("Аудитория успешно добавлена!");
                LoadRooms();
                RoomNumberTextBox.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении аудитории: {ex.Message}");
            }
        }
        private async void EditRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem is RoomViewModel selectedRoom)
            {
                if (string.IsNullOrWhiteSpace(RoomNumberTextBox.Text) || CampusComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Заполните все поля.");
                    return;
                }

                try
                {
                    selectedRoom.RoomNumber = RoomNumberTextBox.Text;
                    selectedRoom.CampusId = (int)CampusComboBox.SelectedValue;

                    var json = JsonConvert.SerializeObject(selectedRoom);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PutAsync($"{_apiUrl}/{selectedRoom.Id}", content);
                    response.EnsureSuccessStatusCode();

                    MessageBox.Show("Аудитория успешно изменена!");
                    LoadRooms();
                    RoomNumberTextBox.Text = "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании аудитории: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Выберите аудиторию для редактирования.");
            }
        }
        private async void DeleteRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem is RoomViewModel selectedRoom)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить аудиторию {selectedRoom.RoomNumber}?",
                                             "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;

                try
                {
                    var response = await _httpClient.DeleteAsync($"{_apiUrl}/{selectedRoom.Id}");
                    response.EnsureSuccessStatusCode();

                    MessageBox.Show("Аудитория успешно удалена!");
                    LoadRooms();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении аудитории: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Выберите аудиторию для удаления.");
            }
        }
        private void RoomsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem is RoomViewModel selectedRoom)
            {
                RoomNumberTextBox.Text = selectedRoom.RoomNumber;
                CampusComboBox.SelectedValue = selectedRoom.CampusId;
            }
        }
    }
}
