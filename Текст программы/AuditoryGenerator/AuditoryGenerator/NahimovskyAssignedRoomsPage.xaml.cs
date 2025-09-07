using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AuditoryGenerator
{
    /// <summary>
    /// Логика взаимодействия для NahimovskyAssignedRoomsPage.xaml
    /// </summary>
    public partial class NahimovskyAssignedRoomsPage : Page
    {
        private List<Teacher> allTeachers = new List<Teacher>();
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiUrl = "http://auditoryhelperapi.somee.com/api/AssignedRooms";
        private readonly string _roomsUrl = "http://auditoryhelperapi.somee.com/api/Rooms";
        private readonly string _teachersUrl = "http://auditoryhelperapi.somee.com/api/Teachers";

        public NahimovskyAssignedRoomsPage()
        {
            InitializeComponent();
            LoadAssignedRooms();
            LoadRooms();
            LoadTeachers();
        }

        private async void LoadAssignedRooms()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var assignedRooms = JsonSerializer.Deserialize<List<AssignedRoomViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var filteredAssignedRooms = assignedRooms.Where(ar => ar.CampusId == 1).ToList();

                AssignedDataGrid.ItemsSource = filteredAssignedRooms;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке привязок: {ex.Message}");
            }
        }

        private async void LoadRooms()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_roomsUrl}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var rooms = JsonSerializer.Deserialize<List<Room>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var filteredRooms = rooms.Where(r => r.CampusId == 1)
                                         .Select(r => new RoomViewModel
                                         {
                                             Id = r.Id,
                                             RoomNumber = r.RoomNumber,
                                             CampusId = r.CampusId,
                                             CampusName = "Нахимовский"
                                         })
                                         .ToList();

                RoomComboBox.ItemsSource = filteredRooms;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке аудиторий: {ex.Message}");
            }
        }
        private async void LoadTeachers()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_teachersUrl}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                allTeachers = JsonSerializer.Deserialize<List<Teacher>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                TeacherComboBox.ItemsSource = allTeachers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке преподавателей: {ex.Message}");
            }
        }
        private string _lastSearch = "";

        private void TeacherComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var searchText = comboBox.Text.ToLower();

            if (searchText == _lastSearch) return;
            _lastSearch = searchText;

            var filtered = allTeachers
                .Where(t => t.FullName != null && t.FullName.ToLower().Contains(searchText))
                .ToList();

            comboBox.ItemsSource = filtered;
            comboBox.IsDropDownOpen = true;
        }
        private async void AddAssignedButton_Click(object sender, RoutedEventArgs e)
        {
            if (RoomComboBox.SelectedValue == null || TeacherComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите аудиторию и преподавателя.");
                return;
            }

            try
            {
                var selectedRoom = (RoomViewModel)RoomComboBox.SelectedItem;
                var existingAssignments = (List<AssignedRoomViewModel>)AssignedDataGrid.ItemsSource;
                bool isDuplicate = existingAssignments.Any(ar => ar.TeacherId == (int)TeacherComboBox.SelectedValue && ar.RoomId == selectedRoom.Id);

                if (isDuplicate)
                {
                    MessageBox.Show("Этот преподаватель уже закреплен за данной аудиторией.");
                    return;
                }
                var assignedRoom = new AssignedRoomViewModel
                {
                    RoomId = selectedRoom.Id.Value,
                    TeacherId = (int)TeacherComboBox.SelectedValue,
                    CampusId = selectedRoom.CampusId
                };
                var json = JsonConvert.SerializeObject(assignedRoom);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                response.EnsureSuccessStatusCode();

                MessageBox.Show("Привязка успешно добавлена!");
                LoadAssignedRooms();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении привязки: {ex.Message}");
            }
        }
        
        private async void EditAssignedButton_Click(object sender, RoutedEventArgs e)
        {
            if (AssignedDataGrid.SelectedItem is AssignedRoomViewModel selectedRoom)
            {
                if (RoomComboBox.SelectedValue == null || TeacherComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите аудиторию и преподавателя.");
                    return;
                }

                try
                {
                    var selectedRoomViewModel = (RoomViewModel)RoomComboBox.SelectedItem;

                    var assignedRoom = new AssignedRoomViewModel
                    {
                        Id = selectedRoom.Id,
                        RoomId = (int)RoomComboBox.SelectedValue,
                        TeacherId = (int)TeacherComboBox.SelectedValue,
                        CampusId = selectedRoomViewModel.CampusId
                    };

                    var json = JsonConvert.SerializeObject(assignedRoom);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PutAsync($"{_apiUrl}/{assignedRoom.Id}", content);
                    response.EnsureSuccessStatusCode();

                    MessageBox.Show("Привязка успешно изменена!");
                    LoadAssignedRooms();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при изменении привязки: {ex.Message}");
                }
            }
        }
        private async void DeleteAssignedButton_Click(object sender, RoutedEventArgs e)
        {
            if (AssignedDataGrid.SelectedItem is AssignedRoomViewModel selectedRoom)
            {
                try
                {
                    var response = await _httpClient.DeleteAsync($"{_apiUrl}/{selectedRoom.Id}");
                    response.EnsureSuccessStatusCode();

                    MessageBox.Show("Привязка успешно удалена!");
                    LoadAssignedRooms();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении привязки: {ex.Message}");
                }
            }
        }

        private void AssignedDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssignedDataGrid.SelectedItem is AssignedRoomViewModel selectedRoom)
            {
                RoomComboBox.SelectedValue = selectedRoom.RoomId;
                TeacherComboBox.SelectedValue = selectedRoom.TeacherId;
            }
        }
    }
}
