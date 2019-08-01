using UnityEngine;
using System.IO;
using System.Text;

namespace Loju.Localisation.Editor
{

    public sealed class CSVParser : System.IDisposable
    {

        private StringReader _reader;

        public CSVParser(string csvData)
        {
            _reader = new StringReader(csvData);
        }

        public void Dispose()
        {
            if (_reader != null) _reader.Dispose();
            _reader = null;
        }

        public int Peek()
        {
            return _reader.Peek();
        }

        public int ParseCSVLineNonAlloc(string[] output, int columnLimit = 1000, bool includeWhiteSpace = false)
        {
            StringBuilder sb = new StringBuilder();
            columnLimit = Mathf.Min(columnLimit, output.Length);

            bool inQuote = false, active = true;
            int columnIndex = 0;
            while (active && _reader.Peek() != -1)
            {

                char c = (char)_reader.Read();

                if (!inQuote && c == '\n' || c == '\r')
                {
                    // end row
                    while ((char)_reader.Peek() == '\n' || (char)_reader.Peek() == '\r') _reader.Read();
                    active = false;
                }
                else if (c == ',' && !inQuote)
                {
                    // end column
                }
                else if (c == '\"')
                {
                    if (!inQuote) inQuote = true;
                    else
                    {
                        if ((char)_reader.Peek() == ',' || (char)_reader.Peek() == '\n' || (char)_reader.Peek() == '\r' || _reader.Peek() == -1)
                        {
                            if ((sb.Length > 0 || includeWhiteSpace) && columnIndex < columnLimit)
                            {
                                output[columnIndex] = sb.ToString();
                                columnIndex++;
                            }

                            sb.Length = 0;

                            inQuote = false;
                        }
                        else sb.Append(c);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            return columnIndex;
        }
    }

}


