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
    /// Логика взаимодействия для Events.xaml
    /// </summary>
    public partial class Events : Page
    {
        private readonly HttpClient _httpClient = new HttpClient();

        private readonly string _apiUrl = "http://auditoryhelperapi.somee.com/api/Events";
        private readonly string _roomsUrl = "http://auditoryhelperapi.somee.com/api/Rooms";
        public Events()
        {
            InitializeComponent();
            LoadEvents(1, EventsDataGridNahim);
            LoadEvents(2, EventsDataGridNezh);

            LoadRooms(1, RoomComboBoxNahim, "Нахимовский");
            LoadRooms(2, RoomComboBoxNezh, "Нежинская");
        }
        private async void LoadEvents(int campusId, DataGrid targetDataGrid)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var events = JsonSerializer.Deserialize<List<EventsViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var roomsResponse = await _httpClient.GetAsync($"{_roomsUrl}");
                roomsResponse.EnsureSuccessStatusCode();
                var roomsJson = await roomsResponse.Content.ReadAsStringAsync();

                var rooms = JsonSerializer.Deserialize<List<RoomViewModel>>(roomsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                foreach (var ev in events)
                {
                    var room = rooms.FirstOrDefault(r => r.Id == ev.RoomId);
                    if (room != null)
                    {
                        ev.RoomNumber = room.RoomNumber;
                    }
                }

                var filteredRooms = rooms.Where(r => r.CampusId == campusId).Select(r => r.Id).ToList();
                var filteredEvents = events.Where(e => filteredRooms.Contains(e.RoomId)).ToList();
                targetDataGrid.ItemsSource = filteredEvents;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке мероприятий: {ex.Message}");
            }
        }
        private async void LoadRooms(int campusId, ComboBox targetComboBox, string campusName)
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
                var filteredRooms = rooms.Where(r => r.CampusId == campusId)
                                         .Select(r => new RoomViewModel
                                         {
                                             Id = r.Id,
                                             RoomNumber = r.RoomNumber,
                                             CampusId = r.CampusId,
                                             CampusName = campusName
                                         })
                                         .ToList();
                targetComboBox.ItemsSource = filteredRooms;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке аудиторий: {ex.Message}");
            }
        }

        private async void AddEventNahim_Click(object sender, RoutedEventArgs e)
        {
            if (RoomComboBoxNahim.SelectedValue == null || EventDateNahim.SelectedDate == null || string.IsNullOrWhiteSpace(EventNameNahim.Text) || string.IsNullOrWhiteSpace(LessonRangeNahim.Text))
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }

            try
            {
                var selectedRoom = (RoomViewModel)RoomComboBoxNahim.SelectedItem;

                var newEvent = new EventsViewModel
                {
                    RoomId = selectedRoom.Id.Value,
                    CampusId = selectedRoom.CampusId,
                    Date = EventDateNahim.SelectedDate.Value.Date,
                    Name = EventNameNahim.Text,
                    LessonRange = LessonRangeNahim.Text
                };

                var json = JsonConvert.SerializeObject(newEvent, new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-dd", 
                    NullValueHandling = NullValueHandling.Ignore
                });

                Console.WriteLine($"Отправляем JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_apiUrl, content);
                response.EnsureSuccessStatusCode();

                MessageBox.Show("Мероприятие успешно добавлено!");
                LoadEvents(1, EventsDataGridNahim);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении мероприятия: {ex.Message}");
            }
        }

        private async void DeleteEventNahim_Click(object sender, RoutedEventArgs e)
        {
            if (EventsDataGridNahim.SelectedItem is EventsViewModel selectedEvent)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить выбранное мероприятие?", "Подтверждение", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"{_apiUrl}/{selectedEvent.Id}");
                        response.EnsureSuccessStatusCode();

                        MessageBox.Show("Мероприятие успешно удалено!");
                        LoadEvents(1, EventsDataGridNahim);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении мероприятия: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите мероприятие для удаления.");
            }
        }
        private async void AddEventNezh_Click(object sender, RoutedEventArgs e)
        {
            if (RoomComboBoxNezh.SelectedValue == null || EventDateNezh.SelectedDate == null || string.IsNullOrWhiteSpace(EventNameNezh.Text) || string.IsNullOrWhiteSpace(LessonRangeNezh.Text))
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }

            try
            {
                var selectedRoom = (RoomViewModel)RoomComboBoxNezh.SelectedItem;

                var newEvent = new EventsViewModel
                {
                    RoomId = selectedRoom.Id.Value,
                    CampusId = selectedRoom.CampusId,
                    Date = EventDateNezh.SelectedDate.Value.Date,
                    Name = EventNameNezh.Text,
                    LessonRange = LessonRangeNezh.Text
                };
                var json = JsonConvert.SerializeObject(newEvent, new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-dd",
                    NullValueHandling = NullValueHandling.Ignore
                });
                Console.WriteLine($"Отправляем JSON: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_apiUrl, content);
                response.EnsureSuccessStatusCode();
                MessageBox.Show("Мероприятие успешно добавлено!");
                LoadEvents(2, EventsDataGridNezh);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении мероприятия: {ex.Message}");
            }
        }
        private async void DeleteEventNezh_Click(object sender, RoutedEventArgs e)
        {
            if (EventsDataGridNezh.SelectedItem is EventsViewModel selectedEvent)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить выбранное мероприятие?", "Подтверждение", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"{_apiUrl}/{selectedEvent.Id}");
                        response.EnsureSuccessStatusCode();
                        MessageBox.Show("Мероприятие успешно удалено!");
                        LoadEvents(2, EventsDataGridNezh);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении мероприятия: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите мероприятие для удаления.");
            }
        }

    }
}
