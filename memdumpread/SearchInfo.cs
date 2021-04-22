using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace memdumpread
{
    internal class SearchInfo
    {
        public byte[] headerStart { get; set; }
        public byte[] headerEnd { get; set; }
        public List<int> startIndexes { get; set; }
        public List<int> endIndexes { get; set; }

        private int startArrIndex { get; set; }
        private int endArrIndex { get; set; }

        public string extension { get; set; }

        public SearchInfo()
        {
            startIndexes = new List<int>();
            endIndexes = new List<int>();
        }

        public void AddStartIndex(int i)
        {
            startIndexes.Add(i);
            startArrIndex++;
        }

        public void AddEndIndex(int i)
        {
            endIndexes.Add(i);
            endArrIndex++;
        }

        public void AddHeaderStart(string s)
        {
            headerStart = Array.Empty<byte>();
            headerStart = StringToByteArray(s);
        }

        public void AddHeaderEnd(string s)
        {
            headerEnd = Array.Empty<byte>();
            headerEnd = StringToByteArray(s);
        }

        internal static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}