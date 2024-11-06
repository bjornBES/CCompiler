using CCompiler.tokenizer;
using System.Collections.Immutable;

namespace CCompiler.Parsing
{
    public class ParserThenParser<R1, R2> : IParser<Tuple<R2, R1>>
    {
        public ParserThenParser(IParser<R1> firstParser, IParser<R2> secondParser)
        {
            FirstParser = firstParser;
            SecondParser = secondParser;
        }

        public IParser<R1> FirstParser { get; }
        public IParser<R2> SecondParser { get; }
        public RuleCombining Combining => RuleCombining.THEN;

        public IParserResult<Tuple<R2, R1>> Parse(ParserInput input)
        {
            var firstResult = FirstParser.Parse(input);
            if (!firstResult.IsSuccessful)
            {
                return new ParserFailed<Tuple<R2, R1>>();
            }
            var secondResult = SecondParser.Parse(firstResult.ToInput());
            if (!secondResult.IsSuccessful)
            {
                return new ParserFailed<Tuple<R2, R1>>();
            }
            return ParserSucceeded.Create(Tuple.Create(secondResult.Result, firstResult.Result), secondResult.Environment, secondResult.Source);
        }
    }

    public class ParserThenConsumer<R> : IParser<R>
    {
        public ParserThenConsumer(IParser<R> parser, IConsumer consumer)
        {
            Parser = parser;
            Consumer = consumer;
        }

        public IParser<R> Parser { get; }
        public IConsumer Consumer { get; }
        public RuleCombining Combining => RuleCombining.THEN;

        public IParserResult<R> Parse(ParserInput input)
        {
            var firstResult = Parser.Parse(input);
            if (!firstResult.IsSuccessful)
            {
                return new ParserFailed<R>();
            }
            var secondResult = Consumer.Consume(firstResult.ToInput());
            if (!secondResult.IsSuccessful)
            {
                return new ParserFailed<R>();
            }
            return ParserSucceeded.Create(firstResult.Result, secondResult.Environment, secondResult.Source);
        }
    }

    public class ParserThenTransformer<R1, R2> : IParser<R2>
    {
        public ParserThenTransformer(IParser<R1> parser, ITransformer<R1, R2> transformer)
        {
            Parser = parser;
            Transformer = transformer;
        }

        public IParser<R1> Parser { get; }
        public ITransformer<R1, R2> Transformer { get; }
        public RuleCombining Combining => RuleCombining.THEN;

        public IParserResult<R2> Parse(ParserInput input)
        {
            var firstResult = Parser.Parse(input);
            if (!firstResult.IsSuccessful)
            {
                return new ParserFailed<R2>();
            }
            return Transformer.Transform(firstResult.Result, firstResult.ToInput());
        }
    }

    public class ConsumerThenParser<R> : IParser<R>
    {
        public ConsumerThenParser(IConsumer consumer, IParser<R> parser)
        {
            Consumer = consumer;
            Parser = parser;
        }

        public IConsumer Consumer { get; }
        public IParser<R> Parser { get; }
        public RuleCombining Combining => RuleCombining.THEN;

        public IParserResult<R> Parse(ParserInput input)
        {
            var firstResult = Consumer.Consume(input);
            if (!firstResult.IsSuccessful)
            {
                return new ParserFailed<R>();
            }
            return Parser.Parse(firstResult.ToInput());
        }
    }

    public class ConsumerThenConsumer : IConsumer
    {
        public ConsumerThenConsumer(IConsumer firstConsumer, IConsumer secondConsumer)
        {
            FirstConsumer = firstConsumer;
            SecondConsumer = secondConsumer;
        }

        public IConsumer FirstConsumer { get; }
        public IConsumer SecondConsumer { get; }

        public IParserResult Consume(ParserInput input)
        {
            var result1 = FirstConsumer.Consume(input);
            if (!result1.IsSuccessful)
            {
                return result1;
            }
            return SecondConsumer.Consume(result1.ToInput());
        }
    }

    public class TransformerThenParser<S, R1, R2> : ITransformer<S, Tuple<R2, R1>>
    {
        public TransformerThenParser(ITransformer<S, R1> transformer, IParser<R2> parser)
        {
            Transformer = transformer;
            Parser = parser;
        }

