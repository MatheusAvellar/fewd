using System;
using System.Diagnostics;
using System.Text;

namespace fewd {
  class FEWDConverter {

    private static StringBuilder output = null;

    public static int ConvertImage(string path, string to) {
      output = new StringBuilder();

      // [Ref] stackoverflow.com/a/12436300/4824627
      // [Ref] docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.beginerrorreadline
      Process proc = new Process();
      ProcessStartInfo psi = new ProcessStartInfo {
        UseShellExecute = false,
        // Using convert.exe from ImageMagick: see imagemagick.org/script/license.php
        FileName = @"Resources\Binary\convert.exe",
        Arguments = $"{path} {to}",
        RedirectStandardOutput = true,
        // [Ref] stackoverflow.com/a/6857053/4824627
        WindowStyle = ProcessWindowStyle.Hidden,
        // [Ref] stackoverflow.com/a/13269085/4824627
        CreateNoWindow = true
      };
      proc.StartInfo = psi;
      proc.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);

      Console.WriteLine($"Executing [{proc.StartInfo.FileName} {proc.StartInfo.Arguments}]");
      proc.Start();

      proc.BeginOutputReadLine();
      proc.WaitForExit();

      if(output.Length > 0) {
        // If the process wrote more than just white space, write the output to the console.
        Console.WriteLine(output);
      }
      int exit_code = proc.ExitCode;
      proc.Close();
      proc.Dispose();
      return exit_code;
    }

    private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
      // Collect the net view command output.
      if(!string.IsNullOrEmpty(outLine.Data)) {
        // Add the text to the collected output.
        output.Append($"\n  {outLine.Data}");
      }
    }
  }
}
