namespace Tsubasa.Helper.YoutubeSearch
{
    public class Video
    {
        public readonly string ThumbnailUrl;
        public readonly string Title;
        public readonly string Url;

        public Video(string url, string title, string thumbnailUrl)
        {
            Url = url;
            Title = title;
            ThumbnailUrl = thumbnailUrl;
        }
    }
}