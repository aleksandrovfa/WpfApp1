using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace HtmlTagCounting
{
    public class MainViewModel : INotifyPropertyChanged
    {
        //private ObservableCollection<string> urls;
        private ObservableCollection<UrlInfo> urls;
        private int progress;
        private string maxTagUrl;
        private string status;
        private CancellationTokenSource cts;

        public MainViewModel()
        {
            urls = new ObservableCollection<UrlInfo>();
            progress = 0;
            maxTagUrl = "";
            status = "Ready";
            //cts = new CancellationTokenSource();
        }

        public ICommand StartCommand => new RelayCommand(async () =>
        {
            try
            {
                cts = new CancellationTokenSource();
                IsProcessing = true;
                Progress = 0;
                MaxTagUrl = "";
                Status = "Processing...";

                // Считываем Url из файла
                string[] fileLines = File.ReadAllLines("urls.txt");

                // Создаем список задач для подсчета тегов
                //var tasks = new List<Task<Tuple<string, int>>>();
                var tasks = new List<Task<UrlInfo>>();
                foreach (var url in fileLines)
                {
                    var task = Task.Run(() =>
                    {
                        // Загружаем html страницу и считаем теги <a>
                        int tagCount = LoadAndCountTag(url, cts.Token);
                        Progress = progress + 100/fileLines.Length;
                        //return Tuple.Create(url, tagCount);
                        return new UrlInfo(url, tagCount);
                    });
                    tasks.Add(task);
                }

                // Ожидаем выполнения всех задач
                var results = await Task.WhenAll(tasks);

                // Обновляем список Url и выделяем наиболее часто встречающийся Url
                urls.Clear();
                int maxTagCount = 0;
                foreach (var result in results)
                {
                    urls.Add(result);
                    if (result.Count > maxTagCount)
                    {
                        maxTagCount = result.Count;
                        maxTagUrl = result.Url;
                    }
                }
                Status = "Completed";
                NotifyPropertyChanged(nameof(Urls));
                NotifyPropertyChanged(nameof(MaxTagUrl));
            }
            catch (OperationCanceledException)
            {
                Status = "Cancelled";
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
            finally
            {
                IsProcessing = false;
            }
        });

        public ICommand CancelCommand => new RelayCommand(() =>
        {
            // Отменяем операцию подсчета тегов
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        });

        public ObservableCollection<UrlInfo> Urls
        {
            get { return urls; }
            set { urls = value; NotifyPropertyChanged(nameof(Urls)); }
        }

        public int Progress
        {
            get { return progress; }
            set { progress = value; NotifyPropertyChanged(nameof(Progress)); }
        }

        public string MaxTagUrl
        {
            get { return maxTagUrl; }
            set { maxTagUrl = value; NotifyPropertyChanged(nameof(MaxTagUrl)); }
        }

        public string Status
        {
            get { return status; }
            set { status = value; NotifyPropertyChanged(nameof(Status)); }
        }

        private bool isProcessing;
        public bool IsProcessing
        {
            get { return isProcessing; }
            set
            {
                isProcessing = value;
                NotifyPropertyChanged(nameof(IsProcessing));
                NotifyPropertyChanged(nameof(IsNotProcessing));
            }
        }

        public bool IsNotProcessing => !IsProcessing;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int LoadAndCountTag(string url, CancellationToken ct)
        {
            // Загрузка html страницы и подсчет тегов <a>
            try
            {
                using (var client = new WebClient())
                {
                    string html = client.DownloadString(url);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    int tagCount = doc.DocumentNode.Descendants("a").Count();
                    //Progress++ Convert.ToInt16(100/url.Count());
                    return tagCount;
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new Exception($"Url {url} not found.");
                    }
                }
                throw new Exception($"Error while downloading html from {url}.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while counting tags on {url}.", ex);
            }
            finally
            {
                // В случае отмены операции выбрасываем исключение OperationCanceledException
                ct.ThrowIfCancellationRequested();
            }
        }

        public class UrlInfo
        {
            public string Url { get; set; }
            public int Count { get; set; }

            public UrlInfo(string url, int count)
            {
                Url = url;
                Count = count;
            }
        }
    }
}