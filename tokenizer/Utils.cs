using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCompiler.tokenizer
{
    public static class Utils
    {

        // IsEscapeChar : char -> bool
        // ==============================
        // 
        public static bool IsEscapeChar(char ch)
        {
            switch (ch)
            {
                case 'a':
                case 'b':
                case 'f':
                case 'n':
                case 'r':
                case 't':
                case 'v':
                case '\'':
                case '\"':
                case '\\':
                case '?':
                    return true;
                default:
                    return false;
            }
        }

        // IsHexDigit : char -> bool
        // ============================
        // 
        public static bool IsHexDigit(char ch)
        {
            return (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
        }

        // IsOctDigit : char -> bool
        // ============================
        // 
        public static bool IsOctDigit(char ch)
        {
            return ch >= '0' && ch <= '7';
        }

        // GetHexDigit : char -> long
        // ===========================
        // 
        public static long GetHexDigit(char ch)
        {
            if (ch >= '0' && ch <= '9')
            {
                return ch - '0';
            }
            if (ch >= 'a' && ch <= 'f')
            {
                return ch - 'a' + 0xA;
            }
            if (ch >= 'A' && ch <= 'F')
            {
                return ch - 'A' + 0xA;
            }
            throw new Exception("GetHexDigit: Character is not a hex digit. You should first call IsHexDigit(ch) for a check.");
        }

        // IsSpace : char -> bool
        // =========================
        // 
        public static bool IsSpace(char ch)
        {
            return (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\f' || ch == '\v');
        }
    }
}
