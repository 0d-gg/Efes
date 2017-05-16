using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Recognition;
using System.Globalization;
using System.Reflection;

namespace Efes
{
    class Dropper
    {
        public string StorageDirectory { get; set; }

        private SpeechRecognitionEngine recognizer;

        public Dropper(string fname,  CultureInfo culture, string storageDirectory = null)
        {
            //read keywords from dat file
            var kw = ReadKeywords();

            //use a random temp directory if not specified
            if (storageDirectory == null || !Directory.Exists(storageDirectory))
                storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            StorageDirectory = storageDirectory;
            Directory.CreateDirectory(StorageDirectory);

#if DEBUG
            Console.WriteLine(StorageDirectory);
#endif

            var choices = new Choices(kw.ToArray());
            var gb = new GrammarBuilder();
            gb.AppendWildcard();
            gb.Append(choices);
            gb.AppendWildcard();
            var g = new Grammar(gb);

            recognizer = new SpeechRecognitionEngine(culture);
            recognizer.LoadGrammar(g);
            recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            recognizer.SetInputToDefaultAudioDevice();
        }

        /// <summary>
        /// Enable speech recognition and logging
        /// </summary>
        public void StartListening()
        {
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        /// <summary>
        /// Disable speech recognition and logging
        /// </summary>
        public void StopListening()
        {
            recognizer.RecognizeAsyncStop();
        }

        /// <summary>
        /// Collects files into one wav file
        /// </summary>
        /// <returns>The path to the single wav file</returns>
        public string Collectfiles()
        {
            var files = CollapseWaves(new DirectoryInfo(StorageDirectory));
            return files.Where(x => x.Extension.ToLower() == ".wav").First().FullName;
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
#if DEBUG
            Console.WriteLine(e.Result.Text);
#endif
            using (Stream file = File.Create(Path.Combine(StorageDirectory, Path.GetRandomFileName()) + ".efd"))
            {
                e.Result.Audio.WriteToWaveStream(file);
                file.Close();
            }
        }

        /// <summary>
        /// Reads the keywords to listen for out of an embedded dat file
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> ReadKeywords()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Efes.keywords.dat";
            var keywords = new List<string>();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!line.StartsWith("#") && !String.IsNullOrWhiteSpace(line)) //ignore commented lines
                            keywords.Add(line);
                    }
                }
            }
            return keywords;
        }

        /// <summary>
        /// Recursively merge all files in a directory with a WAV header
        /// </summary>
        /// <param name="dir">Merging directory</param>
        /// <returns></returns>
        private IEnumerable<FileInfo> CollapseWaves(DirectoryInfo dir)
        {
            const short CHANNELS = 1;
            const int SAMPLE_RATE = 16000;
            const short BITS_PER_SAMPLE = 16;
            const int HEADER_SIZE = 44; //size of a wav header
            //these are all case-sensitive / length-sensitive
            const string RIFF = "RIFF";
            const string WAVEfmt = "WAVEfmt ";
            const string DATA = "data";

            var files = dir.GetFiles().Where(f => f.Extension.ToLower() == ".wav" || f.Extension.ToLower() == ".efd");

            if (files.Count() <= 1)
                return files;
            else
            {
                //get first file in the directory
                var first = new FileStream(files.ElementAt(0).FullName, FileMode.Open, FileAccess.Read);
                //get second file
                var second = new FileStream(files.ElementAt(1).FullName, FileMode.Open, FileAccess.Read);
                //each file has a header, so add the total bytes and then subtract the header size for each
                int length = (int)(first.Length + second.Length - (2 * HEADER_SIZE));

                var merged = new FileStream(Path.Combine(dir.FullName, Path.GetRandomFileName()) + ".wav", FileMode.Create, FileAccess.Write);

                var bw = new BinaryWriter(merged);
                bw.Write(RIFF.ToCharArray());
                bw.Write(length + HEADER_SIZE);

                bw.Write(WAVEfmt.ToCharArray());
                //chunk information and audio format info
                bw.Write((int)16);
                bw.Write((short)1);
                bw.Write(CHANNELS);
                bw.Write(SAMPLE_RATE);
                bw.Write((int)(SAMPLE_RATE * ((BITS_PER_SAMPLE * CHANNELS) / 8)));
                bw.Write((short)((BITS_PER_SAMPLE * CHANNELS) / 8));
                bw.Write(BITS_PER_SAMPLE);
                bw.Write(DATA.ToCharArray());
                bw.Write(length);

                //read the files with an offset of the headersize
                byte[] arrFirst = new byte[first.Length];
                first.Read(arrFirst, HEADER_SIZE, arrFirst.Length - HEADER_SIZE);
                byte[] arrSecond = new byte[second.Length];
                second.Read(arrSecond, HEADER_SIZE, arrSecond.Length - HEADER_SIZE);

                first.Close();
                second.Close();

                bw.Write(arrFirst);
                bw.Write(arrSecond);

                bw.Close();
                merged.Close();
                //delete merged files
                File.Delete(files.ElementAt(0).FullName);
                File.Delete(files.ElementAt(1).FullName);
                return CollapseWaves(dir);
            }
        }
    }
}
