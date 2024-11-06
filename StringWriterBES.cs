using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCompiler
{
    public class StringWriterBES
    {
        string str;
        string NEWLINE = Environment.NewLine;
        private void AddString(string value)
        {
            string[] splited = value.Split('\t');

            splited[0] = splited[0].PadRight(6, ' ');
            value = splited[0];

            for (int i = 1; i < splited.Length; i++)
            {
                value += splited[i].PadRight(25, ' ');
            }

            value = value.TrimEnd();

            str += value;
        }
        public void WriteLine(string value)
        {
            if (value != null)
            {
                AddString(value);
            }
            AddString(NEWLINE);
        }
        public void WriteLine()
        {
            AddString(NEWLINE);
        }
        public void Write(string value)
        {
            AddString(value);
        }

        public override string ToString()
        {
            return str;
        }
    }
}
