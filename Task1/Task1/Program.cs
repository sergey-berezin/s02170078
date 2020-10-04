using System;
using System.IO;
using System.Threading.Tasks;
using ImageRecognitionLibrary;

namespace Task1
{
    class Program
    {
        private static void DisplayResult(string message)
        {
            Console.WriteLine(message);
        }

        private static void DisplayInformation(string message)
        {
            Console.WriteLine(message);
        }

        static void Main(string[] args)
        {
            Classifier classifier = new Classifier();

            classifier.Result += DisplayResult;
            classifier.Information += DisplayInformation;

            Task.Run(() =>
            {
                while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
                classifier.Cancel();
            });

            classifier.PredictAll(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Images"); 
        }
    }
}
