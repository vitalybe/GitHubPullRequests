﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GitHubPullRequests
{
    class Program
    {
        static void Main(string[] args)
        {
            var argumentOptions = new ArgumentOptions();
            if (CommandLine.Parser.Default.ParseArguments(args, argumentOptions))
            {
                if (argumentOptions.ListPRs)
                {
                    ListPullRequests();
                }
                else if (argumentOptions.CreatePR && !string.IsNullOrWhiteSpace(argumentOptions.BranchName))
                {
                    CreatePullRequest(argumentOptions, AppSettings.Default.MasterBranch);
                }
                else if (argumentOptions.OpenWeb && argumentOptions.PullRequestId.HasValue)
                {
                    OpenPrWeb(argumentOptions.PullRequestId.Value);
                }
                else if (argumentOptions.Sha && argumentOptions.PullRequestId.HasValue)
                {
                    PrintPrSha(argumentOptions.PullRequestId.Value);
                }
                else
                {
                    Console.WriteLine(argumentOptions.GetUsage());
                }
            }
        }


        private static string RunGitCommand(string command)
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

        private static void PrintPrSha(int pullRequestId)
        {
            var pr = GitHub.GetPullRequestData(pullRequestId);
            Console.WriteLine(pr.head.sha);
        }


        private static void OpenPrWeb(int pullRequestId)
        {
            var pr = GitHub.GetPullRequestData(pullRequestId);
            var urlString = pr.html_url.Value;

            Uri url;
            Uri.TryCreate(urlString, UriKind.Absolute, out url);
            if (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)
            {
                Process.Start(urlString);
            }
            else
            {
                Console.WriteLine("Invalid PR url: " + urlString);
            }
        }

        private static void CreatePullRequest(ArgumentOptions argumentOptions, string mainBranch)
        {
            Console.WriteLine("Creating PR for branch {0} into {1}", argumentOptions.BranchName, mainBranch);

            var executablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            executablePath = new Uri(executablePath).LocalPath;
            var notepadPath = Path.Combine(executablePath, "notepad2.exe");
            var prMessageFilePath = Path.Combine(executablePath, "pull-request-text.txt");

            string prMessageCreatorTemplate =
                string.Format(
                    "{0}" +
                    "# The first line is the title of the PR, all other lines are the body{0}" +
                    "# Lines starting with # and blank lines are ignored.{0}" +
                    "# PR branch: {1}.\n" +
                    "# Main branch: {2}",
                    Environment.NewLine, argumentOptions.BranchName, mainBranch);

            File.WriteAllText(prMessageFilePath, prMessageCreatorTemplate);
            Process.Start(notepadPath, prMessageFilePath).WaitForExit();
            var prMessageCreatorLines = File.ReadAllLines(prMessageFilePath);
            var validLines = prMessageCreatorLines.Where(line => !line.StartsWith("#") && !string.IsNullOrWhiteSpace(line));

            if (validLines.Count() >= 2)
            {
                string title = validLines.ElementAt(0);
                string body = string.Join(Environment.NewLine, validLines.Skip(1));

                var response = GitHub.CreatePullRequest(title, argumentOptions.BranchName, mainBranch, body);
                if (response.StatusCode != HttpStatusCode.Created)
                {
                    Console.WriteLine("Failed to create PR: \n" +
                                      JsonConvert.SerializeObject(JsonConvert.DeserializeObject(response.RawText),
                                                                  Formatting.Indented));
                }
                else
                {
                    dynamic pr = JsonConvert.DeserializeObject(response.RawText);
                    Console.WriteLine("Created PR: " + pr.html_url);
                }
            }
            else
            {
                Console.WriteLine("PR cancelled - At least 2 lines are expected from the user - Title and body");
            }

            //GitHub.CreatePullRequest("From API!", argumentOptions.BranchName, MainBranch, "This is some body\nHi!");
        }

        private static void ListPullRequests()
        {
            var prs = GitHub.OpenPullRequests();

            var columnsWidth = new Dictionary<string, int>();
            columnsWidth["ID"] = 4;
            columnsWidth["Last SHA"] = 10;
            columnsWidth["Owner"] = 15;
            columnsWidth["Branch"] = 30;
            columnsWidth["Title"] = 40;

            var rowStructure = BuildRowStructure(columnsWidth);

            var prListTableBuilder = new StringBuilder();
            prListTableBuilder.AppendLine(string.Format(rowStructure, columnsWidth.Keys.ToArray()));
            prListTableBuilder.AppendLine(string.Format(rowStructure, columnsWidth.Values.Select(columnWidth => new String('-', columnWidth)).ToArray()));

            foreach (var pr in prs)
            {
                var sha = pr.head.sha;
                FetchIfNotInRepo(sha);

                var shortSha = RunGitCommand("rev-parse --short " + sha).Trim();
                prListTableBuilder.AppendLine(string.Format(rowStructure, pr.number, shortSha, pr.user.login, pr.head["ref"], pr.title));
            }

            Console.WriteLine(prListTableBuilder.ToString());
        }

        private static void FetchIfNotInRepo(dynamic sha)
        {
            try
            {
                RunGitCommand("cat-file -t " + sha);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to find commit in repositry, running a fetch...");
                RunGitCommand("fetch");
            }
        }

        private static string BuildRowStructure(Dictionary<string, int> columnsWidth)
        {
            var rowStructureBuilder = new StringBuilder();
            var index = 0;
            foreach (var columnWidth in columnsWidth)
            {
                rowStructureBuilder.Append(string.Format("{{{0}, -{1}}} | ", index, columnWidth.Value));
                index++;
            }
            string buildRowStructure = rowStructureBuilder.ToString();

            return buildRowStructure.Trim(' ', '|');
        }
    }
}