        public ITransformer<S, R1> Transformer { get; }
        public IParser<R2> Parser { get; }

        public IParserResult<Tuple<R2, R1>> Transform(S seed, ParserInput input)
        {
            var result1 = Transformer.Transform(seed, input);
            if (!result1.IsSuccessful)
            {
                return new ParserFailed<Tuple<R2, R1>>();
            }
            var result2 = Parser.Parse(result1.ToInput());
            if (!result2.IsSuccessful)
            {
                return new ParserFailed<Tuple<R2, R1>>();
            }
            return ParserSucceeded.Create(Tuple.Create(result2.Result, result1.Result), result2.Environment, result2.Source);
        }
    }

    public class TransformerThenConsumer<S, R> : ITransformer<S, R>
    {
        public TransformerThenConsumer(ITransformer<S, R> transformer, IConsumer consumer)
        {
            Transformer = transformer;
            Consumer = consumer;
        }

        public ITransformer<S, R> Transformer { get; }
        public IConsumer Consumer { get; }

        public IParserResult<R> Transform(S seed, ParserInput input)
        {
            var result1 = Transformer.Transform(seed, input);
            if (!result1.IsSuccessful)
            {
                return result1;
            }
            var result2 = Consumer.Consume(result1.ToInput());
            if (!result2.IsSuccessful)
            {
                return new ParserFailed<R>();
            }
            return ParserSucceeded.Create(result1.Result, result2.Environment, result2.Source);
        }
    }

    public class TransformerThenTransformer<S, I, R> : ITransformer<S, R>
    {
        public TransformerThenTransformer(ITransformer<S, I> firstTransformer, ITransformer<I, R> secondTransformer)
        {
            FirstTransformer = firstTransformer;
            SecondTransformer = secondTransformer;
        }

        public ITransformer<S, I> FirstTransformer { get; }
        public ITransformer<I, R> SecondTransformer { get; }

        public IParserResult<R> Transform(S seed, ParserInput input)
        {
            var result1 = FirstTransformer.Transform(seed, input);
            if (!result1.IsSuccessful)
            {
                return new ParserFailed<R>();
            }
            return SecondTransformer.Transform(result1.Result, result1.ToInput());
        }
    }

    public enum RuleCombining
    {
        NONE,
        THEN,
        OR
    }

    /// <summary>
    /// A parser consumes one or several tokens, and produces a result.
    /// </summary>
    public interface IParser<out R>
    {
        IParserResult<R> Parse(ParserInput input);
        RuleCombining Combining { get; }
    }

    public static class Parser
    {
        public static NamedParser<R> Create<R>(string name) =>
            new NamedParser<R>(name);
        public static IParser<R> Seed<R>(R seed) =>
            new AlwaysSucceedingParser<R>(seed);
    }

    public class NamedParser<R> : IParser<R>
    {
        public NamedParser(string name)
        {
            Name = name;
            Parser = new SetOnce<IParser<R>>();
        }

        public SetOnce<IParser<R>> Parser { get; }

        public void Is(IParser<R> parser)
        {
            Parser.Value = parser;
        }

        public IParserResult<R> Parse(ParserInput input) =>
            Parser.Value.Parse(input);

        public string Name { get; }
        public string Rule => Parser.Value.ToString();
        public RuleCombining Combining => RuleCombining.NONE;
        public override string ToString() => Name;
    }

    public class OptionalParser<R> : IParser<Option<R>>
    {
        public OptionalParser(IParser<R> parser)
        {
            Parser = parser;
        }
        public IParser<R> Parser { get; }
        public RuleCombining Combining => RuleCombining.NONE;
        public IParserResult<Option<R>> Parse(ParserInput input)
        {
            var result = Parser.Parse(input);
            if (result.IsSuccessful)
            {
                return ParserSucceeded.Create(new Some<R>(result.Result), result.Environment, result.Source);
            }
            return ParserSucceeded.Create(new None<R>(), input.Environment, input.Source);
        }
    }

