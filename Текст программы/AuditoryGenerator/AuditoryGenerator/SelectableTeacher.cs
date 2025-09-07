using System.ComponentModel;

namespace AuditoryGenerator
{
    public class SelectableTeacher : INotifyPropertyChanged
    {
        private string name;
        private bool isSelected;
        private int preferredFloor;

        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public int PreferredFloor
        {
            get => preferredFloor;
            set
            {
                if (preferredFloor != value)
                {
                    preferredFloor = value;
                    OnPropertyChanged(nameof(PreferredFloor));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}