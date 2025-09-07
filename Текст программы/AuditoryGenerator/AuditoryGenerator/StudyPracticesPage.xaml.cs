using DocumentFormat.OpenXml.Spreadsheet;
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
using Page = System.Windows.Controls.Page;

namespace AuditoryGenerator
{
    /// <summary>
    /// Логика взаимодействия для StudyPracticesPage.xaml
    /// </summary>
    public partial class StudyPracticesPage : Page
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _groupsUrl = "http://auditoryhelperapi.somee.com/api/Groups";
        private readonly string _roomsUrl = "http://auditoryhelperapi.somee.com/api/Rooms";
        private readonly string _teachersUrl = "http://auditoryhelperapi.somee.com/api/Teachers";
        public StudyPracticesPage()
        {
            InitializeComponent();
            LoadGroups();
            LoadRooms();
            LoadTeachers();
            LoadPractices(1, PracticeDataGridNahim); 
            LoadPractices(2, PracticeDataGridNezhka);
        }
        private async void LoadGroups()
        {
            try
            {
                var response = await _httpClient.GetAsync(_groupsUrl);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var groups = JsonSerializer.Deserialize<List<Group>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                GroupComboBoxNahim.ItemsSource = groups;
                GroupComboBoxNahim.DisplayMemberPath = "Name";
                GroupComboBoxNahim.SelectedValuePath = "Id";
                GroupComboBoxNezhka.ItemsSource = groups;
                GroupComboBoxNezhka.DisplayMemberPath = "Name";
                GroupComboBoxNezhka.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке групп: {ex.Message}");
            }
        }
        private async void LoadRooms()
        {
            try
            {
                var response = await _httpClient.GetAsync(_roomsUrl);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var rooms = JsonSerializer.Deserialize<List<Room>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var nahimRooms = rooms.FindAll(r => r.CampusId == 1);
                var nezhRooms = rooms.FindAll(r => r.CampusId == 2);
                RoomComboBoxNahimPractice.ItemsSource = nahimRooms;
                RoomComboBoxNahimPractice.DisplayMemberPath = "RoomNumber";
                RoomComboBoxNahimPractice.SelectedValuePath = "Id";
                RoomComboBoxNezhkaPractice.ItemsSource = nezhRooms;
                RoomComboBoxNezhkaPractice.DisplayMemberPath = "RoomNumber";
                RoomComboBoxNezhkaPractice.SelectedValuePath = "Id";
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
                var response = await _httpClient.GetAsync(_teachersUrl);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var teachers = JsonSerializer.Deserialize<List<Teacher>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                TeacherComboBoxNahim.ItemsSource = teachers;
                TeacherComboBoxNahim.DisplayMemberPath = "FullName";
                TeacherComboBoxNahim.SelectedValuePath = "Id";

                TeacherComboBoxNezhka.ItemsSource = teachers;
                TeacherComboBoxNezhka.DisplayMemberPath = "FullName";
                TeacherComboBoxNezhka.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке преподавателей: {ex.Message}");
            }
        }

