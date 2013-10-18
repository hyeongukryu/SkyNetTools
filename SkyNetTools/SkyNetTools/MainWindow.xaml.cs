using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

namespace SkyNetTools
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Makefile|MakeFile"
                };
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    Process(dialog.FileName);
                }
                MessageBox.Show("작업 성공 최고!", "이거 최고예요!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("오류가 있었습니다.", "뀨우...", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Process(string makefilePath)
        {
            var directory = Path.GetDirectoryName(makefilePath);
            var outputDirecetory = Path.Combine(directory, "packages");
            Directory.CreateDirectory(outputDirecetory);

            var lines = (from line in File.ReadLines(makefilePath)
                         where line.StartsWith("#") == false
                         select line).ToArray();

            Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var tokens = line.Split(new[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);

                if (1 <= tokens.Length == false)
                {
                    continue;
                }

                var first = tokens.First();

                if (first.EndsWith(":"))
                {
                    var name = first.Substring(0, first.Length - 1);
                    Entry entry = new Entry
                    {
                        Name = name,
                        Files = tokens.Skip(1).ToArray(),
                        Command = lines[i + 1].Trim()
                    };
                    entries.Add(name, entry);
                }
            }

            var all = entries["all"];
            foreach (var projectName in all.Files)
            {
                var project = entries[projectName];
                var projectFiles = project.Files;
                var projectCommand = project.Command;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# Makefile");
                sb.AppendLine("");
                sb.AppendLine(string.Format("a.out: {0}", string.Join(" ", projectFiles)));

                var tokens = projectCommand.Split(new[] { " " }, StringSplitOptions.None);
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (tokens[i] == "-o")
                    {
                        tokens[i + 1] = "a.out";
                        break;
                    }
                }

                string command = string.Join(" ", tokens);
                sb.AppendLine(string.Format("\t{0}", command));

                Dictionary<string, byte[]> zipEntries = new Dictionary<string, byte[]>();
                foreach (var path in projectFiles)
                {
                    var fullPath = Path.Combine(directory, path);
                    zipEntries.Add(path, File.ReadAllBytes(fullPath));
                }
                zipEntries.Add("Makefile", Encoding.UTF8.GetBytes(sb.ToString()));

                var zipPath = Path.Combine(outputDirecetory, projectName + ".zip");
                Save(zipPath, zipEntries);
            }
        }

        private void Save(string zipPath, Dictionary<string, byte[]> entries)
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            
            using (FileStream zipFile = new FileStream(zipPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Create))
                {
                    foreach (var entry in entries)
                    {
                        ZipArchiveEntry zipEntry = archive.CreateEntry(entry.Key);
                        using (var stream = zipEntry.Open())
                        {
                            stream.Write(entry.Value, 0, entry.Value.Length);
                        }
                    }
                }
            }
        }
    }
}
