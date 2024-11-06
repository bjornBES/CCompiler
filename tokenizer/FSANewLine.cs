namespace CCompiler.tokenizer
{
    public sealed class FSANewLine : FSA
    {
        private enum State
        {
            START,
            END,
            ERROR,
            NEWLINE
        };

        private State _state;

        public FSANewLine()
        {
            _state = State.START;
        }

        public override void Reset()
        {
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

        public override Token RetrieveToken()
        {
            return new EmptyToken();
        }

        public override void ReadChar(Char ch)
        {
            switch (_state)
            {
                case State.END:
                case State.ERROR:
                    _state = State.ERROR;
                    break;
                case State.START:
                    if (ch == '\n')
                    {
                        _state = State.NEWLINE;
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.NEWLINE:
                    _state = State.END;
                    break;
            }
        }

        public override void ReadEOF()
        {
            switch (_state)
            {
                case State.NEWLINE:
                    _state = State.END;
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }
    }
}
