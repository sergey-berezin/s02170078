using System;
using System.IO;
using System.Threading.Tasks;
using ImageRecognitionLibrary;

namespace Task1
{
    class Program
    {
        private static void DisplayResult(PredictionResult predictionResult)
        {
            Console.WriteLine(predictionResult.File + " " + predictionResult.Prediction);
        }

        private static void DisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

        static async Task Main(string[] args)
        {
            Classifier classifier = new Classifier();

            classifier.Result += DisplayResult;
            classifier.Message += DisplayMessage;

            await classifier.PredictAll(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Images");
        }
    }
}
