using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RadioProgramApp.Models;

namespace RadioProgramApp.ViewModels
{
    public class StationViewModel : INotifyPropertyChanged
    {
        private readonly Station _station;
        private readonly ParseRadikoStation _apiService; // RadikoApiServiceのインスタンスを保持

        public string Id => _station.Id;
        public string Name => _station.Name;
        public string RegionName => _station.RegionName;

        private BitmapImage _bannerImageSource;
        public BitmapImage BannerImageSource
        {
            get => _bannerImageSource;
            private set => SetProperty(ref _bannerImageSource, value);
        }

        private string _currentProgramTitle = "読込中..."; // 初期値
        public string CurrentProgramTitle
        {
            get => _currentProgramTitle;
            private set => SetProperty(ref _currentProgramTitle, value);
        }
        private string _currentProgramPerformer;
        public string CurrentProgramPerformer
        {
            get => _currentProgramPerformer;
            private set => SetProperty(ref _currentProgramPerformer, value);
        }

        private string _currentProgramTimeRange;
        public string CurrentProgramTimeRange
        {
            get => _currentProgramTimeRange;
            private set => SetProperty(ref _currentProgramTimeRange, value);
        }

        private ImageSource _currentProgramImageSource; // 番組画像用
        public ImageSource CurrentProgramImageSource
        {
            get => _currentProgramImageSource;
            private set => SetProperty(ref _currentProgramImageSource, value);
        }
        private string _currentProgramInfoText;
        public string CurrentProgramInfoText
        {
            get => _currentProgramInfoText;
            private set => SetProperty(ref _currentProgramInfoText, value);
        }

        // RadikoApiServiceをコンストラクタで受け取るように変更
        public StationViewModel(Station station, ParseRadikoStation apiService)
        {
            _station = station ?? throw new ArgumentNullException(nameof(station));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService)); // ★追加

            LoadBannerImageSynchronously(station.BannerUrl); // これは同期のまま
            // _ = UpdateCurrentProgramAsync(); // ViewModel作成時に放送中番組を読み込む (呼び出し側で制御する方が良い場合も)
        }

        private void LoadBannerImageSynchronously(string bannerUrl)
        {
            // ... (既存の同期的な画像読み込み処理) ...
            if (string.IsNullOrEmpty(bannerUrl))
            {
                BannerImageSource = null;
                return;
            }
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(bannerUrl, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                BannerImageSource = bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SYNC LOAD ERROR] Failed to load banner image {bannerUrl}: {ex.Message}");
                BannerImageSource = null;
            }
        }


        public async Task UpdateCurrentProgramAsync()
        {
            string titleToShow = "番組情報なし";
            string performerToShow = string.Empty;
            string timeRangeToShow = string.Empty;
            string infoTextToShow = string.Empty;
            ProgramInfo currentProgramData = null; // APIから取得した放送中番組のデータを保持

            try
            {
                DateTime now = DateTime.Now;
                DateTime dateForScheduleRequest;
                if (now.Hour < 5) { dateForScheduleRequest = now.AddDays(-1); }
                else { dateForScheduleRequest = now; }

                List<ProgramInfo> schedule = await _apiService.GetProgramScheduleAsync(this.Id, dateForScheduleRequest);

                if (schedule != null && schedule.Any())
                {
                    currentProgramData = FindCurrentProgram(schedule, now); // ProgramInfo全体を取得

                    if (currentProgramData != null)
                    {
                        titleToShow = currentProgramData.Title;
                        performerToShow = string.IsNullOrEmpty(currentProgramData.Performer) ? "-" : currentProgramData.Performer;

                        if (!string.IsNullOrEmpty(currentProgramData.StartTimeHHmm) && !string.IsNullOrEmpty(currentProgramData.EndTimeHHmm) &&
                            currentProgramData.StartTimeHHmm.Length == 4 && currentProgramData.EndTimeHHmm.Length == 4)
                        {
                            timeRangeToShow = $"{currentProgramData.StartTimeHHmm.Insert(2, ":")} - {currentProgramData.EndTimeHHmm.Insert(2, ":")}";
                        }

                        infoTextToShow = currentProgramData.InfoText; // ★追加: InfoTextを取得
                        // 画像URLはこの時点では取得するが、実際の読み込みはUIスレッドで行う
                    }
                    else
                    {
                        titleToShow = "放送中の番組なし";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating current program data for {Name} ({Id}): {ex.Message}");
                titleToShow = "情報取得エラー";
                currentProgramData = null; // エラー時は currentProgramData もクリア
            }

            // UIスレッドでプロパティを一括更新し、画像も同期的に読み込む
            _ = Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CurrentProgramTitle = titleToShow;
                CurrentProgramPerformer = performerToShow;
                CurrentProgramTimeRange = timeRangeToShow;
                CurrentProgramInfoText = infoTextToShow; // ★追加: InfoTextを設定

                ImageSource finalProgramImageSource = null;
                if (currentProgramData != null && !string.IsNullOrEmpty(currentProgramData.ImageUrl))
                {
                    try
                    {
                        // バナー画像と同様の同期的な読み込み
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(currentProgramData.ImageUrl, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        // UIスレッドで実行しているので、ここでフリーズの可能性あり
                        bitmap.EndInit();
                        // bitmap.Freeze(); // UIスレッドで作成・使用の場合は必須ではない
                        finalProgramImageSource = bitmap;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SYNC LOAD ERROR - ProgramImage] Failed to load image {currentProgramData.ImageUrl}: {ex.Message}");
                        finalProgramImageSource = null; // エラー時はnull
                    }
                }
                CurrentProgramImageSource = finalProgramImageSource;
            });
        }

        // ProgramInfo オブジェクト全体を返すように変更
        private ProgramInfo FindCurrentProgram(List<ProgramInfo> programs, DateTime now)
        {
            const string dateTimeFormat = "yyyyMMddHHmmss";
            CultureInfo provider = CultureInfo.InvariantCulture;

            foreach (var prog in programs)
            {
                if (DateTime.TryParseExact(prog.FullStartTime, dateTimeFormat, provider, DateTimeStyles.None, out DateTime startTime) &&
                    DateTime.TryParseExact(prog.FullEndTime, dateTimeFormat, provider, DateTimeStyles.None, out DateTime endTime))
                {
                    if (now >= startTime && now < endTime)
                    {
                        return prog; // ProgramInfo オブジェクトを返す
                    }
                }
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
