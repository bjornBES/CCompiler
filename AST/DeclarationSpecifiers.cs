﻿using System;
using System.Collections.Immutable;
using System.Linq;
using CCompiler.ABT;

namespace CCompiler.AST {
    using static SemanticAnalysis;

    /// <summary>
    /// storage-class-specifier
    ///   : auto | register | static | extern | typedef
    /// </summary>
    public enum StorageClsSpec {
        NULL,
        AUTO,
        REGISTER,
        STATIC,
        EXTERN,
        TYPEDEF
    }

    /// <summary>
    /// Type-specifier
    ///   : void      --+
    ///   | char        |
    ///   | short       |
    ///   | int         |
    ///   | long        +--> Basic Type specifier
    ///   | float       |
    ///   | double      |
    ///   | signed      |
    ///   | unsigned  --+
    ///   | struct-or-union-specifier
    ///   | enum-specifier
    ///   | typedef-name
    /// </summary>
    public abstract class TypeSpec : ISyntaxTreeNode {
        [SemantMethod]
        public abstract ISemantReturn<ExprType> GetExprType(Env env);

        public abstract TypeSpecKind Kind { get; }
    }

    public enum TypeSpecKind {
        NON_BASIC,
        VOID,
        CHAR,
        SHORT,
        INT,
        LONG,
        FLOAT,
        DOUBLE,
        SIGNED,
        UNSIGNED
    }

    public sealed class BasicTypeSpec : TypeSpec {
        public BasicTypeSpec(TypeSpecKind kind) {
            this.Kind = kind;
        }

        public override TypeSpecKind Kind { get; }
        
        [SemantMethod]
        public override ISemantReturn<ExprType> GetExprType(Env env) {
            throw new InvalidProgramException();
        }
    }

    public abstract class NonBasicTypeSpec : TypeSpec {
        public override TypeSpecKind Kind => TypeSpecKind.NON_BASIC;
    }

    /// <summary>
    /// typedef-name
    ///   : identifier
    /// </summary>
    public sealed class TypedefName : NonBasicTypeSpec {
        private TypedefName(string name) {
            this.Name = name;
        }

        public static TypedefName Create(string name) =>
            new TypedefName(name);
        
        [SemantMethod]
        public override ISemantReturn<ExprType> GetExprType(Env env) {
            var entryOpt = env.Find(this.Name);
            if (entryOpt.IsNone) {
                throw new InvalidProgramException("This should not pass the parser.");
            }
            var entry = entryOpt.Value;
            if (entry.Kind != Env.EntryKind.TYPEDEF) {
                throw new InvalidProgramException("This should not pass the parser.");
            }
            return SemantReturn.Create(env, entry.Type);
        }

        public string Name { get; }
    }

    /// <summary>
    /// Type-qualifier
    ///   : const | volatile
    /// </summary>
    public enum TypeQual {
        NULL,
        CONST,
        VOLATILE
    }

    /// <summary>
    /// specifier-qualifier-list
    ///   : [ Type-specifier | Type-qualifier ]+
    /// </summary>
    public class SpecQualList : ISyntaxTreeNode {
        protected SpecQualList(ImmutableList<TypeSpec> typeSpecs, ImmutableList<TypeQual> typeQuals) {
            this.TypeSpecs = typeSpecs;
            this.TypeQuals = typeQuals;
        }

        public static SpecQualList Create(ImmutableList<TypeSpec> typeSpecs, ImmutableList<TypeQual> typeQuals) =>
            new SpecQualList(typeSpecs, typeQuals);

        public static SpecQualList Empty { get; } =
            Create(ImmutableList<TypeSpec>.Empty, ImmutableList<TypeQual>.Empty);

        public static SpecQualList Add(SpecQualList list, TypeSpec typeSpec) =>
            Create(list.TypeSpecs.Add(typeSpec), list.TypeQuals);

        public static SpecQualList Add(SpecQualList list, TypeQual typeQual) =>
            Create(list.TypeSpecs, list.TypeQuals.Add(typeQual));

        public ImmutableList<TypeSpec> TypeSpecs { get; }
        public ImmutableList<TypeQual> TypeQuals { get; }

