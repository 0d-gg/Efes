using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.Threading;
using System.Globalization;
using System.Reflection;

namespace Efes
{
    class Dropper
    {
        public bool Listening { get; set; }

        public Dropper(string fname,  CultureInfo culture, bool listening = true)
        {
            Listening = listening;
            var kw = ReadKeywords(fname);

            var choices = new Choices();
            choices.Add(kw.ToArray());

            var gb = new GrammarBuilder();
            gb.AppendWildcard();
            gb.Append(choices);
            gb.AppendWildcard();

            var g = new Grammar(gb);

            using( SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine(culture))
            {
                recognizer.LoadGrammar(g);
                recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
                recognizer.SetInputToDefaultAudioDevice();
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
                while (listening)
                {
                    Thread.Sleep(139);
                }
            }
            
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine(e.Result.Text);
            using (Stream file = File.Create(Path.GetRandomFileName() + ".wav"))
            {
                e.Result.Audio.WriteToWaveStream(file);
                file.Close();
            }
        }

        private IEnumerable<string> ReadKeywords(string fname)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Efes.keywords.dat";
            var keywords = new List<string>();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith("#") && !String.IsNullOrWhiteSpace(line))
                        keywords.Add(line);
                }
            }
            return keywords;
        }
    }
}
