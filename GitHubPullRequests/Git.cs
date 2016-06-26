using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GitHubPullRequests
{
    class Git
    {
        public static string AppConfig(string key)
        {
            return RunGitCommand("config GitHubPullRequests." + key).Trim();
        }

        public static string RunGitCommand(string command)
        {
            ProcessStartInfo gitInfo = new ProcessStartInfo();
            gitInfo.CreateNoWindow = true;
            gitInfo.RedirectStandardError = true;
            gitInfo.RedirectStandardOutput = true;
            gitInfo.FileName = "git.exe";
            gitInfo.UseShellExecute = false;

            Process gitProcess = new Process();
            gitInfo.Arguments = command; // such as "fetch orign"

            gitProcess.StartInfo = gitInfo;
            gitProcess.Start();

            string stderr = gitProcess.StandardError.ReadToEnd();  // pick up STDERR
            string stdout = gitProcess.StandardOutput.ReadToEnd(); // pick up STDOUT

            gitProcess.WaitForExit();
            int exitCode = gitProcess.ExitCode;
            gitProcess.Close();

            if (exitCode != 0)
            {
                throw new ExternalException(string.Format("Git process returned failure. Exit code: {0}. Error stream: {1}", exitCode, stderr));
            }

            return stdout;
        }
    }
}