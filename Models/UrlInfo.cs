using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTagCounting
{
    public class UrlInfo : INotifyPropertyChanged
    {
        private string url;

        private int count;

        public string Url
        {
            get { return url; }
            set { url = value; NotifyPropertyChanged(nameof(Url)); }
        }

        public int Count
        {
            get { return count; }
            set { count = value; NotifyPropertyChanged(nameof(Count)); }
        }


        public UrlInfo(string url, int count)
        {
            Url = url;
            Count = count;

        }
        public UrlInfo(string url)
        {
            Url = url;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
