using System.ComponentModel;

namespace MDD.Common;

public class ImageViewModel : INotifyPropertyChanged
{
	private string _imagePath;

	public string ImagePath
	{
		get
		{
			return _imagePath;
		}
		set
		{
			if (_imagePath != value)
			{
				_imagePath = value;
				OnPropertyChanged("ImagePath");
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged(string propertyName)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
