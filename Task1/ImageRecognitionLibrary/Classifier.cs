using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace ImageRecognitionLibrary
{
    public class PredictionResult
    {
      
        public string Path { get; set; }
        public string File { get; set; }
        public string Prediction { get; set; }
        public override string ToString()
        {
            return Prediction;
        }
    }

    public class Classifier
    {
        
        public delegate void ImageRecognitionHandler(PredictionResult predictionResult);

        public delegate void MessageHandler(string message);

        public event ImageRecognitionHandler Result;

        public event MessageHandler Message;

        public CancellationTokenSource CancelTokenSource;

        public string ModelFolder = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Model";

        public string ModelFile { get; set; }

        public string[] classLabels { get; set; }

        public int TargetWidth { get; set; }

        public int TargetHeight { get; set; }


        public Classifier(string modelFolder, string modelFile = "\\model.onnx", string labelFile = "\\labels.txt", int targetWidth = 28, int targetHeight = 28)
        {
            ModelFolder = modelFolder;
            ModelFile = modelFile;
            classLabels = File.ReadAllLines(ModelFolder + labelFile);
            TargetWidth = targetWidth;
            TargetHeight = targetHeight;
        }

        public void Cancel()
        {
            Message?.Invoke("Остановка");
            CancelTokenSource.Cancel();
        }

        public async Task PredictAll(string targetDirectory)
        {
            DirectoryInfo dir = new DirectoryInfo(targetDirectory);

            if (!dir.Exists)
            {
                Message?.Invoke("Директория не найдена.");
                return;
            }
            CancelTokenSource = new CancellationTokenSource();
            CancellationToken token = CancelTokenSource.Token;

            var tasks = new List<Task>();

            foreach (var curImg in dir.GetFiles())
            {
                tasks.Add(Task.Factory.StartNew((img) =>
                {
                    FileInfo pImg = (FileInfo)img;
                    PredictionResult result = new PredictionResult { Path = pImg.FullName, File = pImg.Name, Prediction = Predict(pImg.FullName) };
                    Result?.Invoke(result);
                }, curImg, token));
            }

            Task t = Task.WhenAll(tasks);
            
            try
            {
                await t;
            }
            catch (OperationCanceledException ex)
            {
                Message?.Invoke("Процесс был прекращен.");
            }
        }

        public string Predict(string img)
        {
            using var image = Image.Load<Rgb24>(img);

            image.Mutate(x =>
            {
                x.Resize(new ResizeOptions
                {
                    Size = new Size(TargetWidth, TargetHeight),
                    Mode = ResizeMode.Crop
                });
                x.Grayscale();
            });

            var input = new DenseTensor<float>(new[] { 1, 1, TargetHeight, TargetWidth });
            for (int y = 0; y < TargetHeight; y++)
            {
                Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < TargetWidth; x++)
                {
                    input[0, 0, y, x] = pixelSpan[x].R / 255.0f;

                }
            }
            using var session = new InferenceSession(ModelFolder + ModelFile);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(session.InputMetadata.Keys.First(), input)
            };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

            var output = results.First().AsEnumerable<float>().ToArray();
            var sum = output.Sum(x => (float)Math.Exp(x));
            var softmax = output.Select(x => (float)Math.Exp(x) / sum);

            return classLabels[softmax.ToList().IndexOf(softmax.Max())];
        }
    }
}
