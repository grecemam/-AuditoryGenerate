using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
using Path = System.IO.Path;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace AuditoryGenerator
{
    /// <summary>
    /// Логика взаимодействия для AuditoryNezhka.xaml
    /// </summary>
    public partial class AuditoryNezhka : System.Windows.Controls.Page
    {
        private List<ChangeEntry> changesForToday = new List<ChangeEntry>();
        private Dictionary<string, Dictionary<int, string>> groupPracticeRooms = new Dictionary<string, Dictionary<int, string>>();
        private ObservableCollection<SelectableTeacher> allTeacherPreferences = new ObservableCollection<SelectableTeacher>();
        private Dictionary<string, string> teacherToRoom = new Dictionary<string, string>();
        private dynamic schedule;
        private int CampusID = 2;
        private List<EventsViewModel> todayEvents = new List<EventsViewModel>();
        private HashSet<string> eventRoomNumbers = new HashSet<string>();
        private HashSet<string> practiceTeachersToday = new HashSet<string>();
        private ICollectionView _teachersView;
        private ObservableCollection<TeacherRoomDisplay> displayRooms = new ObservableCollection<TeacherRoomDisplay>();
        private string auditoryFilePath;
        string dayOfWeek = DateTime.Now.ToString("dddd", new System.Globalization.CultureInfo("ru-RU")).ToUpper();
        private List<Room> nezkaRooms = new List<Room>();
        private List<AssignedRoomViewModel> assignedRooms = new List<AssignedRoomViewModel>();
        public AuditoryNezhka()
        {
            InitializeComponent();
            LoadSavedAuditoryFilePath();
            LoadWeekTypesAsync();
            Loaded += AuditoryNezhka_Loaded;
            var mainWindow = Application.Current.Windows.OfType<WindowAdmin>().FirstOrDefault();
            LoadTodayTeachersFromJsonNezhka();
            LoadDistributedTeachersFromJson("Нежинская");
            DataGridAuditoryNezhka.ItemsSource = displayRooms;
            if (mainWindow != null)
            {
                var paths = mainWindow.loadedSchedulePaths;
                if (paths.Any())
                {
                    string firstSchedulePath = paths.First();
                }
            }
        }
        private async void AuditoryNezhka_Loaded(object sender, RoutedEventArgs e)
        {
            await GetRoomsFromNezhkaAsync();
            await LoadAssignedRoomsAsync();
            await LoadEventsForTodayAsync();
        }
        private async Task LoadEventsForTodayAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var eventsResponse = await client.GetAsync("http://auditoryhelperapi.somee.com/api/Events");
                    var roomsResponse = await client.GetAsync("http://auditoryhelperapi.somee.com/api/Rooms");
                    eventsResponse.EnsureSuccessStatusCode();
                    roomsResponse.EnsureSuccessStatusCode();
                    var eventsJson = await eventsResponse.Content.ReadAsStringAsync();
                    var roomsJson = await roomsResponse.Content.ReadAsStringAsync();
                    var allEvents = JsonConvert.DeserializeObject<List<EventsViewModel>>(eventsJson);
                    var allRooms = JsonConvert.DeserializeObject<List<Room>>(roomsJson);
                    var eventRoomIds = allEvents.Where(e => e.Date.Date == DateTime.Today).Select(e => e.RoomId).ToHashSet();
                    eventRoomNumbers = allRooms.Where(r => eventRoomIds.Contains((int)r.Id) && r.CampusId == 2).Select(r => r.RoomNumber?.Trim()).Where(rn => !string.IsNullOrWhiteSpace(rn)).ToHashSet();
                    Debug.WriteLine($"Событий всего: {todayEvents.Count}");
                    foreach (var e in todayEvents)
                        Debug.WriteLine($"[{e.Date}] {e.Name} в {e.RoomNumber}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке мероприятий: " + ex.Message);
                }
            }
        }
        private async Task LoadAssignedRoomsAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://auditoryhelperapi.somee.com/");
                    var response = await client.GetAsync("api/AssignedRooms");
                    if (response.IsSuccessStatusCode)
                    {
                        assignedRooms = await response.Content.ReadAsAsync<List<AssignedRoomViewModel>>();
                    }
                    else
                    {
                        string errorDetails = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Ошибка при получении закреплённых аудиторий:\n" +
                                        $"Статус: {(int)response.StatusCode} ({response.StatusCode})\n" +
                                        $"Детали: {errorDetails}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошло исключение:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadAuditoryFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel файлы (*.xlsx)|*.xlsx",
                Title = "Выберите файл аудиторного фонда"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                auditoryFilePath = openFileDialog.FileName;
                SaveAuditoryFilePath();
                LoadedAuditoryFileNameNezhka.Text = "Выбран: " + Path.GetFileName(auditoryFilePath);
            }
        }
        private string GetAuditoryFilePathStorage()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(basePath, "AuditoryGenerator", "auditory_path2.json");
        }
        private string GetTeacherPreferencesPath()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuditoryGenerator");
            Directory.CreateDirectory(appDataPath);
            return Path.Combine(appDataPath, "teacher_preferences_nezka.json");
        }
        private void SaveAuditoryFilePath()
        {
            File.WriteAllText(GetAuditoryFilePathStorage(), JsonConvert.SerializeObject(auditoryFilePath));
        }
        private void LoadSavedAuditoryFilePath()
        {
            string path = GetAuditoryFilePathStorage();
            if (File.Exists(path))
            {
                var savedPath = JsonConvert.DeserializeObject<string>(File.ReadAllText(path));
                if (File.Exists(savedPath))
                {
                    auditoryFilePath = savedPath;
                    LoadedAuditoryFileNameNezhka.Text = "Выбран: " + Path.GetFileName(auditoryFilePath);
                }
            }
        }
        private async Task<Dictionary<string, string>> GetGroupAssignedCellsAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://auditoryhelperapi.somee.com/");

                var response = await client.GetAsync("api/Groups");
                if (response.IsSuccessStatusCode)
                {
                    var groups = await response.Content.ReadAsAsync<List<Group>>();
                    return groups.ToDictionary(g => g.Name, g => g.AssignedCells);
                }
                else
                {
                    MessageBox.Show("Ошибка при получении данных групп с API.");
                    return new Dictionary<string, string>();
                }
            }
        }
        private async Task MarkGroupsInAuditoryFileAsync()
        {
            changesForToday = await LoadScheduleChangesAsync();

            if (string.IsNullOrEmpty(auditoryFilePath))
            {
                MessageBox.Show("Файл аудиторного фонда не выбран.");
                return;
            }
            var mainWindow = Application.Current.Windows.OfType<WindowAdmin>().FirstOrDefault();
            if (mainWindow == null || !mainWindow.loadedSchedulePaths.Any())
            {
                MessageBox.Show("Файл расписания не найден.");
                return;
            }
            string schedulePath = mainWindow.loadedSchedulePaths.First();
            if (!File.Exists(schedulePath))
            {
                MessageBox.Show("Файл расписания по пути не существует.");
                return;
            }
            var json = File.ReadAllText(schedulePath);
            schedule = JsonConvert.DeserializeObject(json);
            await TryReplacePracticeWithRoom();
            var groupCells = await GetGroupAssignedCellsAsync();
            TryCloseExcelIfFileIsLocked(auditoryFilePath);
            using (var workbook = new XLWorkbook(auditoryFilePath))
            {
                var worksheet = workbook.Worksheet(1);
                var teachersToDistribute = new HashSet<string>();
                string currentDay = DateTime.Now.ToString("dddd", new System.Globalization.CultureInfo("ru-RU")).ToUpper();
                worksheet.Range("D1:N1").Merge().Value = currentDay;
                worksheet.Range("D1:N1").Style.Font.SetBold().Font.SetFontSize(36).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
                string currentDateText = DateTime.Now.ToString("d MMMM yyyy г.", new System.Globalization.CultureInfo("ru-RU"));
                var dateCell = worksheet.Range("H2:R2").Merge().FirstCell();
                dateCell.Value = currentDateText;
                dateCell.Style.Font.SetBold().Font.SetFontSize(36).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
                string selectedWeekType = (WeekTypeComboBoxNezhka.SelectedItem as WeekType)?.Name?.ToUpper();
                worksheet.Range("U1:Y1").Merge().Value = selectedWeekType;
                worksheet.Range("U1:Y1").Style.Font.SetBold().Font.SetFontSize(36).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
                teacherToRoom.Clear();
                var allTeachersToday = new HashSet<string>();
                var teacherLessonsToday = new Dictionary<string, List<int>>();
                var groupsObject = schedule.Groups as JObject;
                if (groupsObject != null)
                {
                    foreach (var g in groupsObject.Properties())
                    {
                        var lessons = g.Value["Days"]?[dayOfWeek]?["Buildings"]?["Нежинская"];
                        if (lessons == null) continue;
                        foreach (var lesson in lessons)
                        {
                            string pairKey = lesson["LessonNumber"]?.ToString()?.ToUpper();
                            if (!IsPairForCurrentWeek(pairKey, selectedWeekType)) continue;
                            string teacherName = lesson["Teacher"]?.ToString();
                            if (string.IsNullOrWhiteSpace(teacherName)) continue;
                            var names = teacherName.Split(',').Select(n => n.Trim()).Where(n => !string.IsNullOrWhiteSpace(n) && !n.Equals("ПРЕПОДАВАТЕЛЬ НЕ УКАЗАН", StringComparison.OrdinalIgnoreCase));
                            var match = System.Text.RegularExpressions.Regex.Match(pairKey, @"\d+");
                            if (!match.Success || !int.TryParse(match.Value, out int pairNumber)) continue;
                            foreach (var name in names)
                            {
                                if (!teacherLessonsToday.ContainsKey(name))
                                    teacherLessonsToday[name] = new List<int>();
                                teacherLessonsToday[name].Add(pairNumber);
                                if (!teachersToDistribute.Contains(name))
                                {
                                    teachersToDistribute.Add(name);
                                    Debug.WriteLine($"👤 Препод добавлен для распределения: {name}");
                                }
                            }
                        }
                    }
                }
                var freeTeachers = teachersToDistribute
                .Where(t =>!assignedRooms.Any(ar =>ar.TeacherName.Trim().Equals(t, StringComparison.OrdinalIgnoreCase)&& ar.CampusId == 2)&& !t.Equals("ПРЕПОДАВАТЕЛЬ НЕ УКАЗАН", StringComparison.OrdinalIgnoreCase)&& DoesTeacherHaveClassToday(t, groupsObject, dayOfWeek, selectedWeekType))
                .ToList();
                var busyRooms = assignedRooms.Where(r =>r.CampusId == 2 && DoesTeacherHaveClassToday(r.TeacherName.Trim(), groupsObject, dayOfWeek, selectedWeekType)).Select(r => r.RoomNumber).Distinct().ToHashSet();
                foreach (var room in eventRoomNumbers)
                {
                    busyRooms.Add(room);
                    Debug.WriteLine($"📌 Мероприятие — исключена аудитория {room}");
                }
                var nezkaRoomNumbers = nezkaRooms.Select(r => r.RoomNumber.Trim()).Where(rn => !busyRooms.Contains(rn)).ToList();
                var savedPreferences = LoadTeacherPreferencesFromFile();
                var freeRoomsByFloor = nezkaRoomNumbers.Where(r => !busyRooms.Contains(r)).GroupBy(room =>{if (room.StartsWith("3")) return 3;if (room.StartsWith("2")) return 2;if (room.StartsWith("1")) return 1;return 0;}).ToDictionary(g => g.Key, g => new Queue<string>(g));
                int roomIndex = 0;
                foreach (var teacherFull in freeTeachers)
                {
                    var individualNames = teacherFull.Split(',').Select(name => name.Trim()).Where(name => !string.IsNullOrEmpty(name)).ToList();
                    var sortedTeachers = freeTeachers.SelectMany(t => t.Split(',').Select(name => name.Trim())).Where(name => !string.IsNullOrEmpty(name)).OrderBy(name => teacherLessonsToday.ContainsKey(name) ? teacherLessonsToday[name].Min() : int.MaxValue).ToList();
                    var activeRooms = new Dictionary<string, (string roomNumber, int lastPair)>();
                    Debug.WriteLine($"🏫 Всего доступных кабинетов: {nezkaRoomNumbers.Count}");
                    foreach (var teacher in sortedTeachers)
                    {
                        if (teacherToRoom.ContainsKey(teacher)) continue;
                        string availableRoom = null;
                        var teacherPref = savedPreferences.FirstOrDefault(p => p.Name.Equals(teacher, StringComparison.OrdinalIgnoreCase));
                        if (teacherPref != null && teacherPref.PreferredFloor != 0)
                        {
                            if (freeRoomsByFloor.TryGetValue(teacherPref.PreferredFloor, out var roomsOnPreferredFloor) && roomsOnPreferredFloor.Any())
                            {
                                availableRoom = roomsOnPreferredFloor.Dequeue();
                                Debug.WriteLine($"🏢 Учитываем пожелание: {teacher} на {teacherPref.PreferredFloor} этаж -> кабинет {availableRoom}");
                            }
                        }
                        if (availableRoom == null)
                        {
                            availableRoom = nezkaRoomNumbers.Except(busyRooms).FirstOrDefault();
                        }
                        if (availableRoom == null)
                        {
                            foreach (var kvp in activeRooms.ToList())
                            {
                                if (teacherLessonsToday.ContainsKey(kvp.Key))
                                {
                                    int lastPair = teacherLessonsToday[kvp.Key].Max();
                                    if (teacherLessonsToday[teacher].Min() > lastPair)
                                    {
                                        availableRoom = kvp.Value.roomNumber;
                                        Debug.WriteLine($"🔄 Пересадили {teacher} в кабинет {availableRoom}, освободившийся от {kvp.Key} после пары {teacherLessonsToday[kvp.Key].Max()}");
                                        Debug.WriteLine($"🏁 Кабинет {kvp.Value.roomNumber} освобождён после пары {teacherLessonsToday[kvp.Key].Max()} преподавателем {kvp.Key}");
                                        activeRooms.Remove(kvp.Key);
                                        break;
                                    }
                                }
                            }
                        }
                        if (availableRoom != null)
                        {
                            teacherToRoom[teacher] = availableRoom;
                            Debug.WriteLine($"✅ Назначен кабинет {availableRoom} для {teacher} с {string.Join(", ", teacherLessonsToday[teacher])} пар");
                            busyRooms.Add(availableRoom);

                            if (teacherLessonsToday.ContainsKey(teacher))
                            {
                                activeRooms[teacher] = (availableRoom, teacherLessonsToday[teacher].Max());
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"⚠ Не удалось найти аудиторию для {teacher}");
                            Debug.WriteLine($"⚠ Не найден кабинет для {teacher}. Пары: {string.Join(", ", teacherLessonsToday[teacher])}");
                        }
                    }
                }
                Debug.WriteLine($"🚫 Осталось свободных кабинетов: {nezkaRoomNumbers.Except(busyRooms).Count()}");
                foreach (var groupEntry in schedule.Groups)
                {
                    XLColor fillColor = XLColor.ArylideYellow;
                    string fullKey = groupEntry.Name;
                    if (!groupCells.TryGetValue(fullKey, out string cellRange))
                        continue;
                    var range = worksheet.Range(cellRange);
                    var cells = range.Cells().ToList();
                    foreach (var cell in cells)
                    {
                        cell.Value = " ";
                        cell.Style.Fill.BackgroundColor = XLColor.White;
                        cell.Style.Fill.PatternType = XLFillPatternValues.Solid;
                        cell.Style.Font.FontColor = XLColor.Black;
                        cell.Style.Font.Bold = false;
                    }
                    var dayObj = groupEntry.Value["Days"]?[dayOfWeek];
                    if (dayObj == null || dayObj["Buildings"] == null)
                    {
                        foreach (var cell in cells)
                            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        continue;
                    }
                    bool skipGroup = false;
                    string labelToWrite = null;
                    var buildings = dayObj["Buildings"];
                    foreach (var bld in buildings)
                    {
                        string buildingName = bld.Name.ToString().ToUpper();
                        var lessons = bld.Value;
                        if (buildingName.Contains("НАХИМОВСКИЙ"))
                        {
                            labelToWrite = "НАХИМОВСКИЙ";
                            skipGroup = true;
                            break;
                        }
                        if (buildingName.Contains("ДИСТАНЦИОННО"))
                        {
                            labelToWrite = "ДИСТАНЦИОННО";
                            skipGroup = true;
                            break;
                        }
                        foreach (var lesson in lessons)
                        {
                            string subject = lesson["Subject"]?.ToString().ToUpper();
                            if (subject.Contains("ПРАКТИКА"))
                            {
                                labelToWrite = "ПРАКТИКА";
                                skipGroup = true;
                                break;
                            }
                        }
                        if (skipGroup) break;
                    }
                    
                    if (labelToWrite != null)
                    {
                        var chars = labelToWrite.ToCharArray();
                        int totalCells = cells.Count;
                        int totalChars = chars.Length;
                        fillColor = XLColor.ArylideYellow;
                        if (labelToWrite.Contains("ДИСТАНЦ")) fillColor = XLColor.LightGray;
                        else if (labelToWrite.Contains("ПРАКТИКА")) fillColor = XLColor.LightPink;
                        if (totalChars <= totalCells)
                        {
                            for (int i = 0; i < totalChars; i++)
                            {
                                cells[i].Value = chars[i].ToString();
                                cells[i].Style.Fill.BackgroundColor = fillColor;
                            }
                        }
                        else
                        {
                            int avg = totalChars / totalCells;
                            int extra = totalChars % totalCells;
                            int charIndex = 0;

                            for (int i = 0; i < totalCells; i++)
                            {
                                int lettersThisCell = avg + (i < extra ? 1 : 0);
                                string value = new string(chars.Skip(charIndex).Take(lettersThisCell).ToArray());
                                cells[i].Value = value;
                                cells[i].Style.Fill.BackgroundColor = fillColor;
                                charIndex += lettersThisCell;
                            }
                        }
                    }
                    var changesForThisGroup = changesForToday.Where(c => string.Equals(c.GroupName, fullKey, StringComparison.OrdinalIgnoreCase)).ToList();
                    bool isReplacedToNahim = changesForThisGroup.Any(c => c.RawText?.ToUpper().Contains("НАХИМОВСКИЙ") == true);

                    if (isReplacedToNahim)
                    {
                        Debug.WriteLine($"⛔ Группа {fullKey} — замена в корпус НЕЖИНСКАЯ. Пропускаем обработку и записываем метку.");

                        labelToWrite = "НАХИМОВСКИЙ";
                        var chunks = SplitLabelSmart(labelToWrite, cells.Count);
                        for (int i = 0; i < chunks.Count && i < cells.Count; i++)
                        {
                            cells[i].Value = chunks[i];
                            cells[i].Style.Fill.BackgroundColor = XLColor.FromHtml("#CDA9CF"); // или LightBlue
                            cells[i].Style.Font.FontColor = XLColor.Black;
                            cells[i].Style.Font.Bold = true;
                        }


                        continue;
                    }
                    var groupInSchedule = ((JObject)schedule.Groups).Properties().FirstOrDefault(p => p.Name.Equals(fullKey, StringComparison.OrdinalIgnoreCase))?.Value;
                    var groupBuildings = groupInSchedule?["Days"]?[dayOfWeek]?["Buildings"];
                    if (groupBuildings != null)
                    {
                        if (changesForThisGroup.Any(c => c.RawText?.ToUpper().Contains("НЕЖИНСКАЯ") == true))
                        {
                            skipGroup = false;
                            labelToWrite = null;
                            Debug.WriteLine($"✅ В замене указан корпус НЕЖИНСКАЯ — НЕ пропускаем группу {fullKey}");

                            foreach (var cell in cells)
                            {
                                cell.Value = " ";
                                cell.Style.Fill.BackgroundColor = XLColor.White;
                                cell.Style.Fill.PatternType = XLFillPatternValues.Solid;
                                cell.Style.Font.FontColor = XLColor.Black;
                                cell.Style.Font.Bold = false;
                            }
                        }
                        else
                        {
                            foreach (var b in groupBuildings.Children<JProperty>())
                            {
                                if (b.Name.ToUpper().Contains("НАХИМОВСКИЙ"))
                                {
                                    Debug.WriteLine($"⛔ Пропускаем замены для группы {fullKey} — сегодня она на Нахимовском");
                                    changesForThisGroup.Clear();
                                    break;
                                }
                            }
                        }

                    }
                    foreach (var change in changesForThisGroup)
                    {
                        if (skipGroup)
                            continue;
                        int pairIndex = change.PairNumber;
                        if (pairIndex < 0 || pairIndex >= cells.Count)
                            continue;
                        var targetCell = cells[pairIndex];
                        string rawSubjectText = change.RawText?.ToLowerInvariant() ?? "";
                        if (change.Teachers.Count == 0 && rawSubjectText.Contains("отмен"))
                        {
                            bool isGroupDistantToday = groupBuildings?.Children<JProperty>().Any(b => b.Name.ToUpper().Contains("ДИСТАНЦИОННО")) == true;
                            targetCell.Value = "";
                            targetCell.Style.Fill.BackgroundColor = isGroupDistantToday ? XLColor.LightGray : XLColor.White;
                            continue;
                        }
                        var roomList = new List<string>();
                        foreach (var teacher in change.Teachers)
                        {
                            string teacherPattern = Regex.Escape(teacher);
                            var match = Regex.Match(change.RawText ?? "", $@"\b{teacherPattern}\b\s*\(([^)]*отмен[^)]*)\)", RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                Debug.WriteLine($"🚫 Преподаватель {teacher} — занятие отменено, не добавляем кабинет");
                                continue;
                            }
                            string room = null;
                            var assigned = assignedRooms.FirstOrDefault(r =>r.TeacherName.Trim().Equals(teacher, StringComparison.OrdinalIgnoreCase)&& r.CampusId == 2);
                            if (assigned != null)
                            {
                                room = assigned.RoomNumber;
                            }
                            else if (teacherToRoom.TryGetValue(teacher, out string dynRoom))
                            {
                                room = dynRoom;
                            }
                            else
                            {
                                var availableRoom = nezkaRoomNumbers.Except(busyRooms).FirstOrDefault();
                                if (availableRoom != null)
                                {
                                    room = availableRoom;
                                    busyRooms.Add(room);
                                    teacherToRoom[teacher] = room;
                                    Debug.WriteLine($"🆕 В замене: назначен свободный кабинет {room} для преподавателя {teacher}");
                                }
                                else
                                {
                                    Debug.WriteLine($"⚠️ В замене не удалось найти свободный кабинет для {teacher}");
                                }
                            }

                            if (!string.IsNullOrEmpty(room))
                            {
                                roomList.Add(room);
                            }
                        }
                        if (roomList.Any())
                        {
                            bool isGroupDistantToday = groupBuildings?.Children<JProperty>().Any(b => b.Name.ToUpper().Contains("ДИСТАНЦИОННО")) == true;
                            if (isGroupDistantToday)
                            {
                                Debug.WriteLine($"⛔ Группа {fullKey} — дистанционно, замена есть, пропускаем вставку кабинета");
                                continue;
                            }
                            targetCell.Value = string.Join(Environment.NewLine, roomList.Distinct());
                            targetCell.Style.Alignment.WrapText = true;
                            targetCell.Style.Font.FontColor = XLColor.Black;
                            targetCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#CDA9CF");
                        }
                    }
                    var overridePairs = new HashSet<int>(changesForThisGroup.Select(c => c.PairNumber));
                    if (!skipGroup)
                    {
                        var buildingToday = groupEntry.Value["Days"]?[dayOfWeek]?["Buildings"]?["Нежинская"];
                        if (buildingToday == null)
                            continue;
                        foreach (var lessonObj in buildingToday)
                        {
                            string pairKey = lessonObj["LessonNumber"]?.ToString();
                            if (string.IsNullOrWhiteSpace(pairKey))
                                continue;
                            selectedWeekType = (WeekTypeComboBoxNezhka.SelectedItem as WeekType)?.Name?.ToUpper();
                            if (pairKey.ToUpper().Contains("ЗНАМЕНАТЕЛЬ") && selectedWeekType != "ЗНАМЕНАТЕЛЬ") continue;
                            if (pairKey.ToUpper().Contains("ЧИСЛИТЕЛЬ") && selectedWeekType != "ЧИСЛИТЕЛЬ") continue;
                            var match = System.Text.RegularExpressions.Regex.Match(pairKey, @"\d+");
                            if (!match.Success || !int.TryParse(match.Value, out int pairNumber)) continue;
                            if (overridePairs.Contains(pairNumber)) continue;
                            int cellIndex = pairNumber;
                            if (cellIndex < 0 || cellIndex >= cells.Count) continue;
                            string teacherName = lessonObj["Teacher"]?.ToString();
                            if (string.IsNullOrWhiteSpace(teacherName)) continue;
                            var individualNames = teacherName.Split(',').Select(name => name.Trim()).Where(name => !string.IsNullOrEmpty(name)).ToList();
                            var roomList = new List<string>();
                            foreach (var singleTeacher in individualNames)
                            {
                                var assigned = assignedRooms.FirstOrDefault(r => r.TeacherName.Trim().Equals(singleTeacher, StringComparison.OrdinalIgnoreCase) && r.CampusId == 2);
                                if (assigned != null)
                                {
                                    roomList.Add(assigned.RoomNumber);
                                    continue;
                                }
                                if (teacherToRoom.TryGetValue(singleTeacher, out string dynamicRoom))
                                {
                                    roomList.Add(dynamicRoom);
                                }
                            }
                            if (roomList.Any())
                            {
                                var cell = cells[cellIndex];
                                cell.Value = string.Join(Environment.NewLine, roomList.Distinct());
                                cell.Style.Alignment.WrapText = true;
                                cell.Style.Font.FontColor = XLColor.Black;
                                cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                            }
                        }
                    }
                }
                var parsedGroups = ((Newtonsoft.Json.Linq.JObject)schedule.Groups).Properties().Select(p => p.Name).ToHashSet();
                var missingGroups = groupCells.Keys.Where(groupName => !parsedGroups.Contains(groupName)).ToList();
                foreach (var groupName in missingGroups)
                {
                    if (!groupCells.TryGetValue(groupName, out string cellRange))
                        continue;
                    var range = worksheet.Range(cellRange);
                    var cells = range.Cells().ToList();
                    var label = "ПРАКТИКА";
                    var chunks = SplitLabelSmart(label, cells.Count);
                    for (int i = 0; i < chunks.Count && i < cells.Count; i++)
                    {
                        cells[i].Value = chunks[i];
                        cells[i].Style.Fill.BackgroundColor = XLColor.LightPink;
                    }
                }
                foreach (var kvp in groupPracticeRooms)
                {
                    string groupName = kvp.Key;
                    if (!groupCells.TryGetValue(groupName, out string cellRange))
                        continue;
                    var range = worksheet.Range(cellRange);
                    var cells = range.Cells().ToList();
                    foreach (var cell in cells)
                    {
                        cell.Value = " ";
                        cell.Style.Fill.BackgroundColor = XLColor.White;
                        cell.Style.Font.FontColor = XLColor.Black;
                    }
                    foreach (var pairKvp in kvp.Value)
                    {
                        int pairIndex = pairKvp.Key;
                        string room = pairKvp.Value;

                        if (pairIndex + 1 >= 0 && pairIndex + 1 < cells.Count)
                        {
                            cells[pairIndex + 1].Value = room;
                            cells[pairIndex + 1].Style.Fill.BackgroundColor = XLColor.BabyPink;
                            cells[pairIndex + 1].Style.Font.FontColor = XLColor.Black;
                        }

                    }
                }
                displayRooms.Clear();
                int index = 1;
                foreach (var ar in assignedRooms)
                {
                    if (ar.CampusId != 2) continue;
                    string teacherName = ar.TeacherName.Trim();
                    if (!teacherToRoom.ContainsKey(teacherName)
                        && DoesTeacherHaveClassToday(teacherName, groupsObject, dayOfWeek, selectedWeekType))
                    {
                        teacherToRoom[teacherName] = ar.RoomNumber;

                        Debug.WriteLine($"🏷 Закреплённый кабинет {ar.RoomNumber} добавлен для {teacherName}");
                    }
                }
                foreach (var kvp in teacherToRoom)
                {
                    displayRooms.Add(new TeacherRoomDisplay
                    {
                        Id = index++,
                        TeacherName = kvp.Key,
                        RoomNumber = kvp.Value
                    });
                }
                ShowTodaysChangesWithRooms();
                SaveDistributedTeachersToJson("Нежинская");
                workbook.Save();
                try
                {
                    await OpenExcelAndTrack(auditoryFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при открытии или отслеживании Excel файла: " + ex.Message);
                }

            }
        }
        private async Task TryReplacePracticeWithRoom()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync("http://auditoryhelperapi.somee.com/api/StudyPractices");
                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Ошибка при получении данных учебных практик.");
                        return;
                    }
                    var json = await response.Content.ReadAsStringAsync();
                    var allPractices = JsonConvert.DeserializeObject<List<StudyPracticeViewModel>>(json);
                    var relevantPractices = allPractices.Where(p =>{var room = nezkaRooms.FirstOrDefault(r => r.Id == p.RoomId);return p.Date.Date == DateTime.Today && room != null && room.CampusId == CampusID;}).ToList();
                    practiceTeachersToday.Clear();
                    foreach (var practice in relevantPractices)
                    {
                        var group = await GetGroupById(practice.GroupId);
                        var room = nezkaRooms.FirstOrDefault(r => r.Id == practice.RoomId);
                        var teacher = await GetTeacherById(practice.TeacherId);

                        if (group == null || room == null || teacher == null)
                            continue;
                        assignedRooms.Add(new AssignedRoomViewModel
                        {
                            CampusId = CampusID,
                            RoomNumber = room.RoomNumber,
                            TeacherName = teacher.FullName
                        });
                        teacherToRoom[teacher.FullName] = room.RoomNumber;
                        practiceTeachersToday.Add(teacher.FullName);
                        if (!groupPracticeRooms.ContainsKey(group.Name))
                            groupPracticeRooms[group.Name] = new Dictionary<int, string>();
                        groupPracticeRooms[group.Name] = new Dictionary<int, string>();
                        for (int pair = practice.PairStart; pair <= practice.PairEnd; pair++)
                        {
                            groupPracticeRooms[group.Name][pair - 1] = room.RoomNumber;
                        }
                        Debug.WriteLine($"✅ Заменили ПРАКТИКУ: группа {group.Name}, кабинет {room.RoomNumber}, преподаватель {teacher.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при замене ПРАКТИКИ: " + ex.Message);
                }
            }
        }
        private async Task<Group> GetGroupById(int groupId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"http://auditoryhelperapi.somee.com/api/Groups/{groupId}");
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadAsAsync<Group>();
            }
        }
        private async Task<Teacher> GetTeacherById(int teacherId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"http://auditoryhelperapi.somee.com/api/Teachers/{teacherId}");
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadAsAsync<Teacher>();
            }
        }
        private async Task OpenExcelAndTrack(string filepath)
        {
            try
            {
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filepath,
                    UseShellExecute = true
                });

                if (process != null)
                {
                    await Task.Run(() =>
                    {
                        process.WaitForExit();
                    });

                    MessageBox.Show("Excel файл был закрыт, теперь файл отправляется на сервер.");
                    await UploadFileToServerAsync(filepath, CampusID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть файл: " + ex.Message);
            }
        }
        private bool DoesTeacherHaveClassToday(string teacher, JObject groupsObject, string dayOfWeek, string selectedWeekType)
        {
            selectedWeekType = selectedWeekType?.ToUpper();
            if (practiceTeachersToday.Contains(teacher))
                return true;
            foreach (var g in groupsObject.Properties())
            {
                var lessons = g.Value["Days"]?[dayOfWeek]?["Buildings"]?["Нежинская"];
                if (lessons == null) continue;
                foreach (var lesson in lessons)
                {
                    string ln = lesson["LessonNumber"]?.ToString() ?? "";
                    if (!IsPairForCurrentWeek(ln, selectedWeekType)) continue;
                    string t = lesson["Teacher"]?.ToString() ?? "";
                    if (t.Split(',').Any(x => x.Trim().Equals(teacher, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            return false;
        }



        private async Task LoadWeekTypesAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://auditoryhelperapi.somee.com/");
                var response = await client.GetAsync("api/WeekTypes");
                if (response.IsSuccessStatusCode)
                {
                    var weekTypes = await response.Content.ReadAsAsync<List<WeekType>>();
                    WeekTypeComboBoxNezhka.ItemsSource = weekTypes;
                    WeekTypeComboBoxNezhka.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("Ошибка при загрузке типов недели");
                }
            }
        }

        private void TryCloseExcelIfFileIsLocked(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            foreach (var process in Process.GetProcessesByName("EXCEL"))
            {
                try
                {
                    if (process.MainWindowTitle.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        process.Kill();
                        process.WaitForExit();
                        break;
                    }
                }
                catch { }
            }
        }
        private async Task<List<ChangeEntry>> LoadScheduleChangesAsync()
        {
            var result = new List<ChangeEntry>();
            string today = DateTime.Now.ToString("dd.MM.yyyy");
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            var httpClient = new HttpClient(handler);
            string html = await httpClient.GetStringAsync("https://mpt.ru/izmeneniya-v-raspisanii/");
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var h4s = doc.DocumentNode.SelectNodes("//h4[contains(text(), 'Замены на')]");
            if (h4s == null) return result;
            foreach (var h4 in h4s)
            {
                if (!h4.InnerText.Contains(today)) continue;
                var node = h4.NextSibling;
                while (node != null && !(node.Name == "h4" && node.InnerText.Contains("Замены на")))
                {
                    if (node.Name == "div" && node.SelectSingleNode(".//table[contains(@class, 'table-striped')]") != null)
                    {
                        var table = node.SelectSingleNode(".//table");
                        if (table == null) { node = node.NextSibling; continue; }
                        var captionNode = table.SelectSingleNode(".//caption/b");
                        if (captionNode == null) { node = node.NextSibling; continue; }
                        string groupName = captionNode.InnerText.Trim();
                        var rows = table.SelectNodes(".//tr");
                        if (rows == null || rows.Count < 2) { node = node.NextSibling; continue; }
                        foreach (var row in rows.Skip(1))
                        {
                            var cols = row.SelectNodes(".//td");
                            if (cols == null || cols.Count < 4) continue;
                            string pair = cols[0].InnerText.Trim();
                            string newSubject = cols[2].InnerText.Trim();
                            if (!int.TryParse(pair, out int pairNumber)) continue;
                            /*var teacherMatches1 = Regex.Matches(newSubject, @"[А-ЯЁ][а-яё]+\s[А-Я]\.[А-Я]\.");
                            var teacherMatches2 = Regex.Matches(newSubject, @"[А-Я]\.[А-Я]\.\s[А-ЯЁ][а-яё]+");
                            var teachers = teacherMatches1.Cast<Match>().Concat(teacherMatches2.Cast<Match>()).Select(m => m.Value.Trim()).Distinct().ToList();*/
                            var teacherMatches = Regex.Matches(newSubject, @"\b[А-Я]\.[А-Я]\.\s[А-ЯЁ][а-яё]+\b");
                            var teachers = teacherMatches.Cast<Match>().Select(m => m.Value.Trim()).Distinct().ToList();

                            result.Add(new ChangeEntry
                            {
                                GroupName = groupName,
                                PairNumber = pairNumber,
                                Teachers = teachers,
                                RawText = newSubject
                            });
                        }
                    }
                    node = node.NextSibling;
                }
            }
            return result;
        }
        private void ShowTodaysChangesWithRooms()
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"🔍 Найдено {changesForToday.Count} изменений");
            foreach (var change in changesForToday)
            {
                messageBuilder.AppendLine($"Проверка группы: {change.GroupName}, пара: {change.PairNumber}, преподы: {string.Join(", ", change.Teachers)}");
                bool isOnNezhka = true;
                var groupEntry = ((JObject)schedule.Groups).Properties().FirstOrDefault(p => p.Name.Equals(change.GroupName, StringComparison.OrdinalIgnoreCase))?.Value;
                if (groupEntry == null)
                {
                    messageBuilder.AppendLine($"❌ Группа {change.GroupName} не найдена в расписании (регистр может отличаться)");
                    continue;
                }
                var buildings = groupEntry["Days"]?[dayOfWeek]?["Buildings"];
                if (buildings == null)
                {
                    messageBuilder.AppendLine($"❌ У группы {change.GroupName} нет расписания на {dayOfWeek}");
                    continue;
                }
                foreach (var b in buildings.Children<JProperty>())
                {
                    string building = b.Name.ToUpper();
                    if (building.Contains("НАХИМ"))
                    {
                        messageBuilder.AppendLine($"⛔ {change.GroupName} сегодня на Нахимовском — пропускаем");
                        isOnNezhka = false;
                        break;
                    }
                }
                if (!isOnNezhka) continue;
                foreach (var teacher in change.Teachers)
                {
                    string rawTextLower = change.RawText?.ToLowerInvariant() ?? "";
                    string teacherLower = teacher.ToLowerInvariant();
                    string teacherPattern = Regex.Escape(teacher);
                    var match = Regex.Match(rawTextLower, $@"\b{teacherPattern}\b\s*\(([^)]*отмен[^)]*)\)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        messageBuilder.AppendLine($"🚫 {change.GroupName} {change.PairNumber} {teacher} — занятие отменено (по контексту)");
                        continue;
                    }
                    string room = null;
                    var assigned = assignedRooms.FirstOrDefault(r => r.TeacherName.Trim().Equals(teacher, StringComparison.OrdinalIgnoreCase) && r.CampusId == 2);
                    if (assigned != null)
                    {
                        room = assigned.RoomNumber;
                    }
                    else if (teacherToRoom.TryGetValue(teacher, out string dynamicRoom))
                    {
                        room = dynamicRoom;
                    }
                    messageBuilder.AppendLine($"✅ {change.GroupName} {change.PairNumber} {teacher} → {room ?? "null"}");
                }
            }
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Изменения_на_сегодня.txt");
            File.WriteAllText(filePath, messageBuilder.ToString(), Encoding.UTF8);
            Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = filePath, UseShellExecute = true });
        }
        private async Task<List<Room>> GetRoomsFromNezhkaAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://auditoryhelperapi.somee.com/");
                var response = await client.GetAsync("api/Rooms");

                if (response.IsSuccessStatusCode)
                {
                    var rooms = await response.Content.ReadAsAsync<List<Room>>();
                    nezkaRooms = rooms.Where(r => r.CampusId == 2).ToList();
                    return nezkaRooms;
                }
                else
                {
                    MessageBox.Show("Ошибка при получении списка аудиторий.");
                    return new List<Room>();
                }
            }
        }
        private List<string> SplitLabelSmart(string label, int cellCount)
        {
            var chars = label.ToCharArray();
            if (chars.Length <= cellCount)
            {
                return chars.Select(c => c.ToString()).ToList();
            }
            var result = new List<string>();
            int baseLen = chars.Length / cellCount;
            int extra = chars.Length % cellCount;
            int index = 0;
            for (int i = 0; i < cellCount; i++)
            {
                int take = baseLen + (i < extra ? 1 : 0);
                var chunk = new string(chars, index, take);
                result.Add(chunk);
                index += take;
            }
            return result;
        }
        private async void ApplyMarking_Click(object sender, RoutedEventArgs e)
        {
            await MarkGroupsInAuditoryFileAsync();
        }
        private void LoadTodayTeachersFromJsonNezhka()
        {
            var mainWindow = Application.Current.Windows.OfType<WindowAdmin>().FirstOrDefault();
            if (mainWindow == null || !mainWindow.loadedSchedulePaths.Any()) return;
            string schedulePath = mainWindow.loadedSchedulePaths.First();
            if (!File.Exists(schedulePath)) return;
            string json = File.ReadAllText(schedulePath);
            dynamic schedule = JsonConvert.DeserializeObject(json);
            string currentDay = DateTime.Now.ToString("dddd", new System.Globalization.CultureInfo("ru-RU")).ToUpper();
            string selectedWeekType = (WeekTypeComboBoxNezhka.SelectedItem as WeekType)?.Name?.ToUpper();
            var teachersToday = new HashSet<string>();
            foreach (var group in schedule.Groups)
            {
                var lessons = group.Value["Days"]?[currentDay]?["Buildings"]?["Нежинская"];
                if (lessons == null) continue;
                foreach (var lesson in lessons)
                {
                    string lessonNumber = lesson["LessonNumber"]?.ToString() ?? "";
                    if (!IsPairForCurrentWeek(lessonNumber, selectedWeekType)) continue;
                    string teacher = lesson["Teacher"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(teacher) && !teacher.Trim().Equals("ПРЕПОДАВАТЕЛЬ НЕ УКАЗАН", StringComparison.OrdinalIgnoreCase))
                        teachersToday.Add(teacher.Trim());
                }
            }
            var saved = LoadTeacherPreferencesFromFile();
            allTeacherPreferences.Clear();
            foreach (var teacherName in teachersToday)
            {
                var savedPref = saved.FirstOrDefault(s => s.Name == teacherName);
                allTeacherPreferences.Add(new SelectableTeacher { Name = teacherName, IsSelected = savedPref?.IsSelected ?? false, PreferredFloor = savedPref?.PreferredFloor ?? 0 });
            }
            TeachersListBoxNezhka.ItemsSource = null;
            TeachersListBoxNezhka.ItemsSource = allTeacherPreferences;
            _teachersView = CollectionViewSource.GetDefaultView(TeachersListBoxNezhka.ItemsSource);
        }
        private bool IsPairForCurrentWeek(string pairKey, string selectedWeekType)
        {
            string upper = pairKey.ToUpper();
            if (upper.Contains("ЧИСЛИТЕЛЬ")) return selectedWeekType == "ЧИСЛИТЕЛЬ";
            if (upper.Contains("ЗНАМЕНАТЕЛЬ")) return selectedWeekType == "ЗНАМЕНАТЕЛЬ";
            return true;
        }
        private void ApplyTeacherFilterByFloor()
        {
            if (_teachersView == null) return;
            var selectedComboItem = FloorPreferenceComboBoxNezhka.SelectedItem as ComboBoxItem;
            if (selectedComboItem?.Tag == null) return;
            if (!int.TryParse(selectedComboItem.Tag.ToString(), out int selectedFloor)) return;
            _teachersView.Filter = obj =>
            {
                if (obj is SelectableTeacher teacher)
                    return teacher.PreferredFloor == selectedFloor;
                return false;
            };
            _teachersView.Refresh();
        }

        private List<SelectableTeacher> LoadTeacherPreferencesFromFile()
        {
            string filePath = GetTeacherPreferencesPath();
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<SelectableTeacher>>(json);
            }
            return new List<SelectableTeacher>();
        }

        private void WeekTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTodayTeachersFromJsonNezhka();
        }

        private void ApplyFloorPreference_Click(object sender, RoutedEventArgs e)
        {
            var preferences = TeachersListBoxNezhka.ItemsSource as List<SelectableTeacher>;
            if (preferences == null) return;
            var selectedTeachers = preferences.Where(t => t.IsSelected).ToList();
            if (!selectedTeachers.Any()) return;
            var selectedComboItem = FloorPreferenceComboBoxNezhka.SelectedItem as ComboBoxItem;
            if (selectedComboItem?.Tag == null) return;
            if (!int.TryParse(selectedComboItem.Tag.ToString(), out int selectedFloor)) return;
            foreach (var teacher in preferences)
            {
                teacher.PreferredFloor = teacher.IsSelected ? selectedFloor : 0;
            }

            TeachersListBoxNezhka.Items.Refresh();
            string json = JsonConvert.SerializeObject(allTeacherPreferences, Formatting.Indented);
            string filePath = GetTeacherPreferencesPath();
            File.WriteAllText(filePath, json);


        }
        private void FloorPreferenceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var preferences = TeachersListBoxNezhka.ItemsSource as List<SelectableTeacher>;
            if (preferences == null) return;
            var selectedItem = FloorPreferenceComboBoxNezhka.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag == null || !int.TryParse(selectedItem.Tag.ToString(), out int selectedFloor)) return;
            foreach (var teacher in preferences)
            {
                teacher.IsSelected = teacher.PreferredFloor == selectedFloor;
            }
            TeachersListBoxNezhka.Items.Refresh();
            var view = CollectionViewSource.GetDefaultView(preferences);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(SelectableTeacher.Name), ListSortDirection.Ascending));
        }
        private async Task UploadFileToServerAsync(string filepath, int campusId)
        {
            using (var httpClient = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    var fileStream = File.OpenRead(filepath);
                    var streamContent = new StreamContent(fileStream);
                    form.Add(streamContent, "file", Path.GetFileName(filepath));
                    form.Add(new StringContent(campusId.ToString()), "campusId");
                    var response = await httpClient.PostAsync("http://auditoryhelperapi.somee.com/api/Files/upload/excel", form);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Файл успешно загружен на сервер!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка загрузки на сервер: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void SaveDistributedTeachersToJson(string campusName)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuditoryGenerator", "Saved");
            Directory.CreateDirectory(folderPath);
            string fileName = "distributed_teachers_today.json";
            string fullPath = Path.Combine(folderPath, fileName);
            List<DistributedTeacher> existingData = new List<DistributedTeacher>();
            if (File.Exists(fullPath))
            {
                try
                {
                    string json = File.ReadAllText(fullPath);
                    existingData = JsonConvert.DeserializeObject<List<DistributedTeacher>>(json) ?? new List<DistributedTeacher>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка чтения предыдущих данных: " + ex.Message);
                }
            }
            existingData.RemoveAll(d => d.campus.Equals(campusName, StringComparison.OrdinalIgnoreCase));
            var newData = displayRooms.Select(d => new DistributedTeacher { teacher = d.TeacherName, room = d.RoomNumber, campus = campusName });
            existingData.AddRange(newData);
            string updatedJson = JsonConvert.SerializeObject(existingData, Formatting.Indented);
            File.WriteAllText(fullPath, updatedJson, Encoding.UTF8);
            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var form = new MultipartFormDataContent())
                    {
                        using (var stream = File.OpenRead(fullPath))
                        {
                            var streamContent = new StreamContent(stream);
                            form.Add(streamContent, "file", fileName);

                            var response = httpClient.PostAsync("http://auditoryhelperapi.somee.com/api/Files/upload/distributed", form).Result;

                            if (!response.IsSuccessStatusCode)
                            {
                                MessageBox.Show($"Ошибка при загрузке JSON на сервер: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при отправке JSON: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadDistributedTeachersFromJson(string targetCampus)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuditoryGenerator", "Saved");
            Directory.CreateDirectory(folderPath);
            string fileName = "distributed_teachers_today.json";
            string fullPath = Path.Combine(folderPath, fileName);
            if (!File.Exists(fullPath))
            {
                MessageBox.Show("Файл с распределёнными преподавателями на сегодня не найден.");
                return;
            }
            try
            {
                string json = File.ReadAllText(fullPath);
                var rawList = JsonConvert.DeserializeObject<List<DistributedTeacher>>(json);
                if (rawList != null)
                {
                    var filtered = rawList.Where(item => item.campus.Equals(targetCampus, StringComparison.OrdinalIgnoreCase)).ToList();
                    displayRooms.Clear();
                    int id = 1;
                    foreach (var item in filtered)
                    {
                        displayRooms.Add(new TeacherRoomDisplay { Id = id++, TeacherName = item.teacher, RoomNumber = item.room });
                    }
                    DataGridAuditoryNezhka.ItemsSource = displayRooms;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла:\n{ex.Message}");
            }
        }
    }
}