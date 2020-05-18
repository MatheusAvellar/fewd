using System;
using System.IO;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace fewd {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {

    FileSystemWatcher watcher = null;

    public MainWindow() {
      // TODO:
      // - Fix hacked "Open folder" UI
      // - Check extension of original file; e.g. .mp4 to .jpg would throw
      // | - Having a (to, from) dict of possible file conversions would be nice
      // - Fix awful .Net JPEG encoder quality
      // - Replace Windows XP Rover with something that doesn't infringe copyright
      InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      // TODO: Maybe this isn't the best idea
      if(watcher != null)
        Dispose();

      // Create a "Save As" dialog for selecting a directory (HACK)
      // [Ref] https://stackoverflow.com/a/50261897/4824627
      // [Ref] https://github.com/dotnet/wpf/issues/438
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
      Console.WriteLine("[" + e.OldName + "] to [" + e.Name + "]");

      // Prevent self-triggering event
      if(e.Name.EndsWith(".tmp", true, CultureInfo.CurrentUICulture)
      || e.Name.Equals("File Extension Watchdog")
      || e.OldName.Equals("File Extension Watchdog")) {
        return;
      }

      // Get Image from file
      Image original = Image.FromFile(e.FullPath);
      // Get file path, replacing filename with temp name
      // TODO: Might be a good idea to add a random number at the end
      string tmp_file_path = e.FullPath.Replace(e.Name, "File Extension Watchdog");
      // Get filename extension (e.g. "png")
      string[] str = e.Name.Split('.');
      string ext = str[str.Length - 1];
      switch(ext) {
        case "jpg":
        case "jpeg":
          Console.WriteLine("Converting file to JPEG");
          // Save JPG with """100%""" quality
          object[] enc = GetJpegEncoderByQuality(100L);
          original.Save(tmp_file_path, (ImageCodecInfo)enc[0], (EncoderParameters)enc[1]);
          break;
        case "png":
          Console.WriteLine("Converting file to PNG");
          original.Save(tmp_file_path, ImageFormat.Png);
          break;
        default:
          Console.WriteLine("Extension '" + ext + "' not supported");
          original.Dispose();
          return;
      }
      Console.WriteLine("Converted file saved as `File Extension Watchdog`");

      original.Dispose();
      File.Replace(tmp_file_path, e.FullPath, null);
      Console.WriteLine("Replaced original renamed file! Done!");
    }

    private object[] GetJpegEncoderByQuality(long quality) {
      // [Ref] https://stackoverflow.com/a/29211993/4824627
      var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
      var encParams = new EncoderParameters() {
        Param = new[] { new EncoderParameter(Encoder.Quality, quality) }
      };
      return new object[] { encoder, encParams };
    }

    // [Ref] https://stackoverflow.com/q/15017506/4824627
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
