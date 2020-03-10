using System.Collections.Generic;

namespace Models
{
    public class MiniBook
    {
        private IList<string> _subjects;
        private string _thumbnailUrl;

        public MiniBook(IList<string> subjects, string thumbnailUrl)
        {
            this.Subjects = subjects;
            this.ThumbnailUrl = thumbnailUrl;
        }

        public IList<string> Subjects { get => _subjects; set => _subjects = value; }
        public string ThumbnailUrl { get => _thumbnailUrl; set => _thumbnailUrl = value; }
    }
}