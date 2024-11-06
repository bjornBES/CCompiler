namespace CCompiler.tokenizer
{
    public sealed class TokenCharConst : Token
    {
        public TokenCharConst(string raw, char value)
        {
            Raw = raw;
            Value = value;
        }

        public override TokenKind Kind { get; } = TokenKind.CHAR;
        public string Raw { get; }
        public char Value { get; }
        public override string ToString() => $"{Kind}: '{Raw}'";
    }
    public sealed class FSAChar : FSA
    {
        private enum State
        {
            START,
            END,
            ERROR,
            S,
            C,
            SO,
            SOO,
            SOOO,
            SX,
            SXH,
            SXHH
        }

        private State _state;
        private string _scanned;

        // quote : char
        // ============
        // \' in a char, and \" in a string.
        private readonly char _quote;

        public FSAChar(char quote)
        {
            _state = State.START;
            _quote = quote;
            _scanned = "";
        }

        public override void Reset()
        {
            _scanned = "";
            _state = State.START;
        }

        public override FSAStatus GetStatus()
        {
            if (_state == State.START)
            {
                return FSAStatus.NONE;
            }
            if (_state == State.END)
            {
                return FSAStatus.END;
            }
            if (_state == State.ERROR)
            {
                return FSAStatus.ERROR;
            }
            return FSAStatus.RUNNING;
        }

        // IsChar : char -> bool
        // ========================
        // the character is a 'normal' char, other than <quote> \\ or \n
        // 
        private bool IsChar(char ch)
        {
            return ch != _quote && ch != '\\' && ch != '\n';
        }



        // RetrieveRaw : () -> string
        // ==========================
        // 
        public string RetrieveRaw()
        {
            return _scanned.Substring(0, _scanned.Length - 1);
        }

        // RetrieveChar : () -> char
        // =========================
        // 
        public char RetrieveChar()
        {
            if (_scanned.Length == 3)
            {
                switch (_scanned[1])
                {
                    case 'a':
                        return '\a';
                    case 'b':
                        return '\b';
                    case 'f':
                        return '\f';
                    case 'n':
                        return '\n';
                    case 'r':
                        return '\r';
                    case 't':
                        return '\t';
                    case 'v':
                        return '\v';
                    case '\'':
                        return '\'';
                    case '\"':
                        return '\"';
                    case '\\':
                        return '\\';
                    case '?':
                        return '?';
                    default:
                        return _scanned[1];
                }
            }
            return _scanned[0];
        }

        // RetrieveToken : () -> Token
        // ===========================
        // Note that function never gets used, because FSAChar is just an inner FSA for other FSAs.
        // 
        public override Token RetrieveToken()
        {
            return new EmptyToken();
        }

        // ReadChar : char -> ()
        // =====================
        // Implementation of the FSA
        // 
        public override void ReadChar(char ch)
        {
            _scanned = _scanned + ch;
            switch (_state)
            {
                case State.END:
                case State.ERROR:
                    _state = State.ERROR;
                    break;
                case State.START:
                    if (IsChar(ch))
                    {
                        _state = State.C;
                    }
                    else if (ch == '\\')
                    {
                        _state = State.S;
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.C:
                    _state = State.END;
                    break;
                case State.S:
                    if (Utils.IsEscapeChar(ch))
                    {
                        _state = State.C;
                    }
                    else if (Utils.IsOctDigit(ch))
                    {
                        _state = State.SO;
                    }
                    else if (ch == 'x' || ch == 'X')
                    {
                        _state = State.SX;
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.SX:
                    if (Utils.IsHexDigit(ch))
                    {
                        _state = State.SXH;
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.SXH:
                    if (Utils.IsHexDigit(ch))
                    {
                        _state = State.SXHH;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.SXHH:
                    _state = State.END;
                    break;
                case State.SO:
                    if (Utils.IsOctDigit(ch))
                    {
                        _state = State.SOO;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.SOO:
                    if (Utils.IsOctDigit(ch))
                    {
                        _state = State.SOOO;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.SOOO:
                    _state = State.END;
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }

        // ReadEOF : () -> ()
        // ==================
        // 
        public override void ReadEOF()
        {
            _scanned = _scanned + '0';
            switch (_state)
            {
                case State.C:
                case State.SO:
                case State.SOO:
                case State.SOOO:
                case State.SXH:
                case State.SXHH:
                    _state = State.END;
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }

    }
    public sealed class FSACharConst : FSA
    {
        private enum State
        {
            START,
            END,
            ERROR,
            L,
            Q,
            QC,
            QCQ
        };

        private State _state;
        private char _val;
        private string _raw;
        private readonly FSAChar _fsachar;

        public FSACharConst()
        {
            _state = State.START;
            _fsachar = new FSAChar('\'');
            _raw = "";
            _val = '\0';
        }

        public override void Reset()
        {
            _state = State.START;
            _fsachar.Reset();
        }

        public override FSAStatus GetStatus()
        {
            if (_state == State.START)
            {
                return FSAStatus.NONE;
            }
            if (_state == State.END)
            {
                return FSAStatus.END;
            }
            if (_state == State.ERROR)
            {
                return FSAStatus.ERROR;
            }
            return FSAStatus.RUNNING;
        }

        public override Token RetrieveToken()
        {
            return new TokenCharConst(_raw, _val);
        }

        public override void ReadChar(char ch)
        {
            switch (_state)
            {
                case State.END:
                case State.ERROR:
                    _state = State.ERROR;
                    break;
                case State.START:
                    switch (ch)
                    {
                        case 'L':
                            _state = State.L;
                            break;
                        case '\'':
                            _state = State.Q;
                            _fsachar.Reset();
                            break;
                        default:
                            _state = State.ERROR;
                            break;
                    }
                    break;
                case State.L:
                    if (ch == '\'')
                    {
                        _state = State.Q;
                        _fsachar.Reset();
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.Q:
                    _fsachar.ReadChar(ch);
                    switch (_fsachar.GetStatus())
                    {
                        case FSAStatus.END:
                            _state = State.QC;
                            _raw = _fsachar.RetrieveRaw();
                            _val = _fsachar.RetrieveChar();
                            _fsachar.Reset();
                            ReadChar(ch);
                            break;
                        case FSAStatus.ERROR:
                            _state = State.ERROR;
                            break;
                        default:
                            break;
                    }
                    break;
                case State.QC:
                    if (ch == '\'')
                    {
                        _state = State.QCQ;
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.QCQ:
                    _state = State.END;
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }

        public override void ReadEOF()
        {
            if (_state == State.QCQ)
            {
                _state = State.END;
            }
            else
            {
                _state = State.ERROR;
            }
        }

    }
}