    public class OptionalParserWithDefault<R> : IParser<R>
    {
        public OptionalParserWithDefault(IParser<R> parser, R defaultValue)
        {
            Parser = parser;
            DefaultValue = defaultValue;
        }
        public IParser<R> Parser { get; }
        public R DefaultValue { get; }
        public RuleCombining Combining => RuleCombining.NONE;
        public IParserResult<R> Parse(ParserInput input)
        {
            var result = Parser.Parse(input);
            if (result.IsSuccessful)
            {
                return result;
            }
            return ParserSucceeded.Create(DefaultValue, input.Environment, input.Source);
        }
    }

    public class AlwaysSucceedingParser<R> : IParser<R>
    {
        public AlwaysSucceedingParser(R result)
        {
            Result = result;
        }
        public RuleCombining Combining => RuleCombining.NONE;
        public R Result { get; }
        public IParserResult<R> Parse(ParserInput input) =>
            ParserSucceeded.Create(Result, input.Environment, input.Source);
    }

    public class ParserThenCheck<R> : IParser<R>
    {
        public ParserThenCheck(IParser<R> parser, Predicate<IParserResult<R>> predicate)
        {
            Parser = parser;
            Predicate = predicate;
        }
        public IParser<R> Parser { get; }
        public RuleCombining Combining => RuleCombining.NONE;
        public Predicate<IParserResult<R>> Predicate { get; }
        public IParserResult<R> Parse(ParserInput input)
        {
            var result1 = Parser.Parse(input);
            if (result1.IsSuccessful && !Predicate(result1))
            {
                return new ParserFailed<R>();
            }
            return result1;
        }
    }

    public class OrParser<R> : IParser<R>
    {
        public OrParser(ImmutableList<IParser<R>> parsers)
        {
            Parsers = parsers;
        }
        public ImmutableList<IParser<R>> Parsers { get; }
        public RuleCombining Combining => RuleCombining.OR;
        public IParserResult<R> Parse(ParserInput input)
        {
            foreach (var parser in Parsers)
            {
                var result = parser.Parse(input);
                if (result.IsSuccessful)
                {
                    return result;
                }
            }
            return new ParserFailed<R>();
        }
    }

    //public class ParserOrParser<R> : IParser<R> {
    //    public ParserOrParser(IParser<R> firstParser, IParser<R> secondParser) {
    //        FirstParser = firstParser;
    //        SecondParser = secondParser;
    //    }
    //    public IParser<R> FirstParser { get; }
    //    public IParser<R> SecondParser { get; }
    //    public RuleCombining Combining => RuleCombining.OR;
    //    public IParserResult<R> Parse(ParserInput input) {
    //        var result1 = FirstParser.Parse(input);
    //        if (result1.IsSuccessful) {
    //            return result1;
    //        }
    //        return SecondParser.Parse(input);
    //    }
    //}

    public class ZeroOrMoreParser<R> : IParser<ImmutableList<R>>
    {
        public ZeroOrMoreParser(IParser<R> parser)
        {
            Parser = parser;
        }

        public RuleCombining Combining => RuleCombining.NONE;

        public IParser<R> Parser { get; }

        public IParserResult<ImmutableList<R>> Parse(ParserInput input)
        {
            var list = ImmutableList<R>.Empty;
            IParserResult<R> curResult;
            while ((curResult = Parser.Parse(input)).IsSuccessful)
            {
                list = list.Add(curResult.Result);
                input = curResult.ToInput();
            }
            return ParserSucceeded.Create(list, input.Environment, input.Source);
        }
    }

    public class OneOrMoreParser<R> : IParser<ImmutableList<R>>
    {
        public OneOrMoreParser(IParser<R> parser)
        {
            Parser = parser;
        }

        public RuleCombining Combining => RuleCombining.NONE;
        public IParser<R> Parser { get; }

        public IParserResult<ImmutableList<R>> Parse(ParserInput input)
        {
            var list = ImmutableList<R>.Empty;
            var curResult = Parser.Parse(input);
            if (!curResult.IsSuccessful)
            {
                return new ParserFailed<ImmutableList<R>>();
            }

            IParserResult<R> lastSuccessfulResult;
            do
            {
                list = list.Add(curResult.Result);
                lastSuccessfulResult = curResult;
                curResult = Parser.Parse(lastSuccessfulResult.ToInput());
            } while (curResult.IsSuccessful);

            return ParserSucceeded.Create(list, lastSuccessfulResult.Environment, lastSuccessfulResult.Source);
        }
    }

