using System;
using System.IO;
using System.Windows;
using System.Globalization;
using System.Windows.Input;
using System.Threading;

namespace fewd {
  public partial class MainWindow : Window {

    FileSystemWatcher watcher = null;

    public MainWindow() {
      // TODO:
      // - Fix hacked "Open folder" UI
      // - Check extension of original file; e.g. .mp4 to .jpg would throw
      // | - Having a (to, from) dict of possible file conversions would be nice
      // - Add ffmpeg conversions (i.e. video, gifs?)
      // - Gifski maybe?
      InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      // TODO: Maybe this isn't the best idea
      if(watcher != null)
        Dispose();

      // Create a "Save As" dialog for selecting a directory (HACK)
      // [Ref] stackoverflow.com/a/50261897/4824627
      // [Ref] github.com/dotnet/wpf/issues/438
      var dialog = new Microsoft.Win32.SaveFileDialog {
        Title = "Select a Directory", // instead of default "Save As"
        Filter = "Directory|*.this.directory", // Prevents displaying files
        FileName = "select" // Filename will then be "select.this.directory"
      };
      if(dialog.ShowDialog() == true) {
        string path = dialog.FileName;
        // Remove fake filename from resulting path
        path = path.Replace("\\select.this.directory", "");
        path = path.Replace(".this.directory", "");
        // If user has changed the filename, create the new directory
        if(!Directory.Exists(path)) {
          return;
        }
        // Our final value is in path
        textBox.Text = path;

        // Create watcher on selected folder, check for file and directory names
        watcher = new FileSystemWatcher {
          Path = path + "\\",
          NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
          Filter = "*.*",
          EnableRaisingEvents = true
        };
        // Hook on `Renamed` event
        watcher.Renamed += new RenamedEventHandler(OnChanged);
      }
    }

    private void OnChanged(object source, RenamedEventArgs e) {
      Console.WriteLine($"[{e.OldName}] to [{e.Name}]");

      // Prevent self-triggering event
      if(e.Name.EndsWith(".tmp", true, CultureInfo.CurrentUICulture)
      || e.Name.Contains("File Extension Watchdog")
      || e.OldName.Contains("File Extension Watchdog")) {
        return;
      }

      // Get file path, replacing filename with temp name
      // TODO: Might be a good idea to add a random number at the end
      string tmp_file_path = e.FullPath.Replace(e.Name, "File Extension Watchdog");

      // Get filename extension (e.g. "png")
      string from_ext = GetFileExtension(e.OldName);
      string to_ext = GetFileExtension(e.Name);

      switch(to_ext) {
        case "jpg":
        case "jpeg":
        case "png":
        case "bmp":
          Console.WriteLine($"Converting file to {to_ext.ToUpper()}");

          string imagemagick_ready_tmp_path = $"\"{tmp_file_path}.{to_ext}\"";
          FEWDConverter.ConvertImage(e.FullPath, imagemagick_ready_tmp_path);
          // Replace original file with new, converted file
          Console.WriteLine($"Attempting to move '{tmp_file_path}.{to_ext}' to '{e.FullPath}'");
          // Check for if we fail to replace file
          bool moved = false;
          int tries = 0;
          while(!moved) {
            // Attempt to replace file
            try {
              File.Replace($"{tmp_file_path}.{to_ext}", e.FullPath, null);
              moved = true;
            } catch(IOException) {
              tries++;
              Console.WriteLine($"Failed to move ({tries}) times");
              // If we fail, then try again 1 second later
              Thread.Sleep(1000);
            }
          }
          break;
        default:
          Console.WriteLine($"Extension '{to_ext}' not supported");
          return;
      }
      Console.WriteLine("Successfully converted file!");
    }

    private string GetFileExtension(string str) {
      string[] arr = str.Split('.');
      if(arr.Length > 1) {
        return arr[arr.Length - 1];
      }
      Console.WriteLine($"[!] File without extension ({str})");
      return "";
    }

    // [Ref] stackoverflow.com/q/15017506/4824627
    public void Dispose() {
      watcher.Renamed -= OnChanged;
      watcher.Dispose();
    }

    private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
      if(e.ChangedButton == MouseButton.Left)
        this.DragMove();
    }
  }
}
