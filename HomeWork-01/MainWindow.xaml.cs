using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Shapes;
using System.Configuration;
using System.Data.SqlClient;

namespace HomeWork_01
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlConnection cn;
        Dictionary<string, int> adjectivesVacabulary;
        Dictionary<string, int> verbsVacabulary;

        public MainWindow()
        {
            InitializeComponent();

            cn = new SqlConnection();

            // Построитель строки соединения с сервером
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.InitialCatalog = "words";
            builder.DataSource = ".\\SQLEXPRESS";
            builder.ConnectTimeout = 30;
            builder.IntegratedSecurity = true;  // - заходить с правами текущего пользователя Windows

            cn.ConnectionString = builder.ConnectionString;

            // Создание таблиц
            // create table adjectives(id INT PRIMARY KEY IDENTITY, word NVARCHAR(30), quantity INT)
            // create table verbs(id INT PRIMARY KEY IDENTITY, word NVARCHAR(30), quantity INT)

            var result = MessageBox.Show("Создать новые таблицы в БД words?", "Создание таблиц", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    CreateTable("adjectives");
                    CreateTable("verbs");
                }
                catch (Exception)
                {

                    MessageBox.Show("По-видимому таблицы уже созданы (либо что-то пошло не так)!");
                }
            }
        }

        // Формирование частотного словаря
        Dictionary<string, int> CreateDictionary(string sourceDir, string[] endings)
        {
            // Все слова
            string[] words;

            // Слова с нужными окончаниями
            List<string> wordsWithEndings = new List<string>();

            // Частотный словарь
            Dictionary<string, int> vacabulary = new Dictionary<string, int>();

            DirectoryInfo dinfo = new DirectoryInfo(sourceDir);

            if (dinfo.Exists)
            {
                try
                {
                    string[] names = Directory.GetFiles(sourceDir);

                    // Поиск в txt-файлах непосредственно из папки
                    foreach (string temp in names)
                    {
                        if (System.IO.Path.GetExtension(temp) == ".txt")
                        {
                            words = File.ReadAllText(temp, Encoding.Default).Split(new Char[] { ' ', '.', ',', ':', '!', '-', '?', ';', '\t' });

                            for (int i = 0; i < words.Length; i++)
                            {
                                for (int j = 0; j < endings.Length; j++)
                                {
                                    if (words[i].EndsWith(endings[j])) wordsWithEndings.Add(words[i]);
                                }
                            }

                            foreach (var item in wordsWithEndings)
                            {
                                if (vacabulary.ContainsKey(item))
                                {
                                    vacabulary[item]++;
                                }
                                else
                                {
                                    vacabulary.Add(item, 1);
                                }
                            }
                        }
                    }

                    // Поиск в txt-файлах из подпапок
                    DirectoryInfo[] dirs = dinfo.GetDirectories();
                    foreach (DirectoryInfo current in dirs)
                    {
                        names = Directory.GetFiles(current.FullName);
                        foreach (string temp in names)
                        {
                            if (System.IO.Path.GetExtension(temp) == ".txt")
                            {
                                words = File.ReadAllText(temp, Encoding.Default).Split(new Char[] { ' ', '.', ',', ':', '!', '-', '?', ';', '\t' });

                                for (int i = 0; i < words.Length; i++)
                                {
                                    for (int j = 0; j < endings.Length; j++)
                                    {
                                        if (words[i].EndsWith(endings[j])) wordsWithEndings.Add(words[i]);
                                    }
                                }

                                foreach (var item in wordsWithEndings)
                                {
                                    if (vacabulary.ContainsKey(item))
                                    {
                                        vacabulary[item]++;
                                    }
                                    else
                                    {
                                        vacabulary.Add(item, 1);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Папка не найдена!");
            }

            return vacabulary;

        }

        // Создание таблиц
        void CreateTable(string tableName)
        {
            cn.Open();

            string command = $"create table {tableName} (id INT PRIMARY KEY IDENTITY, word NVARCHAR(30), quantity INT)";
            SqlCommand cmd = new SqlCommand(command, cn);
            cmd.ExecuteNonQuery();

            cn.Close();
        }

        // Сохранение в БД
        void SaveToDB(Dictionary<string, int> dict, string tableName)
        {
            cn.Open();
            string command;

            foreach (var item in dict)
            {
                command = $"insert into {tableName} (word, quantity) " +
                $"values ('{item.Key}', {item.Value})";
                SqlCommand cmd = new SqlCommand(command, cn);
                cmd.ExecuteNonQuery();
            }

            cn.Close();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (CommonFileDialog.IsPlatformSupported)
            {
                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                dialog.InitialDirectory = @"c:\";
                CommonFileDialogResult result = dialog.ShowDialog();

                if (result == CommonFileDialogResult.Ok)
                {
                    adjectivesVacabulary = CreateDictionary(dialog.FileName, new string[] { "ая", "яя", "ий", "ой", "ее", "ое", "ие", "ые" });
                    MessageBox.Show($"Частотный словарь прилагательных ({adjectivesVacabulary.Count} слов) сформирован!");

                    verbsVacabulary = CreateDictionary(dialog.FileName, new string[] { "ешь", "ет", "ем", "ете", "ут", "ют", "ишь", "ит", "им", "ите", "aт", "ят", "ить", "ять" });
                    MessageBox.Show($"Частотный словарь глаголов ({verbsVacabulary.Count} слов) сформирован!");
                }
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveToDB(adjectivesVacabulary, "adjectives");
                MessageBox.Show("Прилагательные выгружены в БД!");
            }
            catch (Exception)
            {
                MessageBox.Show("Что-то пошло не так!");
            } 
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveToDB(verbsVacabulary, "verbs");
                MessageBox.Show("Глаголы выгружены в БД!");
            }
            catch (Exception)
            {

                MessageBox.Show("Что-то пошло не так!");
            }
        }
    }
}