    public class OneOrMoreParserWithSeparator<R> : IParser<ImmutableList<R>>
    {
        public OneOrMoreParserWithSeparator(IConsumer separatorConsumer, IParser<R> elementParser)
        {
            SeparatorConsumer = separatorConsumer;
            ElementParser = elementParser;
        }
        public RuleCombining Combining => RuleCombining.NONE;
        public IConsumer SeparatorConsumer { get; }
        public IParser<R> ElementParser { get; }

        public IParserResult<ImmutableList<R>> Parse(ParserInput input)
        {
            var list = ImmutableList<R>.Empty;
            var curResult = ElementParser.Parse(input);
            if (!curResult.IsSuccessful)
            {
                return new ParserFailed<ImmutableList<R>>();
            }
            IParserResult<R> lastElementResult;

            do
            {
                list = list.Add(curResult.Result);
                lastElementResult = curResult;

                var separatorResult = SeparatorConsumer.Consume(curResult.ToInput());
                if (!separatorResult.IsSuccessful)
                {
                    break;
                }
                curResult = ElementParser.Parse(separatorResult.ToInput());
            } while (curResult.IsSuccessful);

            return ParserSucceeded.Create(list, lastElementResult.Environment, lastElementResult.Source);
        }
    }

    /// <summary>
    /// A consumer consumes one or several tokens, and doesn't produce any result.
    /// </summary>
    public interface IConsumer
    {
        IParserResult Consume(ParserInput input);
    }

    public class NamedConsumer : IConsumer
    {
        public NamedConsumer()
        {
            Consumer = new SetOnce<IConsumer>();
        }

        public SetOnce<IConsumer> Consumer { get; }

        public void Is(IConsumer consumer)
        {
            Consumer.Value = consumer;
        }

        public IParserResult Consume(ParserInput input) =>
            Consumer.Value.Consume(input);

        public override string ToString()
        {
            if (Consumer.IsSet)
            {
                return Consumer.Value.ToString();
            }
            return "<Unset Consumer>";
        }
    }

    public class OptionalConsumer : IParser<Boolean>
    {
        public OptionalConsumer(IConsumer consumer)
        {
            Consumer = consumer;
        }

        public RuleCombining Combining => RuleCombining.NONE;

        public IConsumer Consumer { get; }

        public IParserResult<Boolean> Parse(ParserInput input)
        {
            var result = Consumer.Consume(input);
            if (result.IsSuccessful)
            {
                return ParserSucceeded.Create(true, result.Environment, result.Source);
            }
            return ParserSucceeded.Create(false, input.Environment, input.Source);
        }
    }

    public class OrConsumer : IConsumer
    {
        public OrConsumer(ImmutableList<IConsumer> consumers)
        {
            Consumers = consumers;
        }

        public ImmutableList<IConsumer> Consumers { get; }

        public IParserResult Consume(ParserInput input)
        {
            foreach (var consumer in Consumers)
            {
                var result = consumer.Consume(input);
                if (result.IsSuccessful)
                {
                    return result;
                }
            }
            return new ParserFailed();
        }
    }

    //public class ConsumerOrConsumer : IConsumer {
    //    public ConsumerOrConsumer(IConsumer firstConsumer, IConsumer secondConsumer) {
    //        FirstConsumer = firstConsumer;
    //        SecondConsumer = secondConsumer;
    //    }

    //    public IConsumer FirstConsumer { get; }
    //    public IConsumer SecondConsumer { get; }

    //    public IParserResult Consume(ParserInput input) {
    //        var result1 = FirstConsumer.Consume(input);
    //        if (!result1.IsSuccessful) {
    //            return new ParserFailed();
    //        }
    //        return SecondConsumer.Consume(result1.ToInput());
    //    }
    //}

