using System;

namespace GitHubPullRequests
{
    static class Log
    {
        public static void Write(string text)
        {
            Console.WriteLine("[{0}] {1}", DateTime.Now.ToShortTimeString(), text);
        }
    }
}