using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace memdumpread
{
    internal class Program
    {
        public static int arrlenght = 0;

        private static void Main(string[] args)
        {
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            DirectoryInfo d = new DirectoryInfo(filepath + "\\dmps");

            var files = d.GetFiles("*.DMP");

            SearchInfo[] imageTypes = new SearchInfo[2];
            for (int i = 0; imageTypes.Length > i; i++)
            {
                imageTypes[i] = new SearchInfo();
            }

            //jpg
            imageTypes[0].AddHeaderStart("FFD8FFE000104A464946");
            imageTypes[0].AddHeaderEnd("FFD9");
            imageTypes[0].extension = ".jpg";
            //png
            imageTypes[1].AddHeaderStart("89504E470D0A1A0A0000000D49484452");
            imageTypes[1].AddHeaderEnd("49454E44AE426082");
            imageTypes[1].extension = ".png";

            int typeCount = imageTypes.Length;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            foreach (var file in files)
            {
                var filedata = File.ReadAllBytes(file.FullName);

                arrlenght = filedata.Length;

                int breakIndex = filedata.Length;

                Dictionary<int, int> written = new Dictionary<int, int>();

                Parallel.For(0, arrlenght, (i, state) =>
                {
                    if (state.ShouldExitCurrentIteration)
                    {
                        if (state.LowestBreakIteration < i)
                            return;
                    }
                    if (i >= breakIndex)
                    {
                        state.Break();
                    }
                    else
                    {
                        for (int x = 0; x < typeCount; x++)
                        {
                            if (checkIndex(i, imageTypes[x].headerStart, filedata))
                            {
                                imageTypes[x].AddStartIndex(i);
                                Console.WriteLine("found");
                            }
                        }
                    }
                });
                Console.WriteLine("starting writing to disk from file: " + file.Name);
                for (int x = 0; x < imageTypes.Length; x++)
                {
                    imageTypes[x].startIndexes.Sort();
                    Console.WriteLine("writing " + imageTypes[x].extension);
                    bool shouldBreak = false;
                    for (int i = 0; i < imageTypes[x].startIndexes.Count; i++)
                    {
                        if (i + 1 >= imageTypes[x].startIndexes.Count)
                        {
                            imageTypes[x].startIndexes.Add(arrlenght);
                            shouldBreak = true;
                        }
                        Parallel.For(imageTypes[x].startIndexes[i], imageTypes[x].startIndexes[i + 1], (j, state) =>
                        {
                            if (state.ShouldExitCurrentIteration)
                            {
                                if (state.LowestBreakIteration < j)
                                    return;
                            }
                            if (j >= imageTypes[x].startIndexes[i + 1])
                            {
                                state.Break();
                            }
                            if (filedata.Length >= j + imageTypes[x].headerEnd.Length)
                            {
                                if (checkIndex(j, imageTypes[x].headerEnd, filedata))
                                {
                                    state.Break();
                                    var endIndex = j + imageTypes[x].headerEnd.Length;
                                    if (!written.ContainsValue(imageTypes[x].startIndexes[i]))
                                    {
                                        try
                                        {
                                            written.Add(imageTypes[x].startIndexes[i], endIndex);
                                        }
                                        catch { }
                                        using (FileStream stream = new FileStream(filepath + "\\dmps\\out\\" + imageTypes[x].extension + "\\" + x + j + imageTypes[x].extension, FileMode.Create, FileAccess.Write, FileShare.Read))
                                        {
                                            stream.Write(filedata, imageTypes[x].startIndexes[i], endIndex - imageTypes[x].startIndexes[i]);
                                        }
                                    }
                                }
                            }
                        });
                        if (shouldBreak) { break; }
                    }
                }
                written.Clear();
            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
        }

        public static bool checkIndex(int i, byte[] fileHeader, byte[] filedata)
        {
            int headerLength = fileHeader.Length;
            if (i <= arrlenght - headerLength)
            {
                for (int x = 0; x < headerLength; x++)
                {
                    if (filedata[i + x] == fileHeader[x])
                    {
                        if (x + 1 == headerLength)
                        {
                            return true;
                        }
                    }
                    else { return false; }
                }
            }
            return false;
        }
    }
}