    public class EnvironmentTransformer : IConsumer
    {
        public EnvironmentTransformer(Func<ParserEnvironment, ParserEnvironment> transformer)
        {
            Transformer = transformer;
        }

        public Func<ParserEnvironment, ParserEnvironment> Transformer { get; }

        public IParserResult Consume(ParserInput input)
        {
            return ParserSucceeded.Create(Transformer(input.Environment), input.Source);
        }
    }

    /// <summary>
    /// A transformer consumes zero or more tokens, and takes a previous result to produce a new result.
    /// </summary>
    public interface ITransformer<in S, out R>
    {
        IParserResult<R> Transform(S seed, ParserInput input);
    }

    public class IdentityTransformer<R> : ITransformer<R, R>
    {
        public IParserResult<R> Transform(R seed, ParserInput input) =>
            ParserSucceeded.Create(seed, input.Environment, input.Source);
    }

    public class SimpleTransformer<S, R> : ITransformer<S, R>
    {
        public SimpleTransformer(Func<S, R> transformFunc)
        {
            TransformFunc = transformFunc;
        }
        public Func<S, R> TransformFunc { get; }
        public IParserResult<R> Transform(S seed, ParserInput input) =>
            ParserSucceeded.Create(TransformFunc(seed), input.Environment, input.Source);
    }

    public class NamedTransformer<S, R> : ITransformer<S, R>
    {
        public NamedTransformer()
        {
            Transformer = new SetOnce<ITransformer<S, R>>();
        }

        public SetOnce<ITransformer<S, R>> Transformer { get; }

        public void Is(ITransformer<S, R> transformer)
        {
            Transformer.Value = transformer;
        }

        public IParserResult<R> Transform(S seed, ParserInput input) =>
            Transformer.Value.Transform(seed, input);

        public override string ToString()
        {
            if (Transformer.IsSet)
            {
                return Transformer.Value.ToString();
            }
            return "<Unset transformer>";
        }
    }

    public class OptionalTransformer<R> : ITransformer<R, R>
    {
        public OptionalTransformer(ITransformer<R, R> transformer)
        {
            Transformer = transformer;
        }
        public ITransformer<R, R> Transformer { get; }
        public IParserResult<R> Transform(R seed, ParserInput input)
        {
            var result = Transformer.Transform(seed, input);
            if (result.IsSuccessful)
            {
                return result;
            }
            return ParserSucceeded.Create(seed, input.Environment, input.Source);
        }
    }

    public class OrTransformer<S, R> : ITransformer<S, R>
    {
        public OrTransformer(ImmutableList<ITransformer<S, R>> transformers)
        {
            Transformers = transformers;
        }

        public ImmutableList<ITransformer<S, R>> Transformers { get; }

        public IParserResult<R> Transform(S seed, ParserInput input)
        {
            foreach (var transformer in Transformers)
            {
                var result = transformer.Transform(seed, input);
                if (result.IsSuccessful)
                {
                    return result;
                }
            }
            return new ParserFailed<R>();
        }
    }

    //public class TransformerOrTransformer<S, R> : ITransformer<S, R> {
    //    public TransformerOrTransformer(ITransformer<S, R> firstTransformer, ITransformer<S, R> secondTransformer) {
    //        FirstTransformer = firstTransformer;
    //        SecondTransformer = secondTransformer;
    //    }

    //    public ITransformer<S, R> FirstTransformer { get; }
    //    public ITransformer<S, R> SecondTransformer { get; }

    //    public IParserResult<R> Transform(S seed, ParserInput input) {
    //        var result1 = FirstTransformer.Transform(seed, input);
    //        if (result1.IsSuccessful) {
    //            return result1;
    //        }
    //        return SecondTransformer.Transform(seed, input);

    //    }

    //    public override string ToString() {
    //        return FirstTransformer + " | " + SecondTransformer;
    //    }
    //}

    public class ResultTransformer<R> : ITransformer<R, R>
    {
        public ResultTransformer(Func<IParserResult<R>, IParserResult<R>> transformFunc)
        {
            TransformFunc = transformFunc;
        }

        public Func<IParserResult<R>, IParserResult<R>> TransformFunc { get; }

