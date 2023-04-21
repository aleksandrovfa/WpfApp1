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
        private UrlInfo isSelected;

        public MainViewModel()
        {
            urls = new ObservableCollection<UrlInfo>();
            progress = 0;
            maxTagUrl = "";
            status = "Ready";
            string[] fileLines = File.ReadAllLines("urls.txt");
            foreach (var item in fileLines)
            {
                urls.Add(new UrlInfo(item));
            }
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

                // Создаем список задач для подсчета тегов
                var tasks = new List<Task<UrlInfo>>();
                foreach (var url in urls)
                {
                    var task = Task.Run(() =>
                    {
                        // Загружаем html страницу и считаем теги <a>
                        url.Count = LoadAndCountTag(url.Url, cts.Token);
                        Progress = progress + 100/urls.Count;
                        return url;
                    });
                    tasks.Add(task);
                }

                // Ожидаем выполнения всех задач
                var results = await Task.WhenAll(tasks);
                Status = "Completed";
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
                // Обновляем список Url и выделяем наиболее часто встречающийся Url
                IsProcessing = false;
                int maxTagCount = 0;
                foreach (var result in urls)
                {
                    if (result.Count > maxTagCount)
                    {
                        maxTagCount = result.Count;
                        maxTagUrl = result.Url;
                        IsSelected = result;
                    }
                }
                NotifyPropertyChanged(nameof(Urls));
                NotifyPropertyChanged(nameof(MaxTagUrl));
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

        public UrlInfo IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; NotifyPropertyChanged(nameof(IsSelected)); }
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
                        throw new Exception($"Url {url} не найден.");
                    }
                }
                throw new Exception($"Ошибка при загрузке html {url}.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при подсчете тегов {url}.", ex);
            }
            finally
            {
                // В случае отмены операции выбрасываем исключение OperationCanceledException
                ct.ThrowIfCancellationRequested();
            }
        }

      
    }
}