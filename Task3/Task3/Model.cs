using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Threading;
using ImageRecognitionLibrary;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Task3
{
    public class Model : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Classifier classifier = new Classifier(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.FullName + "\\Task1\\Model");

        public Dispatcher CurDispatcher;

        public ImageRecognitionContext CurDbContext;

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
            RepeatedCalls = new ObservableCollection<OutputRepeatedCalls>();
            CurDbContext = new ImageRecognitionContext();
            foreach (string label in classifier.classLabels)
            {
                Number.Add(new OutputNumber { Label = label, Number = 0 });
            }
            Labels = classifier.classLabels;
        }

        public ObservableCollection<OutputPrediction> All { get; set; }

        public ObservableCollection<OutputPrediction> Selected { get; set; }

        public ObservableCollection<OutputNumber> Number { get; set; }

        public ObservableCollection<OutputRepeatedCalls> RepeatedCalls { get; set; }

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

        public async Task Start()
        {
            DirectoryInfo dir = new DirectoryInfo(FolderPath);

            classifier.CancelTokenSource = new CancellationTokenSource();
            CancellationToken token = classifier.CancelTokenSource.Token;

            var tasks = new List<Task>();

            foreach (var curImg in dir.GetFiles())
            {
                tasks.Add(Task.Factory.StartNew((img) =>
                {

                    FileInfo pImg = (FileInfo)img;

                    PredictionResult predictionResult = ImageInDB(pImg);

                    if (predictionResult == null)
                    {

                        predictionResult = new PredictionResult { Path = pImg.FullName, File = pImg.Name, Prediction = classifier.Predict(pImg.FullName) };
                        lock (CurDbContext)
                        {
                            Blob blob = new Blob { ImageBytes = File.ReadAllBytes(predictionResult.Path) };
                            CurDbContext.Blobs.Add(blob);
                            CurDbContext.SaveChanges();
                            CurDbContext.Images.Add(new Image
                            {
                                Hash = GetHashCode(File.ReadAllBytes(predictionResult.Path)),
                                Prediction = predictionResult.Prediction,
                                RepeatedCallsNumber = 0,
                                BlobId = blob.Id
                            });
                            CurDbContext.SaveChanges();
                        }
                    }

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

                }, curImg, token));

            }

            Task t = Task.WhenAll(tasks);

            try
            {
                await t;
            }
            catch (OperationCanceledException ex)
            {

            }
        }

        public PredictionResult ImageInDB(FileInfo pImg)
        {
            lock (CurDbContext)
            {
                byte[] data = File.ReadAllBytes(pImg.FullName);

                var sameHash = CurDbContext.Images.Where(x => x.Hash == GetHashCode(data));

                if (sameHash != null)
                {
                    foreach (var i in sameHash.ToList())
                    {
                        var sameBlob = CurDbContext.Blobs.Find(i.BlobId);
                        if (sameBlob != null && data.SequenceEqual(sameBlob.ImageBytes))
                        {
                            CurDbContext.Images.Where(x => x.BlobId == sameBlob.Id).First().RepeatedCallsNumber++;
                            CurDbContext.SaveChanges();
                            return new PredictionResult { Path = pImg.FullName, File = pImg.Name, Prediction = i.Prediction };
                        }
                    }
                }
                return null;
            }
        }

        public void Refresh()
        {
            RepeatedCalls.Clear();
            lock (CurDbContext)
            {
                if (CurDbContext.Blobs != null)
                {
                    foreach (var i in CurDbContext.Blobs.ToList())
                    {
                        RepeatedCalls.Add(new OutputRepeatedCalls
                        {
                            Image = ToImage(i.ImageBytes),
                            Number = CurDbContext.Images.Where(x => x.BlobId == i.Id).First().RepeatedCallsNumber
                        });
                    }
                }
            }
        }

        public void Stop()
        {
            classifier.Cancel();
        }

        public void ClearDB()
        {
            CurDbContext.Clear();
        }

        public BitmapImage ToImage(byte[] array)
        {
            using (var ms = new System.IO.MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        public static int GetHashCode(byte[] data)
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < data.Length; i++)
                    hash = (hash ^ data[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }
    }

    public class OutputRepeatedCalls
    {
        public BitmapImage Image { get; set; }
        public int Number { get; set; }
    }

    public class OutputPrediction
    {
        public string Prediction { get; set; }
        public BitmapImage Image { get; set; }
    }

    public class OutputNumber : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Label { get; set; }

        private int number;

        public int Number
        {
            get
            {
                return number;
            }
            set
            {
                number = value;
                OnPropertyChanged("Number");
            }
        }
    }
}
