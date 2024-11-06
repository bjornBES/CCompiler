using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCompiler.tokenizer
{
    public enum FSAStatus
    {
        NONE,
        END,
        RUNNING,
        ERROR
    }
    public abstract class FSA
    {
        public abstract FSAStatus GetStatus();
        public abstract void ReadChar(char c);
        public abstract void Reset();
        public abstract void ReadEOF();
        public abstract Token RetrieveToken();
    }
}
