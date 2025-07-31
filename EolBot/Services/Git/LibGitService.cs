using EolBot.Helpers;
using EolBot.Services.Git.Abstract;
using LibGit2Sharp;

namespace EolBot.Services.Git
{
    public class LibGitService(Signature signature) : IGitService
    {
        private readonly PullOptions _defaultPullOptions = new();

        public bool EnsureCloned(string url, string path)
        {
            var cloned = false;
            if (!Repository.IsValid(path))
            {
                if (Directory.Exists(path))
                {
                    DirectoryHelper.DeleteDirectory(path);
                }
                _ = Repository.Clone(url, path);
                cloned = true;
            }
            return cloned;
        }

        public void Pull(string path)
        {
            using Repository repo = new(path);
            Commands.Pull(repo, signature, _defaultPullOptions);
        }
    }
}
