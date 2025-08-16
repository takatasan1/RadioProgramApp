using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RadioProgramApp.ViewModels
{
    public class RegionGroupViewModel : INotifyPropertyChanged
    {
        public string RegionName { get; }
        public ObservableCollection<StationViewModel> Stations { get; }

        public RegionGroupViewModel(string regionName, ObservableCollection<StationViewModel> stations)
        {
            RegionName = regionName;
            Stations = stations;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