        private static ImmutableDictionary<ImmutableSortedSet<TypeSpecKind>, ExprType> BasicTypeSpecLookupTable { get; }

        static SpecQualList() {

            BasicTypeSpecLookupTable = ImmutableDictionary<ImmutableSortedSet<TypeSpecKind>, ExprType>.Empty
                
            .Add(ImmutableSortedSet.Create(TypeSpecKind.VOID), new VoidType())

            .Add(ImmutableSortedSet.Create(TypeSpecKind.CHAR), new CharType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.CHAR, TypeSpecKind.SIGNED), new CharType())

            .Add(ImmutableSortedSet.Create(TypeSpecKind.CHAR, TypeSpecKind.UNSIGNED), new UCharType())

            .Add(ImmutableSortedSet.Create(TypeSpecKind.SHORT), new ShortType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.SHORT, TypeSpecKind.SIGNED), new ShortType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.SHORT, TypeSpecKind.INT), new ShortType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.SHORT, TypeSpecKind.INT, TypeSpecKind.SIGNED), new ShortType())

            .Add(ImmutableSortedSet.Create(TypeSpecKind.SHORT, TypeSpecKind.UNSIGNED), new UShortType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.SHORT, TypeSpecKind.INT, TypeSpecKind.UNSIGNED), new UShortType())

            .Add(ImmutableSortedSet.Create(TypeSpecKind.INT), new LongType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.INT, TypeSpecKind.SIGNED), new LongType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.INT, TypeSpecKind.LONG), new LongType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.INT, TypeSpecKind.SIGNED, TypeSpecKind.LONG), new LongType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.SIGNED), new LongType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.SIGNED, TypeSpecKind.LONG), new LongType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.LONG), new LongType())

            .Add(ImmutableSortedSet.Create(TypeSpecKind.UNSIGNED), new ULongType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.UNSIGNED, TypeSpecKind.INT), new ULongType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.UNSIGNED, TypeSpecKind.LONG), new ULongType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.UNSIGNED, TypeSpecKind.INT, TypeSpecKind.LONG), new ULongType())

            .Add(ImmutableSortedSet.Create(TypeSpecKind.FLOAT), new FloatType())

            .Add(ImmutableSortedSet.Create(TypeSpecKind.DOUBLE), new DoubleType())
            .Add(ImmutableSortedSet.Create(TypeSpecKind.DOUBLE, TypeSpecKind.LONG), new DoubleType())
            ;
        }

        /// <summary>
        /// Get qualified Type, based on Type specifiers & Type qualifiers.
        /// </summary>
        [SemantMethod]
        public ISemantReturn<ExprType> GetExprType(Env env) {
            Boolean isConst = this.TypeQuals.Contains(TypeQual.CONST);
            Boolean isVolatile = this.TypeQuals.Contains(TypeQual.VOLATILE);

            // If no Type specifier is given, assume long Type.
            if (this.TypeSpecs.IsEmpty) {
                return SemantReturn.Create(env, new LongType(isConst, isVolatile));
            }

            // If every Type specifier is basic, go to the lookup table.
            if (this.TypeSpecs.All(typeSpec => typeSpec.Kind != TypeSpecKind.NON_BASIC)) {
                var basicTypeSpecKinds =
                    this.TypeSpecs
                    .ConvertAll(typeSpec => typeSpec.Kind)
                    .Distinct()
                    .ToImmutableSortedSet();

                foreach (var pair in BasicTypeSpecLookupTable) {
                    if (pair.Key.SetEquals(basicTypeSpecKinds)) {
                        return SemantReturn.Create(env, pair.Value);
                    }
                }

                throw new InvalidOperationException("Invalid Type specifier set.");
            }

            // If there is a non-basic Type specifier, semant it.
            if (this.TypeSpecs.Count != 1) {
                throw new InvalidOperationException("Invalid Type specifier set.");
            }

            var type = Semant(this.TypeSpecs[0].GetExprType, ref env);
            return SemantReturn.Create(env, type.GetQualifiedType(isConst, isVolatile));
        }
    }

    /// <summary>
    /// declaration-specifiers
    ///   : [ storage-class-specifier | Type-specifier | Type-qualifier ]+
    /// </summary>
    public sealed class DeclnSpecs : SpecQualList {
        private DeclnSpecs(ImmutableList<StorageClsSpec> storageClsSpecs, ImmutableList<TypeSpec> typeSpecs, ImmutableList<TypeQual> typeQuals)
            : base(typeSpecs, typeQuals) {
            this.StorageClsSpecs = storageClsSpecs;
        }

        public static DeclnSpecs Create(ImmutableList<StorageClsSpec> storageClsSpecs, ImmutableList<TypeSpec> typeSpecs, ImmutableList<TypeQual> typeQuals) =>
            new DeclnSpecs(storageClsSpecs, typeSpecs, typeQuals);

        public new static DeclnSpecs Empty { get; } = Create(ImmutableList<StorageClsSpec>.Empty, ImmutableList<TypeSpec>.Empty, ImmutableList<TypeQual>.Empty);

        public static DeclnSpecs Add(DeclnSpecs declnSpecs, StorageClsSpec storageClsSpec) =>
            Create(declnSpecs.StorageClsSpecs.Add(storageClsSpec), declnSpecs.TypeSpecs, declnSpecs.TypeQuals);

        public static DeclnSpecs Add(DeclnSpecs declnSpecs, TypeSpec typeSpec) =>
            Create(declnSpecs.StorageClsSpecs, declnSpecs.TypeSpecs.Add(typeSpec), declnSpecs.TypeQuals);

        public static DeclnSpecs Add(DeclnSpecs declnSpecs, TypeQual typeQual) =>
            Create(declnSpecs.StorageClsSpecs, declnSpecs.TypeSpecs, declnSpecs.TypeQuals.Add(typeQual));

        [SemantMethod]
        public StorageClass GetStorageClass() {
            if (this.StorageClsSpecs.Count == 0) {
                return StorageClass.AUTO;
            }

            if (this.StorageClsSpecs.Count == 1) {
                switch (this.StorageClsSpecs[0]) {
                    case StorageClsSpec.AUTO:
                    case StorageClsSpec.NULL:
                    case StorageClsSpec.REGISTER:
                        return StorageClass.AUTO;

                    case StorageClsSpec.EXTERN:
                        return StorageClass.EXTERN;

                    case StorageClsSpec.STATIC:
                        return StorageClass.STATIC;

                    case StorageClsSpec.TYPEDEF:
                        return StorageClass.TYPEDEF;

                    default:
                        throw new InvalidOperationException();
                }
            }

            throw new InvalidOperationException("Multiple storage class specifiers.");
        }

        public ImmutableList<StorageClsSpec> StorageClsSpecs { get; }

        /// <summary>
        /// Only used by the parser.
        /// </summary>
        [Obsolete]
        public bool IsTypedef() => this.StorageClsSpecs.Contains(StorageClsSpec.TYPEDEF);
    }

    /// <summary>
    /// struct-or-union
    ///   : struct | union
    /// </summary>
    public enum StructOrUnion {
        STRUCT,
        UNION
    }

    /// <summary>
    /// struct-or-union-specifier
    /// </summary>
    public sealed class StructOrUnionSpec : NonBasicTypeSpec {
        private StructOrUnionSpec(StructOrUnion structOrUnion, Option<string> name, Option<ImmutableList<StructDecln>> memberDeclns) {
            this.StructOrUnion = structOrUnion;
            this.Name = name;
            this.MemberDeclns = memberDeclns;
        }

        [Obsolete]
        public static StructOrUnionSpec Create(StructOrUnion structOrUnion, Option<string> name, Option<ImmutableList<StructDecln>> memberDeclns) =>
            new StructOrUnionSpec(structOrUnion, name, memberDeclns);

        public static StructOrUnionSpec Create(StructOrUnion structOrUnion, Option<string> name, ImmutableList<StructDecln> memberDeclns) =>
            new StructOrUnionSpec(structOrUnion, name, Option.Some(memberDeclns));

        public static StructOrUnionSpec Create(StructOrUnion structOrUnion, string name) =>
            new StructOrUnionSpec(structOrUnion, Option.Some(name), Option<ImmutableList<StructDecln>>.None);

        public StructOrUnion StructOrUnion { get; }
        public Option<string> Name { get; }
        public Option<ImmutableList<StructDecln>> MemberDeclns { get; }

        [SemantMethod]
        public ISemantReturn<ImmutableList<Tuple<Option<string>, ExprType>>> GetMembers(Env env, ImmutableList<StructDecln> memberDeclns) {
            var result = memberDeclns.Aggregate(ImmutableList<Tuple<Option<string>, ExprType>>.Empty, (acc, decln) => acc.AddRange(Semant(decln.GetMemberDeclns, ref env))
            );

            return SemantReturn.Create(env, result);
        }

        //            +----------------------------------------------+-------------------------------------------+
        //            |                   members                    |                 members X                 |
        // +----------+----------------------------------------------+-------------------------------------------+
        // |          | May have incomplete Type in current scope.   |                                           |
        // |   name   | 1. Get/New incomplete Type in current scope; | Name must appear in previous environment. |
        // |          | 2. Fill up with members.                     |                                           |
        // +----------+----------------------------------------------+-------------------------------------------+
        // |  name X  | Fill up with members.                        |                     X                     |
        // +----------+----------------------------------------------+-------------------------------------------+
        [SemantMethod]
        public override ISemantReturn<ExprType> GetExprType(Env env) {

            StructOrUnionType type;

            // If no members provided, then we need to find the Type in the current environment.
            if (this.MemberDeclns.IsNone) {

                if (this.Name.IsNone) {
                    throw new InvalidProgramException("This should not pass the parser");
                }

                var name = this.Name.Value;
                var typeName = (this.StructOrUnion == StructOrUnion.STRUCT ? "struct" : "union") + $" {name}";

                // Try to find Type name in the current environment.
                var entryOpt = env.Find(typeName);

                // If name not found: create an incomplete Type and add it into the environment.
                if (entryOpt.IsNone) {
                    type = StructOrUnionType.CreateIncompleteType(this.StructOrUnion, name);
                    env = env.PushEntry(Env.EntryKind.TYPEDEF, typeName, type);
                    return SemantReturn.Create(env, type);
                }

                // If name found: fetch it.
                if (entryOpt.Value.Kind != Env.EntryKind.TYPEDEF) {
                    throw new InvalidProgramException("A struct or union in env that is not typedef? This should not appear.");
                }

                return SemantReturn.Create(env, entryOpt.Value.Type);

            }

            // If members are provided, the user is trying to define a new struct/union.

            if (this.Name.IsSome) {

                var name = this.Name.Value;
                var typeName = (this.StructOrUnion == StructOrUnion.STRUCT ? "struct" : "union") + $" {name}";

                // Try to find Type name in the current environment.
                // Notice we need to search the current **scope** only.
                var entryOpt = env.FindInCurrentScope(typeName);

                // If name not found: create an incomplete Type and add it into the environment.
                if (entryOpt.IsNone) {
                    type = StructOrUnionType.CreateIncompleteType(this.StructOrUnion, name);
                    env = env.PushEntry(Env.EntryKind.TYPEDEF, typeName, type);
                } else {
                    if (entryOpt.Value.Kind != Env.EntryKind.TYPEDEF) {
                        throw new InvalidProgramException(
                            "A struct or union in env that is not typedef? This should not appear.");
                    }

                    type = entryOpt.Value.Type as StructOrUnionType;
                    if (type == null) {
                        throw new InvalidProgramException(
                            $"{typeName} is not a struct or union? This should not appear.");
                    }
                }

                // Current Type mustn't be already complete.
                if (type.IsComplete) {
                    throw new InvalidOperationException($"Redifinition of {typeName}");
                }

            } else {
                var typeName = (this.StructOrUnion == StructOrUnion.STRUCT ? "struct" : "union") + " <unnamed>";
                type = StructOrUnionType.CreateIncompleteType(this.StructOrUnion, typeName);
            }

            var members = Semant(GetMembers, this.MemberDeclns.Value, ref env);
            type.Define(this.StructOrUnion, members);

            return SemantReturn.Create(env, type);
        }
    }

    /// <summary>
    /// enum-specifier
    ///   : enum [identifier]? '{' enumerator-list '}'
    ///   | enum identifier
    /// 
    /// enumerator-list
    ///   : enumerator [ ',' enumerator ]*
    /// </summary>
    public sealed class EnumSpec : NonBasicTypeSpec {
        private EnumSpec(Option<string> name, Option<ImmutableList<Enumr>> enumrs) {
            this.Name = name;
            this.Enumrs = enumrs;
        }

        private static EnumSpec Create(Option<string> name, Option<ImmutableList<Enumr>> enumrs) =>
            new EnumSpec(name, enumrs);

        public static EnumSpec Create(Option<string> name, ImmutableList<Enumr> enumrs) =>
            Create(name, Option.Some(enumrs));

        public static EnumSpec Create(string name) =>
            Create(Option.Some(name), Option<ImmutableList<Enumr>>.None);

        [SemantMethod]
        public override ISemantReturn<ExprType> GetExprType(Env env) {
            if (this.Enumrs.IsNone) {
                // If no enumerators provided: must find enum Type in the current environment.

                if (this.Name.IsNone) {
                    throw new InvalidProgramException("This should not pass the parser.");
                }

                var name = this.Name.Value;
                var entryOpt = env.Find($"enum {name}");

                if (entryOpt.IsNone || entryOpt.Value.Kind != Env.EntryKind.TYPEDEF) {
                    throw new InvalidOperationException($"enum {name} has not been defined.");
                }

                return SemantReturn.Create(env, new LongType());
            }

            // If enumerators are provided: add names to environment
            int offset = 0;
            foreach (var enumr in this.Enumrs.Value) {

                if (enumr.Init.IsSome) {
                    // If the user provides an initialization Value, use it.
                    var init = SemantExpr(enumr.Init.Value, ref env);
                    init = ABT.TypeCast.MakeCast(init, new LongType());
                    if (!init.IsConstExpr) {
                        throw new InvalidOperationException("Enumerator initialization must have a constant Value.");
                    }
                    offset = ((ConstLong)init).Value;
                }

                env = env.PushEnum(enumr.Name, new LongType(), offset);

                offset++;
            }

            // If the user provides a name to the enum, add it to the environment.
            if (this.Name.IsSome) {
                var typeName = $"enum {this.Name.Value}";

                if (env.FindInCurrentScope(typeName).IsSome) {
                    throw new InvalidOperationException($"{typeName} is already defined.");
                }
                env = env.PushEntry(Env.EntryKind.TYPEDEF, typeName, new LongType());
            }

            return SemantReturn.Create(env, new LongType());
        }

        public Option<string> Name { get; }
        public Option<ImmutableList<Enumr>> Enumrs { get; }
    }

    /// <summary>
    /// enumerator
    ///   : enumeration-constant [ '=' constant-expression ]?
    /// 
    /// enumeration-constant
    ///   : identifier
    /// </summary>
    public sealed class Enumr : ISyntaxTreeNode {
        private Enumr(string name, Option<Expr> init) {
            this.Name = name;
            this.Init = init;
        }

        public string Name { get; }
        public Option<Expr> Init { get; }

        public static Enumr Create(string name, Option<Expr> init) =>
            new Enumr(name, init);

        [Obsolete]
        public Tuple<Env, string, int> GetEnumerator(Env env, int idx) {
            if (this.Init.IsNone) {
                return new Tuple<Env, string, int>(env, this.Name, idx);
            }

            ABT.Expr init = this.Init.Value.GetExpr(env);

            init = ABT.TypeCast.MakeCast(init, new LongType());
            if (!init.IsConstExpr) {
                throw new InvalidOperationException("Error: expected constant integer");
            }
            int initIdx = ((ConstLong)init).Value;

            return new Tuple<Env, string, int>(env, this.Name, initIdx);
        }
    }

}