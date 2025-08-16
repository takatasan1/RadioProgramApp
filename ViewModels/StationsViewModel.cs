using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using RadioProgramApp.Models;

namespace RadioProgramApp.ViewModels
{
    public class StationsViewModel : INotifyPropertyChanged
    {
        private readonly ParseRadikoStation _parseRadikoStation;
        private List<Station> _allStations; // APIから取得した全ての放送局情報を保持

        public ObservableCollection<string> AvailableRegions { get; }
        private string _selectedRegionName;
        private const string DefaultRegion = "関東"; // デフォルト地域

        public string SelectedRegionName
        {
            get => _selectedRegionName;
            set
            {
                // SetPropertyはINotifyPropertyChangedの実装を助けるヘルパーメソッド
                // (別途定義するか、既存の基底クラスにあればそれを使用)
                if (SetProperty(ref _selectedRegionName, value))
                {
                    this.FilterAndDisplayStations(); // 選択地域が変更されたら表示を更新
                }
            }
        }

        private ObservableCollection<RegionGroupViewModel> _regionGroups;
        public ObservableCollection<RegionGroupViewModel> RegionGroups
        {
            get => _regionGroups;
            private set => SetProperty(ref _regionGroups, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsContentLoaded));
                }
            }
        }
        public bool IsContentLoaded => !_isLoading;

        public StationsViewModel()
        {
            _parseRadikoStation = new ParseRadikoStation();
            AvailableRegions = new ObservableCollection<string>();
            RegionGroups = new ObservableCollection<RegionGroupViewModel>();
            _allStations = new List<Station>(); // 初期化
            _ = LoadStationsAsync();
        }

        
        public async Task LoadStationsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                _allStations = await _parseRadikoStation.GetStationsAsync();

                if (_allStations != null && _allStations.Any())
                {
                    var uniqueRegions = _allStations
                        .Select(s => s.RegionName)
                        .Distinct()
                        .ToList();

                    // UIスレッドでAvailableRegionsを更新
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AvailableRegions.Clear();
                        foreach (var region in uniqueRegions)
                        {
                            AvailableRegions.Add(region);
                        }
                    });

                    // デフォルト地域を設定 (これによりSelectedRegionNameのセッターが呼ばれ、フィルタリングも実行される)
                    if (AvailableRegions.Contains(DefaultRegion))
                    {
                        SelectedRegionName = DefaultRegion;
                    }
                    else if (AvailableRegions.Any())
                    {
                        SelectedRegionName = AvailableRegions.First(); // 関東がない場合は最初の地域
                    }
                    else
                    {
                        // 利用可能な地域がない場合、SelectedRegionNameはnull/空のまま
                        // FilterAndDisplayStationsを明示的に呼んで空のリストを表示
                        FilterAndDisplayStations();
                    }
                }
                else
                {
                    // 全局データが取得できなかった場合
                    Application.Current.Dispatcher.Invoke(() => AvailableRegions.Clear());
                    FilterAndDisplayStations(); // 空のリストを表示
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"放送局リストの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Failed to load stations: {ex.Message}");
                Application.Current.Dispatcher.Invoke(() => AvailableRegions.Clear());
                FilterAndDisplayStations(); // エラー時も空のリストを表示
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterAndDisplayStations()
        {
            var newRegionGroups = new ObservableCollection<RegionGroupViewModel>();

            if (_allStations != null && _allStations.Any() && !string.IsNullOrEmpty(SelectedRegionName))
            {
                var stationsInSelectedRegion = _allStations
                    .Where(s => s.RegionName == SelectedRegionName)
                    .ToList();

                if (stationsInSelectedRegion.Any())
                {
                    // StationViewModel作成時にRadikoApiServiceインスタンスを渡す
                    var stationViewModels = stationsInSelectedRegion
                        .Select(s => new StationViewModel(s, _parseRadikoStation)) // ★変更: _apiServiceを渡す
                        .ToList();

                    foreach (var svm in stationViewModels)
                    {
                        // 各StationViewModelの放送中番組情報を非同期で読み込む
                        // UIをブロックしないように `_ =` で呼び出し、結果を待たない
                        _ = svm.UpdateCurrentProgramAsync();
                    }

                    var group = new RegionGroupViewModel(SelectedRegionName, new ObservableCollection<StationViewModel>(stationViewModels));
                    newRegionGroups.Add(group);
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                RegionGroups = newRegionGroups;
            });
        }


        // INotifyPropertyChangedの実装 (SetPropertyヘルパーを含む)
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
