using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ClosedXML.Excel;
using Newtonsoft.Json;

namespace AuditoryGenerator
{
    public partial class MainWindow : Window
    {
        private readonly ApiService apiService = new ApiService();
        private int attemptCount = 0;
        private const int MaxAttempts = 3;
        private readonly string blockFilePath = "login_block.txt";

        public MainWindow()
        {
            InitializeComponent();
            
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(blockFilePath))
            {
                var blockTime = File.GetLastWriteTime(blockFilePath);
                if (DateTime.Now < blockTime.AddMinutes(5))
                {
                    var minutesLeft = Math.Ceiling((blockTime.AddMinutes(5) - DateTime.Now).TotalMinutes);
                    MessageBox.Show($"Слишком много попыток. Попробуй снова через {minutesLeft} мин.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    File.Delete(blockFilePath);
                    attemptCount = 0;
                }
            }

            string enteredPassword = PasswordBox.Password;
            string AdminPassword = Encoding.UTF8.GetString(Convert.FromBase64String("NTY0NTM0MjNBZG1pbjEy"));

            if (enteredPassword == AdminPassword)
            {
                WindowAdmin windowAdmin = new WindowAdmin();
                windowAdmin.Show();
                Close();
            }
            else
            {
                attemptCount++;
                if (attemptCount >= MaxAttempts)
                {
                    File.WriteAllText(blockFilePath, "blocked");
                    MessageBox.Show("Вы ввели неправильный пароль 3 раза. Попробуй через 5 минут.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Неверный пароль! Осталось попыток: {MaxAttempts - attemptCount}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


    }
}