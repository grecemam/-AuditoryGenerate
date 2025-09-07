using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для TeachersPage.xaml
    /// </summary>
    public partial class TeachersPage : Page
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiUrl = "http://auditoryhelperapi.somee.com/api/Teachers";
        public List<Teacher> TeachersList { get; set; }
        public TeachersPage()
        {
            InitializeComponent();
            LoadTeachers();
        }
        private async void LoadTeachers()
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                TeachersList = JsonSerializer.Deserialize<List<Teacher>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                TeachersDataGrid.ItemsSource = TeachersList;
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }
        private async void AddTeacherButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                MessageBox.Show("Введите ФИО преподавателя.");
                return;
            }

            string newFullName = FullNameTextBox.Text.Trim();

            string pattern = @"^[А-ЯЁ]\.[А-ЯЁ]\.\s[А-ЯЁ][а-яё]+(?:-[А-ЯЁ][а-яё]+)?$";
            if (!Regex.IsMatch(newFullName, pattern))
            {
                MessageBox.Show("Неверный формат ФИО. Введите в формате: И.О. Фамилия (например, И.И. Иванов).");
                return;
            }
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var teachers = JsonSerializer.Deserialize<List<Teacher>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (teachers.Any(t => t.FullName.Equals(newFullName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Такой преподаватель уже существует. Попробуйте ввести другое ФИО.");
                    return;
                }
                var teacher = new Teacher { FullName = newFullName };
                var jsonTeacher = JsonConvert.SerializeObject(teacher);
                var content = new StringContent(jsonTeacher, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync(_apiUrl, content);
                response.EnsureSuccessStatusCode();
                MessageBox.Show("Преподаватель успешно добавлен!");
                LoadTeachers();
                FullNameTextBox.Text = "";
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка при добавлении преподавателя: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private async void EditTeacherButton_Click(object sender, RoutedEventArgs e)
        {
            if (TeachersDataGrid.SelectedItem is Teacher selectedTeacher)
            {
                string newFullName = FullNameTextBox.Text;
                if (string.IsNullOrWhiteSpace(newFullName))
                {
                    MessageBox.Show("Введите ФИО преподавателя.");
                    return;
                }
                string pattern = @"^[А-ЯЁ]\.[А-ЯЁ]\.\s[А-ЯЁ][а-яё]+(?:-[А-ЯЁ][а-яё]+)?$";
                newFullName = newFullName.Trim();
                bool isMatch = Regex.IsMatch(newFullName, pattern, RegexOptions.None);
                if (!isMatch)
                {
                    MessageBox.Show("Неверный формат ФИО. Введите в формате: И.О. Фамилия (например, И.И. Иванов).");
                    return;
                }
                try
                {
                    selectedTeacher.FullName = newFullName;

                    var json = JsonConvert.SerializeObject(selectedTeacher);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"{_apiUrl}/{selectedTeacher.Id}", content);
                    response.EnsureSuccessStatusCode();

                    MessageBox.Show("Преподаватель успешно изменён!");
                    LoadTeachers();
                    FullNameTextBox.Text = "";
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка при редактировании преподавателя: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Выберите преподавателя для редактирования.");
                return;
            }
        }
        private void TeachersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TeachersDataGrid.SelectedItem is Teacher selectedTeacher)
            {
                FullNameTextBox.Text = selectedTeacher.FullName;
            }
        }
        private async void DeleteTeacherButton_Click(object sender, RoutedEventArgs e)
        {
            if (TeachersDataGrid.SelectedItem is Teacher selectedTeacher)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить преподавателя {selectedTeacher.FullName}?",
                                             "Подтверждение удаления",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;

                try
                {
                    var response = await _httpClient.DeleteAsync($"{_apiUrl}/{selectedTeacher.Id}");
                    response.EnsureSuccessStatusCode();

                    MessageBox.Show("Преподаватель успешно удалён!");
                    LoadTeachers();
                    FullNameTextBox.Text = "";
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка при удалении преподавателя: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Выберите преподавателя для удаления.");
                return;
            }
        }


    }
}