        public IParserResult<R> Transform(R seed, ParserInput input) =>
            TransformFunc(ParserSucceeded.Create(seed, input.Environment, input.Source));
    }

    public class ZeroOrMoreTransformer<R> : ITransformer<R, R>
    {
        public ZeroOrMoreTransformer(ITransformer<R, R> transformer)
        {
            Transformer = transformer;
        }
        public ITransformer<R, R> Transformer { get; }
        public IParserResult<R> Transform(R seed, ParserInput input)
        {
            IParserResult<R> curResult = ParserSucceeded.Create(seed, input.Environment, input.Source);

            IParserResult<R> lastSuccessfulResult;
            do
            {
                lastSuccessfulResult = curResult;
                curResult = Transformer.Transform(lastSuccessfulResult.Result, lastSuccessfulResult.ToInput());
            } while (curResult.IsSuccessful);

            return lastSuccessfulResult;
        }
    }

    public class OneOrMoreTransformer<R> : ITransformer<R, R>
    {
        public OneOrMoreTransformer(ITransformer<R, R> transformer)
        {
            Transformer = transformer;
        }
        public ITransformer<R, R> Transformer { get; }
        public IParserResult<R> Transform(R seed, ParserInput input)
        {
            var curResult = Transformer.Transform(seed, input);
            if (!curResult.IsSuccessful)
            {
                return new ParserFailed<R>();
            }

            IParserResult<R> lastSuccessfulResult;
            do
            {
                lastSuccessfulResult = curResult;
                curResult = Transformer.Transform(lastSuccessfulResult.Result, lastSuccessfulResult.ToInput());
            } while (curResult.IsSuccessful);

            return lastSuccessfulResult;
        }
    }

    public class OperatorConsumer : IConsumer
    {
        public OperatorConsumer(OperatorVal operatorVal)
        {
            OperatorVal = operatorVal;
        }

        public static IConsumer Create(OperatorVal operatorVal) =>
            new OperatorConsumer(operatorVal);

        public OperatorVal OperatorVal { get; }

        public IParserResult Consume(ParserInput input)
        {
            if ((input.Source.First() as TokenOperator)?.Val == OperatorVal)
            {
                return ParserSucceeded.Create(input.Environment, input.Source.Skip(1));
            }
            return new ParserFailed();
        }
    }

    public class IdentifierParser : IParser<string>
    {
        public RuleCombining Combining => RuleCombining.NONE;
        public IParserResult<string> Parse(ParserInput input)
        {
            var token = input.Source.First() as TokenIdentifier;
            if (token == null)
            {
                return new ParserFailed<string>();
            }
            return ParserSucceeded.Create(token.Val, input.Environment, input.Source.Skip(1));
        }
    }

    public class KeywordConsumer : IConsumer
    {
        public KeywordConsumer(KeywordVal keywordVal)
        {
            KeywordVal = keywordVal;
        }
        public KeywordVal KeywordVal { get; }
        public static KeywordConsumer Create(KeywordVal keywordVal) =>
            new KeywordConsumer(keywordVal);
        public IParserResult Consume(ParserInput input)
        {
            if ((input.Source.First() as TokenKeyword)?.Val == KeywordVal)
            {
                return ParserSucceeded.Create(input.Environment, input.Source.Skip(1));
            }
            return new ParserFailed();
        }
    }

    public class KeywordParser<R> : IParser<R>
    {
        public KeywordParser(KeywordVal keywordVal, R result)
        {
            KeywordVal = keywordVal;
            Result = result;
        }

        public RuleCombining Combining => RuleCombining.NONE;

        public KeywordVal KeywordVal { get; }
        public R Result { get; }

        public IParserResult<R> Parse(ParserInput input)
        {
            if ((input.Source.First() as TokenKeyword)?.Val == KeywordVal)
            {
                return ParserSucceeded.Create(Result, input.Environment, input.Source.Skip(1));
            }
            return new ParserFailed<R>();
        }
    }

    public class KeywordParser
    {
        public static KeywordParser<R> Create<R>(KeywordVal keywordVal, R result) =>
            new KeywordParser<R>(keywordVal, result);
    }

}