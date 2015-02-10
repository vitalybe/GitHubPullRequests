using CommandLine;
using CommandLine.Text;

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
}