using Microsoft.Win32;
using Newtonsoft.Json;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace AuditoryGenerator
{
    /// <summary>
    /// Логика взаимодействия для WindowAdmin.xaml
    /// </summary>
    public partial class WindowAdmin : Window
    {
        public List<string> loadedSchedulePaths { get; private set; } = new List<string>();
        public WindowAdmin()
        {
            InitializeComponent();
            MainFrame.NavigationService.Navigate(new PageWelcome());
            LoadSchedulePaths();
        }
        private string GetStorageFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loaded_paths.json");
        }

        private async void LoadSchedule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string parsedPath = await ParseScheduleAndSaveAsync();

                loadedSchedulePaths.Clear();
                loadedSchedulePaths.Add(parsedPath);

                SaveSchedulePaths();
                UpdateFileList();
                MessageBox.Show("Расписание успешно получено с сайта!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при парсинге: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task<string> ParseScheduleAndSaveAsync()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            string baseUrl = "https://mpt.ru";
            string scheduleUrl = baseUrl + "/raspisanie/";
            var httpClient = new HttpClient();
            var groupSchedules = new Dictionary<string, GroupData>();
            var scheduleHtml = await httpClient.GetStringAsync(scheduleUrl);
            var scheduleDoc = new HtmlAgilityPack.HtmlDocument();
            scheduleDoc.LoadHtml(scheduleHtml);
            var tabPanels = scheduleDoc.DocumentNode.SelectNodes("//div[@role='tabpanel']");
            if (tabPanels != null)
            {
                foreach (var panel in tabPanels)
                {
                    var groupNameNode = panel.SelectSingleNode(".//h3");
                    if (groupNameNode == null) continue;

                    string fullGroupName = groupNameNode.InnerText.Trim();
                    string groupName = fullGroupName.Replace("Группа ", "").Trim();

                    var groupData = new GroupData();
                    string currentDay = null;
                    string currentBuilding = null;
                    var tables = panel.SelectNodes(".//table[contains(@class, 'table-striped')]");
                    if (tables != null)
                    {
                        foreach (var table in tables)
                        {
                            var rows = table.SelectNodes(".//tr");

                            foreach (var row in rows)
                            {
                                var dayHeader = row.SelectSingleNode(".//th[@colspan='3']");
                                if (dayHeader != null)
                                {
                                    var match = Regex.Match(dayHeader.InnerText.Trim(), @"^(ПОНЕДЕЛЬНИК|ВТОРНИК|СРЕДА|ЧЕТВЕРГ|ПЯТНИЦА|СУББОТА|ВОСКРЕСЕНЬЕ)(.*)$");
                                    if (match.Success)
                                    {
                                        currentDay = match.Groups[1].Value.Trim();
                                        currentBuilding = match.Groups[2].Value.Trim();

                                        if (string.IsNullOrWhiteSpace(currentBuilding))
                                            currentBuilding = "Не определено";

                                        if (!groupData.Days.ContainsKey(currentDay))
                                            groupData.Days[currentDay] = new DayData();

                                        if (!groupData.Days[currentDay].Buildings.ContainsKey(currentBuilding))
                                            groupData.Days[currentDay].Buildings[currentBuilding] = new List<Lesson>();
                                    }
                                    continue;
                                }

                                var cols = row.SelectNodes(".//td");
                                if (cols != null && cols.Count >= 3 && currentDay != null && currentBuilding != null)
                                {
                                    string para = cols[0].InnerText.Trim();
                                    if (currentBuilding == "Не определено")
                                    {
                                        var tempSubjects = cols[1].SelectNodes(".//div[contains(@class, 'label')]") != null
                                            ? cols[1].SelectNodes(".//div[contains(@class, 'label')]")
                                                .Select(n => n.InnerText.Trim()).ToList()
                                            : cols[1].InnerText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                                        bool allPractice = tempSubjects.All(s => s.ToUpper().Contains("ПРАКТИКА"));
                                        string determinedBuilding = allPractice ? "ПРАКТИКА" : "Дистанционно";
                                        if (groupData.Days[currentDay].Buildings.ContainsKey("Не определено"))
                                        {
                                            groupData.Days[currentDay].Buildings.Remove("Не определено");
                                        }
                                        currentBuilding = determinedBuilding;
                                        if (!groupData.Days[currentDay].Buildings.ContainsKey(currentBuilding))
                                            groupData.Days[currentDay].Buildings[currentBuilding] = new List<Lesson>();
                                    }
                                    var subjects = cols[1].SelectNodes(".//div[contains(@class, 'label')]")?
                                        .Select(n => n.InnerText.Trim()).ToList()
                                        ?? cols[1].InnerText.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                                    var teachers = cols[2].SelectNodes(".//div[contains(@class, 'label')]")?
                                        .Select(n => n.InnerText.Trim()).ToList()
                                        ?? cols[2].InnerText.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                                    if (subjects.Count == 2)
                                    {
                                        groupData.Days[currentDay].Buildings[currentBuilding].Add(new Lesson
                                        {
                                            LessonNumber = $"{para} (Числитель)",
                                            Subject = subjects[0],
                                            Teacher = teachers.ElementAtOrDefault(0) ?? "Преподаватель не указан"
                                        });
                                        groupData.Days[currentDay].Buildings[currentBuilding].Add(new Lesson
                                        {
                                            LessonNumber = $"{para} (Знаменатель)",
                                            Subject = subjects[1],
                                            Teacher = teachers.ElementAtOrDefault(1) ?? "Преподаватель не указан"
                                        });
                                    }
                                    else if (subjects.Count == 1)
                                    {
                                        groupData.Days[currentDay].Buildings[currentBuilding].Add(new Lesson
                                        {
                                            LessonNumber = para,
                                            Subject = subjects[0],
                                            Teacher = teachers.ElementAtOrDefault(0) ?? "Преподаватель не указан"
                                        });
                                    }
                                }

                            }
                        }
                    }

                    groupSchedules[groupName] = groupData;
                }
            }

            var result = new GroupSchedule { Groups = groupSchedules };
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"Расписание_МПТ.json";
            string path = Path.Combine(desktop, fileName);
            File.WriteAllText(path, JsonConvert.SerializeObject(result, Formatting.Indented));
            return path;

        }
        private void Exit_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindow main = new MainWindow();
            Close();
            main.Show();
        }
        private async void DownloadTemplate_Click(object sender, MouseButtonEventArgs e)
        {
            var result = MessageBox.Show("Выберите корпус для шаблона Excel:\nДа — Нахимовский\nНет — Нежинская",
                                         "Выбор корпуса",
                                         MessageBoxButton.YesNoCancel,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            string campusName = result == MessageBoxResult.Yes ? "Нахимовский" : "Нежинская";
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"Аудиторник_{campusName}.xlsx";
            string filePath = Path.Combine(desktopPath, fileName);

            try
            {
                var apiService = new ApiService();
                await apiService.GenerateAuditoryFileAsync(filePath);
                MessageBox.Show($"Файл успешно сохранён:\n{filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при создании файла:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RemoveFile_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string fullPath)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить файл: {fullPath}?",
                                             "Подтверждение удаления",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
                for (int i = 0; i < loadedSchedulePaths.Count; i++)
                {
                    if (Path.GetFileName(loadedSchedulePaths[i]).Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        loadedSchedulePaths.Remove(loadedSchedulePaths[i]);
                        SaveSchedulePaths();
                        UpdateFileList();
                        return;
                    }
                }
            }
            else
            {
                MessageBox.Show("Файл не найден в списке (может быть путь отличается?)");
            }
            
        }
        private void SaveSchedulePaths()
        {
            File.WriteAllText(GetStorageFilePath(), JsonConvert.SerializeObject(loadedSchedulePaths));
        }
        public void LoadSchedulePaths()
        {
            string path = GetStorageFilePath();
            if (File.Exists(path))
            {
                var data = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path));
                loadedSchedulePaths = data?.Where(File.Exists).ToList() ?? new List<string>();
                UpdateFileList();
            }

        }
        private void UpdateFileList()
        {
            LoadedFilesList.ItemsSource = null;
            LoadedFilesList.ItemsSource = loadedSchedulePaths.Select(Path.GetFileName).ToList();
        }
        private void TeacherTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MainFrame.Navigate(new TeachersPage());
        }

        private void RoomTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MainFrame.Navigate(new RoomsPage());
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MainFrame.Navigate(new NahimovskyAssignedRoomsPage());
        }

        private void TextBlock_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            MainFrame.Navigate(new NezinskayaAssignedRoomsPage());
        }
        private void TextBlock_MouseLeftButtonUp_2(object sender, MouseButtonEventArgs e)
        {
            MainFrame.Navigate(new Events());
        }
        private void TextBlock_MouseLeftButtonUp_3(object sender, MouseButtonEventArgs e)
        {
            MainFrame.Navigate(new StudyPracticesPage());
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.ResizeMode = ResizeMode.CanResize;
                this.Topmost = false;
            }
            base.OnKeyDown(e);
        }
        private void TextBlock_MouseLeftButtonUp_4(object sender, MouseButtonEventArgs e)
        {
            MainFrame.Navigate(new AuditoryNahim());
        }
        private void TextBlock_MouseLeftButtonUp_5(object sender, MouseButtonEventArgs e)
        {
            MainFrame.Navigate(new AuditoryNezhka());
        }
    }
}
