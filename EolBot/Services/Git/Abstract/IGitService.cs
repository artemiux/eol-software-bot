namespace EolBot.Services.Git.Abstract
{
    public interface IGitService
    {
        bool EnsureCloned(string url, string path);

        void Pull(string path);
    }
}
