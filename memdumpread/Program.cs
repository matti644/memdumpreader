using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace memdumpread
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //FFD8FFE000104A464946
            byte[] jpgstart = StringToByteArray("FFD8FFE000104A464946");
            byte[] jpgend = StringToByteArray("FFD9");

            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            DirectoryInfo d = new DirectoryInfo(filepath + "\\dmps");

            var files = d.GetFiles("*.DMP");

            foreach (var file in files)
            {
                var filedata = File.ReadAllBytes(file.FullName);

                int[] startIndexes = new int[200000];
                int[] endIndexes = new int[200000];

                int arrlenght = filedata.Length;

                int startArrIndex = 0;

                int breakIndex = filedata.Length - 10;

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
                    else if (filedata[i] == jpgstart[0] && filedata[i + 1] == jpgstart[1])
                    {
                        if (filedata[i + 2] == jpgstart[2] && filedata[i + 3] == jpgstart[3] &&
                            filedata[i + 4] == jpgstart[4] && filedata[i + 5] == jpgstart[5] &&
                            filedata[i + 6] == jpgstart[6] && filedata[i + 7] == jpgstart[7] &&
                            filedata[i + 8] == jpgstart[8] && filedata[i + 9] == jpgstart[9])
                        {
                            startIndexes[startArrIndex] = i;
                            startArrIndex++;
                            Console.WriteLine("found");
                        }
                    }
                });
                Parallel.For(0, startIndexes.Length, (i, state) =>
                {
                    if (state.ShouldExitCurrentIteration)
                    {
                        if (state.LowestBreakIteration < i)
                            return;
                    }
                    if (startIndexes[i] == 0)
                    {
                        if (i != 0)
                        {
                            state.Break();
                        }
                    }
                    if (i == startIndexes.Length)
                    {
                        state.Break();
                    }
                    else
                    {
                        Parallel.For(startIndexes[i], startIndexes[i + 1], (j, state) =>
                          {
                              if (state.ShouldExitCurrentIteration)
                              {
                                  if (state.LowestBreakIteration < j)
                                      return;
                              }
                              if (filedata.Length >= j + 1)
                              {
                                  if (filedata[j] == jpgend[0] && filedata[j + 1] == jpgend[1])
                                  {
                                      state.Break();
                                      var endIndex = j + 2;
                                      using (FileStream stream = new FileStream(filepath + "\\dmps\\out\\" + j + ".jpg", FileMode.Create, FileAccess.Write, FileShare.Read))
                                      {
                                          stream.Write(filedata, startIndexes[i], endIndex - startIndexes[i]);
                                      }
                                  }
                              }
                          });
                    }
                });
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}