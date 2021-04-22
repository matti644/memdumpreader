using System;
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
                    Console.WriteLine("writing " + imageTypes[x].extension);
                    Parallel.For(0, imageTypes[x].startIndexes.Length, (i, statei) =>
                    {
                        if (statei.ShouldExitCurrentIteration)
                        {
                            if (statei.LowestBreakIteration < i)
                                return;
                        }
                        if (imageTypes[x].startIndexes[i] == 0)
                        {
                            if (i != 0)
                            {
                                statei.Break();
                            }
                        }
                        if (i == imageTypes[x].startIndexes.Length)
                        {
                            statei.Break();
                        }
                        else
                        {
                            Parallel.For(imageTypes[x].startIndexes[i], imageTypes[x].startIndexes[i + 1], (j, statej) =>
                              {
                                  if (statej.ShouldExitCurrentIteration)
                                  {
                                      if (statej.LowestBreakIteration < j)
                                          return;
                                  }
                                  if (filedata.Length >= j + 1)
                                  {
                                      //if (filedata[j] == imageTypes[x].headerEnd[0] && filedata[j + 1] == imageTypes[x].headerEnd[1])
                                      //{
                                      if (checkIndex(j, imageTypes[x].headerEnd, filedata))
                                      {
                                          statej.Break();
                                          var endIndex = j + imageTypes[x].headerEnd.Length;
                                          using (FileStream stream = new FileStream(filepath + "\\dmps\\out\\" + j + imageTypes[x].extension, FileMode.Create, FileAccess.Write, FileShare.Read))
                                          {
                                              stream.Write(filedata, imageTypes[x].startIndexes[i], endIndex - imageTypes[x].startIndexes[i]);
                                          }
                                      }
                                  }
                              });
                        }
                    });
                }
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