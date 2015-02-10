using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;

namespace GitHubPullRequests
{
    class ArgumentOptions
    {
        [Option('l', "list", HelpText = "List open PRs")]
        public bool ListPRs { get; set; }

        [Option('c', "create", HelpText = "Create a new PR")]
        public bool CreatePR { get; set; }

        [Option('b', "branch", HelpText = "The name of the branch")]
        public string BranchName { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("Command line pull requests", "0.1"),
                Copyright = new CopyrightInfo("Vitaly Belman", 2014),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("");
            help.AddPreOptionsLine("List PRs: pr -l");
            help.AddPreOptionsLine("Create PR: pr -c -b feature/my-new-feature");
            help.AddOptions(this);
            return help;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            const string MainBranch = "develop";

            var argumentOptions = new ArgumentOptions();
            if (CommandLine.Parser.Default.ParseArguments(args, argumentOptions))
            {
                if (argumentOptions.ListPRs)
                {
                    ListPullRequests();
                }
                else if(argumentOptions.CreatePR && !string.IsNullOrWhiteSpace(argumentOptions.BranchName))
                {
                    Console.WriteLine("Creating PR for branch {0} into {1}", argumentOptions.BranchName, MainBranch);

                    var executablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                    executablePath = new Uri(executablePath).LocalPath;
                    var notepadPath = Path.Combine(executablePath, "notepad2.exe");
                    var prMessageFilePath = Path.Combine(executablePath, "pull-request-text.txt");

                    string prMessageCreatorTemplate =
                        string.Format(
                        "# The first line is the title of the PR, all other lines are the body{0}" + 
                        "# Lines starting with # and blank lines are ignored.{0}" + 
                        "# PR branch: {1}.\n" +
                        "# Main branch: {2}",
                        Environment.NewLine, argumentOptions.BranchName, MainBranch);

                    File.WriteAllText(prMessageFilePath, prMessageCreatorTemplate);
                    Process.Start(notepadPath, prMessageFilePath).WaitForExit();
                    var prMessageCreatorLines = File.ReadAllLines(prMessageFilePath);
                    var validLines = prMessageCreatorLines.Where(line => !line.StartsWith("#") && !string.IsNullOrWhiteSpace(line));

                    if (validLines.Count() >= 2)
                    {
                        string title = validLines.ElementAt(0);
                        string body = string.Join(Environment.NewLine, validLines.Skip(1));

                        var response = GitHub.CreatePullRequest(title, argumentOptions.BranchName, MainBranch, body);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            Console.WriteLine("Failed to create PR: \n" + JsonConvert.SerializeObject(JsonConvert.DeserializeObject(response.RawText), Formatting.Indented));
                        }
                    }
                    else
                    {
                        Console.WriteLine("PR cancelled - At least 2 lines are expected from the user - Title and body");
                    }

                    //GitHub.CreatePullRequest("From API!", argumentOptions.BranchName, MainBranch, "This is some body\nHi!");
                }
                else
                {
                    Console.WriteLine(argumentOptions.GetUsage());
                }
            }

            //ListPullRequests();

            // GitHub.CreatePullRequest("From API!", "feature/branch1", "dev", "This is some body\nHi!");
        }

        private static void ListPullRequests()
        {
            var prs = GitHub.OpenPullRequests();

            var columnsWidth = new Dictionary<string, int>();
            columnsWidth["ID"] = 4;
            columnsWidth["Last commit"] = 40;
            columnsWidth["Owner"] = 15;
            columnsWidth["Title"] = 40;

            var rowStructure = BuildRowStructure(columnsWidth);

            Console.WriteLine(rowStructure, columnsWidth.Keys.ToArray());
            Console.WriteLine(rowStructure, columnsWidth.Values.Select(columnWidth => new String('-', columnWidth)).ToArray());

            foreach (var pr in prs)
            {
                Console.WriteLine(string.Format(rowStructure, pr.number, pr.head.sha, pr.user.login, pr.title));
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
