using ClosedXML.Excel;
using DocumentFormat.OpenXml.ExtendedProperties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http.Formatting;
using System.Text;


namespace AuditoryGenerator
{
    public class ApiService
    {
        private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "http://auditoryhelperapi.somee.com/api";
        private List<PatternQualificationMap> patternQualificationList;
        private List<GroupAssignedCell> assignedCellsList = new List<GroupAssignedCell>();

        public async Task<List<Group>> GetGroupsAsync()
        {
            var response = await client.GetStringAsync($"{BaseUrl}/Groups");
            return JsonConvert.DeserializeObject<List<Group>>(response);
        }
        public async Task<List<Qualification>> GetQualificationsAsync()
        {
            var response = await client.GetStringAsync($"{BaseUrl}/Qualifications");
            return JsonConvert.DeserializeObject<List<Qualification>>(response);
        }
        public async Task<Dictionary<string, List<(string groupName, int year)>>> GetSpecialtiesAsync()
        {
            var groups = await GetGroupsAsync();
            var qualifications = await GetQualificationsAsync();
            patternQualificationList = groups.Select(group =>{var qualification = qualifications.FirstOrDefault(q => q.Id == group.QualificationId);if (qualification == null) return null;var pattern = ExtractPattern(group.Abbreviation);
                return new PatternQualificationMap{Pattern = pattern,QualificationCode = qualification.Code};}).Where(x => x != null).GroupBy(x => x.Pattern).Select(g => g.First()).ToList();
            var specialtyDict = new Dictionary<string, List<(string, int)>>();
            foreach (var group in groups)
            {
                var qualification = qualifications.Find(q => q.Id == group.QualificationId);
                if (qualification == null)
                {
                    MessageBox.Show($"Ошибка: Не найдена квалификация для группы {group.Name}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
                string pattern = ExtractPattern(group.Abbreviation);
                if (!specialtyDict.ContainsKey(pattern)) {specialtyDict[pattern] = new List<(string, int)>();}
                specialtyDict[pattern].Add((group.Abbreviation, group.YearOfAdmission));
            }
            return specialtyDict;
        }
        private string ExtractPattern(string groupName)
        {
            string pattern = Regex.Replace(groupName, @"[\d]", "").Trim();
            if (pattern.Contains("-")) pattern = pattern.Split('-')[0];
            return pattern;
        }
        public async Task GenerateAuditoryFileAsync(string filePath)
        {
            var specialtiesDict = await GetSpecialtiesAsync();
            var groupedByQualification = specialtiesDict.Select(kvp =>{var qualificationCode = patternQualificationList.FirstOrDefault(p => p.Pattern == kvp.Key)?.QualificationCode ?? kvp.Key;
                return new{Pattern = kvp.Key,QualificationCode = qualificationCode,Groups = kvp.Value};}).GroupBy(x => x.QualificationCode).ToList();
            List<CombinedSpecialty> combinedSpecialties = new List<CombinedSpecialty>();
            foreach (var group in groupedByQualification)
            {
                var smallPatterns = group.Where(x => x.Groups.Count <= 2).SelectMany(x => x.Groups).OrderByDescending(g => g.Item2).ToList();
                var largePatterns = group.Where(x => x.Groups.Count > 2).Select(x => new CombinedSpecialty{Header = x.QualificationCode,Groups = x.Groups.OrderByDescending(g => g.Item2).ToList()});
                if (smallPatterns.Any())
                {
                    combinedSpecialties.Add(new CombinedSpecialty{Header = group.Key,Groups = smallPatterns});
                }
                combinedSpecialties.AddRange(largePatterns);
            }
            var specialties = specialtiesDict.Select(entry => new{Specialty = entry.Key,Groups = entry.Value.Select(g => (groupName: g.Item1, year: g.Item2)).OrderByDescending(g => g.year).ToList()}).ToList();
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Аудиторник");
            worksheet.Columns().Width = 15;worksheet.Rows().Height = 68.8;
            worksheet.Row(1).Height = 46.8;worksheet.Row(2).Height = 46.8;
            worksheet.Row(3).Height = 30;worksheet.Row(4).Height = 42;
            worksheet.Row(5).Height = 45;worksheet.Row(6).Height = 45;
            for (int i = 7; i <= 14; i++) worksheet.Row(i).Height = 70;
            worksheet.Row(15).Height = 37;worksheet.Row(16).Height = 37;
            worksheet.Row(17).Height = 45;worksheet.Row(18).Height = 45;
            for (int i = 19; i <= 26; i++) worksheet.Row(i).Height = 70;
            worksheet.Row(27).Height = 37;worksheet.Row(28).Height = 37;
            worksheet.Row(29).Height = 45;worksheet.Row(30).Height = 45;
            for (int i = 31; i <= 38; i++) worksheet.Row(i).Height = 70;
            worksheet.Range("A1:C1").Merge().Value = "ДЕНЬ:";
            worksheet.Range("A1:C1").Style.Font.SetBold().Font.SetFontSize(36).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
            worksheet.Range("D1:N1").Merge().Value = "СУББОТА";
            worksheet.Range("D1:N1").Style.Font.SetBold().Font.SetFontSize(36).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
            worksheet.Range("Q1:T1").Merge().Value = "НЕДЕЛЯ:";worksheet.Range("Q1:T1").Style.Font.SetBold().Font.SetFontSize(36).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
            worksheet.Range("U1:Y1").Merge().Value = "ЧИСЛИТЕЛЬ";
            worksheet.Range("U1:Y1").Style.Font.SetBold().Font.SetFontSize(36).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
            worksheet.Cell("A2").Value = "Аудиторный фонд на";
            worksheet.Cell("A2").Style.Font.SetBold().Font.SetFontSize(36).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
            string currentDateText = DateTime.Now.ToString("d MMMM yyyy г.");
            var dateCell = worksheet.Range("H2:R2").Merge().FirstCell();
            dateCell.Value = currentDateText;
            dateCell.Style.Font.SetBold().Font.SetFontSize(36).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
            worksheet.Range("S2:W2").Merge().Value = "2024/2025 учебный год";
            worksheet.Range("S2:W2").Style.Font.SetBold().Font.SetFontSize(34).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left).Alignment.SetVertical(XLAlignmentVerticalValues.Bottom);
            int maxColumns = 42;
            int startCol = 2;
            int startRow = 4;
            int tableSpacing = 12;
            AddPairsHeader(worksheet, startRow);
            while (combinedSpecialties.Count > 0)
            {
                bool drewSomething = false;
                for (int i = 0; i < combinedSpecialties.Count;)
                {
                    var specialty = combinedSpecialties[i];
                    int groupCount = specialty.Groups.Count;
                    int availableCols = maxColumns - startCol;
                    if (groupCount <= availableCols)
                    {
                        AddSpecialtyTable(worksheet, startCol, startRow, specialty.Header, specialty.Groups, assignedCellsList);
                        startCol += groupCount;combinedSpecialties.RemoveAt(i);drewSomething = true;
                    }
                    else{i++;}
                }
                if (!drewSomething){startRow += tableSpacing;startCol = 2;AddPairsHeader(worksheet, startRow);}
            }
            workbook.SaveAs(filePath);
            var allGroups = await GetGroupsAsync();
            foreach (var cell in assignedCellsList){var group = allGroups.FirstOrDefault(g => $"{g.Abbreviation}-{g.YearOfAdmission}" == cell.GroupName);}
        }
        private void AddPairsHeader(IXLWorksheet worksheet, int rowStart)
        {
            var lightBlue = XLColor.FromHtml("#DDEBF7");
            worksheet.Range(rowStart + 1, 1, rowStart + 2, 1).Merge().Value = "Пара";
            worksheet.Range(rowStart + 1, 1, rowStart + 2, 1).Style.Font.SetBold().Font.SetFontSize(22).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center).Fill.SetBackgroundColor(lightBlue).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            for (int i = 0; i <= 7; i++)
            {
                worksheet.Cell(rowStart + 3 + i, 1).Value = i;
                worksheet.Cell(rowStart + 3 + i, 1).Style.Font.SetBold().Font.SetFontSize(22).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center).Fill.SetBackgroundColor(lightBlue).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
        }
        private void AddSpecialtyTable(IXLWorksheet worksheet, int startCol, int startRow, string specialty, List<(string groupName, int year)> groups, List<GroupAssignedCell> groupCellMap)
        {
            var lightBlue = XLColor.FromHtml("#DDEBF7");
            var lightYellow = XLColor.FromHtml("#FFF2CC");
            worksheet.Range(startRow, startCol, startRow, startCol + groups.Count - 1).Merge().Value = specialty;
            worksheet.Range(startRow, startCol, startRow, startCol + groups.Count - 1).Style.Font.SetBold().Font.SetFontSize(22).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            for (int i = 0; i < groups.Count; i++)
            {
                int col = startCol + i;
                worksheet.Cell(startRow + 1, col).Value = groups[i].groupName;
                worksheet.Cell(startRow + 1, col).Style.Font.SetBold().Font.SetFontSize(19).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center).Fill.SetBackgroundColor(lightBlue).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                worksheet.Cell(startRow + 2, col).Value = groups[i].year;
                worksheet.Cell(startRow + 2, col).Style.Font.SetBold().Font.SetFontSize(22).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center).Fill.SetBackgroundColor(lightBlue).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                string columnLetter = worksheet.Column(col).ColumnLetter();
                string range = $"{columnLetter}{startRow + 3}:{columnLetter}{startRow + 10}";
                groupCellMap.Add(new GroupAssignedCell
                {
                    GroupName = $"{groups[i].groupName}-{groups[i].year}",
                    AssignedCells = range
                });
            }
            for (int row = 0; row < 8; row++)
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    int col = startCol + i;
                    worksheet.Cell(startRow + 3 + row, col).Value = " ";
                    worksheet.Cell(startRow + 3 + row, col).Style.Font.SetBold().Font.SetFontSize(22).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center).Fill.SetBackgroundColor(lightYellow).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }
            }
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < groups.Count; col++)
                {
                    worksheet.Cell(row + startRow + 3, startCol + col).Value = " ";
                    worksheet.Cell(row + startRow + 3, startCol + col).Style.Font.SetBold().Font.SetFontSize(22).Font.SetFontName("Arial Cyr").Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center).Fill.SetBackgroundColor(lightYellow).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }
            }
        }
    }
}
