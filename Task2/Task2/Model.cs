using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Threading;
using ImageRecognitionLibrary;
using System.Windows;
using System.IO;

namespace Task2
{
    public class Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Classifier classifier = new Classifier(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.FullName + "\\Task1\\Model");

        public Dispatcher CurDispatcher;

        public string[] Labels { get; set; }

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Model(Dispatcher curDispatcher)
        {
            CurDispatcher = curDispatcher;
            All = new ObservableCollection<OutputPrediction>();
            Selected = new ObservableCollection<OutputPrediction>();
            Number = new ObservableCollection<OutputNumber>();
            foreach(string label in classifier.classLabels)
            {
                Number.Add(new OutputNumber { Label = label, Number = 0 });
            }
            classifier.Result += UpdateResult;
            Labels = classifier.classLabels;
        }

        private void UpdateResult(PredictionResult predictionResult)
        {
            CurDispatcher.Invoke(() =>
            {
                if (SelectedItem != null && predictionResult.Prediction == SelectedItem)
                {
                    Selected.Add(new OutputPrediction { Prediction = predictionResult.Prediction, Image = new BitmapImage(new Uri(predictionResult.Path)) });
                }
                foreach (var i in Number)
                {
                    if (i.Label == predictionResult.Prediction)
                    {
                        i.Number++;
                    }
                }
                All.Add(new OutputPrediction { Prediction = predictionResult.Prediction, Image = new BitmapImage(new Uri(predictionResult.Path)) });

            }, DispatcherPriority.Render);
        }

        public ObservableCollection<OutputPrediction> All { get; set; }

        public ObservableCollection<OutputPrediction> Selected { get; set; }

        public ObservableCollection<OutputNumber> Number { get; set; }

        private string folder;

        public string FolderPath
        {
            get
            {
                return folder;
            }
            set
            {
                folder = value;
                OnPropertyChanged("FolderPath");
            }
        }

        private string selectedItem;

        public string SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                if (value != null)
                {
                    Selected = new ObservableCollection<OutputPrediction>(All.Where(x => x.Prediction == selectedItem));
                    OnPropertyChanged("Selected");
                }
            }
        }

        public async void Start()
        {
            await classifier.PredictAll(FolderPath);
        }

        public void Stop()
        {
            classifier.Cancel();
        }
    }

    public class OutputPrediction
    {
        public string Prediction { get; set; }
        public BitmapImage Image { get; set; }
    }

    public class OutputNumber
    {
        public string Label { get; set; }
        public int Number { get; set; }
    }
}