        private async void LoadPractices(int campusId, DataGrid targetDataGrid)
        {
            try
            {
                var response = await _httpClient.GetAsync("http://auditoryhelperapi.somee.com/api/StudyPractices");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var practices = JsonSerializer.Deserialize<List<StudyPracticeViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var roomsResponse = await _httpClient.GetAsync(_roomsUrl);
                roomsResponse.EnsureSuccessStatusCode();
                var roomsJson = await roomsResponse.Content.ReadAsStringAsync();

                var rooms = JsonSerializer.Deserialize<List<RoomViewModel>>(roomsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var groupsResponse = await _httpClient.GetAsync(_groupsUrl);
                groupsResponse.EnsureSuccessStatusCode();
                var groupsJson = await groupsResponse.Content.ReadAsStringAsync();
                var groups = JsonSerializer.Deserialize<List<Group>>(groupsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var teachersResponse = await _httpClient.GetAsync(_teachersUrl);
                teachersResponse.EnsureSuccessStatusCode();
                var teachersJson = await teachersResponse.Content.ReadAsStringAsync();
                var teachers = JsonSerializer.Deserialize<List<Teacher>>(teachersJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                foreach (var practice in practices)
                {
                    var room = rooms.FirstOrDefault(r => r.Id == practice.RoomId);

                    if (room != null)
                    {
                        practice.RoomNumber = room.RoomNumber;
                    }
                    var group = groups.FirstOrDefault(g => g.Id == practice.GroupId);
                    if (group != null)
                    {
                        practice.GroupName = group.Name;
                    }

                    var teacher = teachers.FirstOrDefault(t => t.Id == practice.TeacherId);
                    if (teacher != null)
                    {
                        practice.TeacherFullName = teacher.FullName;
                    }
                }

                var filteredRooms = rooms.Where(r => r.CampusId == campusId).Select(r => r.Id).ToList();
                var filteredPractices = practices.Where(p => filteredRooms.Contains(p.RoomId)).ToList();

                targetDataGrid.ItemsSource = filteredPractices;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке учебных практик: {ex.Message}");
            }
        }
        private async void AddPracticeNahim_Click(object sender, RoutedEventArgs e)
        {
            if (GroupComboBoxNahim.SelectedValue == null ||
                RoomComboBoxNahimPractice.SelectedValue == null ||
                TeacherComboBoxNahim.SelectedValue == null ||
                PracticeDateNahim.SelectedDate == null ||
                string.IsNullOrWhiteSpace(LessonRangeNahimPractice.Text))
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }
            try
            {
                var newPractice = new StudyPracticeViewModel
                {
                    GroupId = (int)GroupComboBoxNahim.SelectedValue,
                    RoomId = (int)RoomComboBoxNahimPractice.SelectedValue,
                    TeacherId = (int)TeacherComboBoxNahim.SelectedValue,
                    Date = PracticeDateNahim.SelectedDate.Value,
                    LessonRange = LessonRangeNahimPractice.Text
                };

                var json = JsonConvert.SerializeObject(newPractice, new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-dd",
                    NullValueHandling = NullValueHandling.Ignore
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("http://auditoryhelperapi.somee.com/api/StudyPractices", content);
                response.EnsureSuccessStatusCode();

                MessageBox.Show("Учебная практика успешно добавлена!");
                LoadPractices(1, PracticeDataGridNahim);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении учебной практики: {ex.Message}");
            }
        }

        private async void AddPracticeNezhka_Click(object sender, RoutedEventArgs e)
        {
            if (GroupComboBoxNezhka.SelectedValue == null ||
                RoomComboBoxNezhkaPractice.SelectedValue == null ||
                TeacherComboBoxNezhka.SelectedValue == null ||
                PracticeDateNezhka.SelectedDate == null ||
                string.IsNullOrWhiteSpace(LessonRangeNezhkaPractice.Text))
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }
            try
            {
                var newPractice = new StudyPracticeViewModel
                {
                    GroupId = (int)GroupComboBoxNezhka.SelectedValue,
                    RoomId = (int)RoomComboBoxNezhkaPractice.SelectedValue,
                    TeacherId = (int)TeacherComboBoxNezhka.SelectedValue,
                    Date = PracticeDateNezhka.SelectedDate.Value,
                    LessonRange = LessonRangeNezhkaPractice.Text
                };

                var json = JsonConvert.SerializeObject(newPractice, new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-dd",
                    NullValueHandling = NullValueHandling.Ignore
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("http://auditoryhelperapi.somee.com/api/StudyPractices", content);
                response.EnsureSuccessStatusCode();

                MessageBox.Show("Учебная практика для Нежинской успешно добавлена!");

                
                LoadPractices(2, PracticeDataGridNezhka);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении учебной практики для Нежинской: {ex.Message}");
            }
        }
        private async void DeletePracticeNezhka_Click(object sender, RoutedEventArgs e)
        {
            if (PracticeDataGridNezhka.SelectedItem is StudyPracticeViewModel selectedPractice)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить выбранную учебную практику?",
                                             "Подтверждение удаления",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"http://auditoryhelperapi.somee.com/api/StudyPractices/{selectedPractice.Id}");
                        response.EnsureSuccessStatusCode();

                        MessageBox.Show("Учебная практика успешно удалена!");

                        LoadPractices(2, PracticeDataGridNezhka);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении учебной практики: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите учебную практику для удаления.");
            }
        }
        private async void DeletePracticeNahim_Click(object sender, RoutedEventArgs e)
        {
            if (PracticeDataGridNahim.SelectedItem is StudyPracticeViewModel selectedPractice)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить выбранную учебную практику?",
                                             "Подтверждение удаления",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"http://auditoryhelperapi.somee.com/api/StudyPractices/{selectedPractice.Id}");
                        response.EnsureSuccessStatusCode();

                        MessageBox.Show("Учебная практика успешно удалена!");
                        LoadPractices(1, PracticeDataGridNahim);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении учебной практики: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите учебную практику для удаления.");
            }
        }

    }
}

