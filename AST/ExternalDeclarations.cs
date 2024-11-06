using CCompiler.ABT;
using System.Collections.Immutable;
using static CCompiler.AST.SemanticAnalysis;

namespace CCompiler.AST
{
    public interface ISyntaxTreeNode { }

    /// <summary>
    /// A translation unit consists of a list of external declarations - functions and objects.
    /// </summary>
    public sealed class TranslnUnit : ISyntaxTreeNode
    {
        private TranslnUnit(ImmutableList<IExternDecln> declns)
        {
            Declns = declns;
        }

        public static TranslnUnit Create(ImmutableList<IExternDecln> externDeclns) =>
            new TranslnUnit(externDeclns);

        [SemantMethod]
        public ISemantReturn<ABT.TranslnUnit> GetTranslnUnit()
        {
            var env = new Env();
            var externDeclns = Declns.Aggregate(ImmutableList<Tuple<Env, ExternDecln>>.Empty, (acc, externDecln) => acc.AddRange(Semant(externDecln.GetExternDecln, ref env))
            );
            return SemantReturn.Create(env, new ABT.TranslnUnit(externDeclns.ToList()));
        }

        public ImmutableList<IExternDecln> Declns { get; }
    }


    public interface IExternDecln : ISyntaxTreeNode
    {
        [SemantMethod]
        ISemantReturn<ImmutableList<Tuple<Env, ExternDecln>>> GetExternDecln(Env env);
    }

    /// <summary>
    /// A function definition gives the implementation.
    /// </summary>
    public sealed class FuncDef : IExternDecln
    {
        public FuncDef(DeclnSpecs specs, Declr declr, CompoundStmt stmt)
        {
            Specs = specs;
            Declr = declr;
            Stmt = stmt;
        }

        public static FuncDef Create(Option<DeclnSpecs> declnSpecs, Declr declr, Stmt body) =>
            new FuncDef(declnSpecs.IsSome ? declnSpecs.Value : DeclnSpecs.Empty, declr, body as CompoundStmt);

        public DeclnSpecs Specs { get; }
        public Declr Declr { get; }
        public CompoundStmt Stmt { get; }

        [SemantMethod]
        public ISemantReturn<ImmutableList<Tuple<Env, ExternDecln>>> GetExternDecln(Env env)
        {
            var storageClass = Specs.GetStorageClass();
            var baseType = Semant(Specs.GetExprType, ref env);
            var name = Declr.Name;
            var type = Semant(Declr.DecorateType, baseType, ref env);

            var funcType = type as FunctionType;
            if (funcType == null)
            {
                throw new InvalidOperationException("Expected a function Type.");
            }

            switch (storageClass)
            {
                case StorageClass.AUTO:
                case StorageClass.EXTERN:
                case StorageClass.STATIC:
                    env = env.PushEntry(Env.EntryKind.GLOBAL, name, type);
                    break;
                case StorageClass.TYPEDEF:
                default:
                    throw new InvalidOperationException("Invalid storage class specifier for function definition.");
            }

            env = env.InScope();
            env = env.SetCurrentFunction(funcType);
            var stmt = SemantStmt(Stmt.GetStmt, ref env);
            env = env.OutScope();

            return SemantReturn.Create(env, ImmutableList.Create(Tuple.Create(env, new ABT.FuncDef(name, storageClass, funcType, stmt) as ExternDecln)));
        }
    }

}