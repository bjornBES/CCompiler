using CCompiler.ABT;
using System.Runtime.Intrinsics.X86;

namespace CCompiler.CodeGeneration
{
    public enum Reg
    {
        A,
        B,
        C,
        D,

        HL,

        R1,
        R2,

        AX,
        CX,
        DX,
        BX,

        BP,
        SP,

        AL,
        BL,
        CL,
        DL,

        AF,
        BF,
        CF,
        DF,
    }

    public class CGenState
    {
        private enum Status
        {
            NONE,
            TEXT,
            DATA
        }

        public static Dictionary<Reg, string> reg_strs = new Dictionary<Reg, string>
        {
            [Reg.A] = "A",
            [Reg.C] = "C",
            [Reg.D] = "D",
            [Reg.B] = "B",
            
            [Reg.HL] = "HL",

            [Reg.AX] = "AX",
            [Reg.CX] = "CX",
            [Reg.DX] = "DX",
            [Reg.BX] = "BX",
            [Reg.BP] = "BP",
            [Reg.SP] = "SP",

            [Reg.R1] = "R1",
            [Reg.R2] = "R2",

            [Reg.AL] = "AL",
            [Reg.BL] = "BL",
            [Reg.CL] = "CL",
            [Reg.DL] = "DL",

            [Reg.AF] = "AF",
            [Reg.BF] = "BF",
            [Reg.CF] = "CF",
            [Reg.DF] = "DF",
        };

        public static string RegToString(Reg reg) => reg_strs[reg];

        public CGenState()
        {
            os = new StringWriterBES();
            rodata = new StringWriter();
            rodata.WriteLine("    .section TEXT");

            rodata_idx = 0;
            label_idx = 2;
            status = Status.NONE;
            label_packs = new Stack<LabelPack>();
            return_label = -1;
            os.ToString();
        }

        public void TEXT()
        {
            if (status != Status.TEXT)
            {
                os.WriteLine("    .section TEXT");
                status = Status.TEXT;
            }
        }

        public void DATA()
        {
            if (status != Status.DATA)
            {
                os.WriteLine("    .section DATA");
                status = Status.DATA;
            }
        }

        public void GLOBL(string name) => os.WriteLine($".global\t{name}");
        public void LOCAL(string name) => os.WriteLine($".local\t{name}");
        public void BYTE(int value) => os.WriteLine($".db\t{value}");
        public void ZERO(int size) => os.WriteLine($".res\t{size}");
        public void LONG(int value) => os.WriteLine($".dword\t{value}");

        public void MOVL(string dst, string src) => os.WriteLine($"\tmovd\t{dst},\t{src}");
        public void MOVL(Reg dst, string src) => MOVL(RegToString(dst), src);
        public void MOVL(string dst, int src) => MOVL(dst, $"${src}");
        public void MOVL(Reg dst, int src) => MOVL(RegToString(dst), $"{src}");
        public void MOVL(Reg dst, Reg src) => MOVL(RegToString(dst), RegToString(src));
        public void MOVL(string dst, Reg src) => MOVL(dst, RegToString(src));
        public void MOVL(Reg dst, int offset, Reg src) => MOVL($"[{RegToString(dst)} - {offset}]", RegToString(src));
        public void MOVL(Reg dst, Reg src, int offset) => MOVL($"[{RegToString(src)} - {offset}]", RegToString(dst));

        public void CMPL(string op1, string op2)
        {
            os.WriteLine($"\tcmp\t{op1},\t{op2}");
        }

        public void CMPL(Reg op1, Reg op2) => CMPL(RegToString(op1), RegToString(op2));

        public void CMPL(Reg op1, int op2) => CMPL(RegToString(op1), $"${op2}");

        public void PUSHL(string dst) => os.WriteLine($"\tpush\t{dst}");
        public void PUSHL(Reg dst) => PUSHL(RegToString(dst));
        public void POPL(string dst) => os.WriteLine($"\tpop\t{dst}");
        public void POPL(Reg dst) => POPL(RegToString(dst));

        public void CALL(string addr)
        {
            os.WriteLine($"\tcall\t[{addr}]");
        }

        public void ADDL(string src, string dst, string comment = "")
        {
            os.Write($"\tadd\t{src},\t{dst}");
            if (comment == "")
            {
                os.WriteLine();
            }
            else
            {
                COMMENT(comment);
            }
        }

        public void DEC(string dst) => os.WriteLine($"dec\t{dst}");
        public void DEC(Reg dst) => DEC(RegToString(dst));

        public void ADDL(Reg dst, int src, string comment = "") => ADDL(RegToString(dst), $"0x{Convert.ToString(src, 16)}", comment);
        public void ADDL(Reg dst, Reg src, string comment = "") => ADDL(RegToString(dst), RegToString(src), comment);

        public void SUBL(string src, string dst, string comment = "")
        {
            os.Write($"\tsub\t{src},\t{dst}");
            if (comment == "")
            {
                os.WriteLine();
            }
            else
            {
                COMMENT(comment);
            }
        }
        public void ANDL(string dst, string src) => os.WriteLine($"\tand\t{dst},\t{src}");
        public void ANDL(Reg dst, Reg src) => ANDL(RegToString(dst), RegToString(src));
        public void ANDL(Reg dst, int src) => ANDL(RegToString(dst), $"0x{Convert.ToString(src, 16)}");

        public void SUBL(Reg dst, int src, string comment = "") => SUBL(RegToString(dst), $"0x{Convert.ToString(src, 16)}", comment);
        public void SUBL(Reg dst, Reg src, string comment = "") => SUBL(RegToString(dst), RegToString(src), comment);

        public void MUL(string dst, string src) => os.WriteLine($"mul\t{dst},\t{src}");
        public void MUL(Reg dst, Reg src) => MUL(RegToString(dst), RegToString(src));

        public void DIVL(string dst, string src) => os.WriteLine($"div\t{dst},\t{src}");
        public void DIVL(Reg dst, Reg src) => DIVL(RegToString(dst), RegToString(src));

        public void IDIVL(string dst, string src) => os.WriteLine($"div\t{dst},\t{src}");
        public void IDIVL(Reg dst, Reg src) => IDIVL(RegToString(dst), RegToString(src));

        public void FADD(string dst, string src) => os.WriteLine($"FADD\t{dst},\t{src}");
        public void FADD(Reg dst, Reg src) => FADD(RegToString(dst), RegToString(src));
        public void FDIV(string dst, string src) => os.WriteLine($"FDIV\t{dst},\t{src}");
        public void FDIV(Reg dst, Reg src) => FDIV(RegToString(dst), RegToString(src));

        public void PUSHR()
        {
            os.WriteLine("\tPUSHR");
            StackSize += 20;
        }
        public void ENTER() => os.WriteLine($"\tenter");

        public void LEA(string dst, string addr) => os.WriteLine($"mov\t{dst},\t{addr}");
        public void LEA(Reg dst, string addr) => LEA(RegToString(dst), addr);
        public void LEA(string dst, string addr, int offset)
        {
            os.WriteLine($"mov\t{dst},\t{addr}");
            os.WriteLine($"add\t{dst},\t{offset}");
        }
        public void LEA(Reg dst, string addr, int offset) => LEA(RegToString(dst), addr, offset);
        public void LEA(Reg dst, Reg addr, int offset) => LEA(RegToString(dst), RegToString(addr), offset);

        public void COMMENT(string comment)
        {
            os.WriteLine("\t; " + comment);
        }
        public void NEWLINE()
        {
            os.WriteLine();
        }

        public void CGenConvertFloatToLong()
        {
            CGenExpandStack(4);
            // FISTL(0, Reg.SP);
            // MOVL(0, Reg.SP, Reg.AX);
            CGenShrinkStack(4);
        }
        public void CGenConvertLongToFloat()
        {
            CGenExpandStack(4);
            // MOV(Reg.AX, 0, Reg.SP);
            // FILDL(0, Reg.SP);
            CGenShrinkStack(4);
        }

        public void CGenForceStackSizeTo(int nbytes)
        {
            StackSize = nbytes;
            //LEA(-nbytes, Reg.BP, Reg.SP);
        }

        public void CGenExpandStackWithAlignment(int nbytes, int align)
        {
            nbytes = Utils.RoundUp(StackSize + nbytes, align) - StackSize;
            CGenExpandStack(nbytes);
        }

        public void CGenExpandStack(int nbytes, string comment = "")
        {
            StackSize += nbytes;
            ADDL(Reg.SP, nbytes);
        }
        public void CGenShrinkStack(int nbytes, string comment = "")
        {
            StackSize -= nbytes;
            SUBL(Reg.SP, nbytes);
        }

        public void CGenFuncStart(string name)
        {
            os.WriteLine(name + ":");
            ENTER();
            PUSHR();
            
            StackSize = 0;
        }

        public string CGenLongConst(int val)
        {
            string name = ".LC" + rodata_idx;
            rodata.WriteLine(name + ":");
            rodata.WriteLine("    .dd " + val);
            rodata_idx++;
            return name;
        }
        public string CGenLongLongConst(int lo, int hi)
        {
            string name = ".LC" + rodata_idx;
            rodata.WriteLine(name + ":");
            rodata.WriteLine("    .dd " + lo);
            rodata.WriteLine("    .dd " + hi);
            rodata_idx++;
            return name;
        }

        public string CGenstring(string str)
        {
            string name = ".LC" + rodata_idx;
            rodata.WriteLine(name + ":");
            rodata.WriteLine("    .db \"" + str + "\", 0");
            rodata_idx++;
            return name;
        }

        /// <summary>
        /// Fast Memory Copy using assembly.
        /// Make sure that
        /// 1) R1 = source address
        /// 2) HL = destination address
        /// 3) CX = number of bytes
        /// </summary>
        public void CGenMemCpy()
        {
        }

        /// <summary>
        /// Fast Memory Copy using assembly.
        /// Make sure that
        /// 1) R1 = source address
        /// 2) HL = destination address
        /// 3) CX = number of bytes
        /// </summary>
        public void CGenMemCpyReversed()
        {
        }

        public void CGenPopLong(int saved_size, Reg dst)
        {
            if (StackSize == saved_size)
            {
                POPL(dst);
                StackSize -= 4;
            }
            else
            {
                MOVL(dst, Reg.BP, saved_size);
            }
        }

        public int CGenPushLong(Reg src)
        {
            PUSHL(src);
            StackSize += 4;
            return StackSize;
        }

        public int CGenPushLong(int imm)
        {
            MOVL(Reg.R1, imm);
            PUSHL(Reg.R1);
            StackSize += 4;
            return StackSize;
        }

        /*
        /// <summary>
        /// FCHS: %st(0) = -%st(0)
        /// </summary>
        public void FCHS() => os.WriteLine("    fchs");

        /// <summary>
        /// FLDS: load float to FPU stack.
        /// </summary>
        public void FLDS(string src) => os.WriteLine($"    flds {src}");

        public void FLDS(int imm, Reg src) => FLDS($"{imm}({RegToString(src)})");

        /// <summary>
        /// FLDL: load double to FPU stack.
        /// </summary>
        /// <param name="addr">Address.</param>
        public void FLDL(string addr) => os.WriteLine($"    fldl {addr}");

        public void FLDL(int imm, Reg from) => FLDL($"{imm}({RegToString(from)})");

        /// <summary>
        /// FLD1: push 1.0 to FPU stack.
        /// </summary>
        public void FLD1() => os.WriteLine("    fld1");

        /// <summary>
        /// FLD0: push 0.0 to FPU stack.
        /// </summary>
        public void FLDZ() => os.WriteLine("    fldz");

        /// <summary>
        /// FSTS: store float from FPU stack.
        /// </summary>
        /// <param name="addr"></param>
        public void FSTS(string addr) => os.WriteLine($"    fsts {addr}");

        public void FSTS(int imm, Reg to) => FSTS($"{imm}({RegToString(to)})");

        /// <summary>
        /// FSTPS: pop float from FPU stack, and store to {addr}.
        /// </summary>
        public void FSTPS(string addr) => os.WriteLine($"    fstps {addr}");

        public void FSTPS(int imm, Reg to) => FSTPS($"{imm}({RegToString(to)})");

        /// <summary>
        /// FSTL: store double from FPU stack.
        /// </summary>
        public void FSTL(string addr) => os.WriteLine($"    fstl {addr}");

        public void FSTL(int imm, Reg to) => FSTL($"{imm}({RegToString(to)})");

        /// <summary>
        /// FSTPL: pop from FPU and store *double*.
        /// </summary>
        public void FSTPL(string addr) => os.WriteLine($"    fstpl {addr}");

        public void FSTPL(int imm, Reg to) => FSTPL($"{imm}({RegToString(to)})");

        /// <summary>
        /// FSTP: copy %st(0) to dst, then pop %st(0).
        /// </summary>
        public void FSTP(string dst) => os.WriteLine($"    fstp {dst}");

        public void FSTP(Reg dst) => FSTP(RegToString(dst));

        /// <summary>
        /// FADD: calculate %st(op1) + %st(op2) and rewrite %st(op2).
        /// </summary>
        public void FADD(int op1, int op2) => os.WriteLine($"    fadd %st({op1}), %st({op2})");

        /// <summary>
        /// FADDP: pop operands from %st(0) and %st(1),
        ///        push addition result back to %st(0).
        /// </summary>
        public void FADDP() => os.WriteLine("    faddp");

        /// <summary>
        /// FADD: calculate %st(op1) + %st(op2) and rewrite %st(op2).
        /// </summary>
        public void FSUB(int op1, int op2) => os.WriteLine($"    fsub %st({op1}), %st({op2})");

        /// <summary>
        /// FSUBP: pop operands from %st(0) and %st(1),
        ///        push %st(0) / %st(1) back to %st(0).
        /// </summary>
        public void FSUBP() => os.WriteLine("    fsubp");

        /// <summary>
        /// FMULP: pop operands from %st(0) and %st(1), push multiplication result back to %st(0).
        /// </summary>
        public void FMULP() => os.WriteLine("    fmulp");

        /// <summary>
        /// FDIVP: pop operands from %st(0) and %st(1), push %st(0) / %st(1) back to %st(0).
        /// </summary>
        public void FDIVP() => os.WriteLine("    fdivp");

        public void PUSHR() => os.WriteLine($"\tpushr");
        /// <summary>
        /// PUSHL: push long into stack.
        /// </summary>
        /// <remarks>
        /// PUSHL changes the size of the stack, which should be tracked carefully.
        /// So, PUSHL is set private. Consider using <see cref="CGenPushLong"/>
        /// </remarks>
        private void PUSHL(string src) => os.WriteLine($"\tpush\t{src}");

        private void PUSHL(Reg src) => PUSHL(RegToString(src));

        private void PUSHL(int imm) => PUSHL($"${imm}");

        public void POPR() => os.WriteLine($"\tpushr");
        /// <summary>
        /// POPL: pop long from stack.
        /// </summary>
        /// <remarks>
        /// POPL changes the size of the stack, which should be tracked carefully.
        /// So, POPL is set private. Consider using <see cref="CGenPopLong"/>
        /// </remarks>
        private void POPL(string dst) => os.WriteLine($"\tpop\t{dst}");

        private void POPL(Reg dst) => POPL(RegToString(dst));

        /// <summary>
        /// MOV: move a 4-byte long
        /// </summary>
        public void MOVL(string dst, string src) => os.WriteLine($"\tmovd\t{dst},\t{src}");
        public void MOVL(Reg dst, string src) => MOV(RegToString(dst), src);
        public void MOVL(string dst, int src) => MOV(dst, $"${src}");
        public void MOVL(Reg dst, int src) => MOV(RegToString(dst), $"{src}");
        public void MOVL(Reg dst, Reg src) => MOV(RegToString(dst), RegToString(src));
        public void MOVL(string dst, Reg src) => MOV(dst, RegToString(src));
        public void MOVL(Reg src, Reg dst, int offset) => MOV($"[{RegToString(dst)} - {offset}]", RegToString(src));
        public void MOVL(int offset, Reg src, Reg dst) => MOV($"[{RegToString(src)} - {offset}]", RegToString(dst));

        /// <summary>
        /// MOVZBL: move a byte and zero-extend to a 4-byte long
        /// </summary>
        public void MOVZBL(string src, string dst) => os.WriteLine($"mov\t{src},\t{dst}");

        public void MOVZBL(string src, Reg dst) => MOVZBL(src, RegToString(dst));

        public void MOVZBL(int offset, Reg src, Reg dst) => MOVZBL($"{offset}({RegToString(src)})", RegToString(dst));

        public void MOVZBL(Reg src, Reg dst) => MOVZBL(RegToString(src), RegToString(dst));

        /// <summary>
        /// MOVSBL: move a byte and sign-extend to a 4-byte long
        /// </summary>
        public void MOVSBL(string src, string dst) => os.WriteLine($"\tmov\t{src},\t{dst}");

        public void MOVSBL(string src, Reg dst) => MOVSBL(src, RegToString(dst));

        public void MOVSBL(int offset, Reg src, Reg dst) => MOVSBL($"{offset}({RegToString(src)})", RegToString(dst));

        public void MOVSBL(Reg src, Reg dst) => MOVSBL(RegToString(src), RegToString(dst));

        /// <summary>
        /// MOVB: move a byte
        /// </summary>
        public void MOVB(string src, string dst) => os.WriteLine($"\tmov\t{src},\t{dst}");

        public void MOVB(Reg from, int imm, Reg to)
        {
            MOVB(RegToString(from), imm + "(" + RegToString(to) + ")");
        }

        public void MOVB(Reg from, Reg to) => MOVB(RegToString(from), RegToString(to));

        /// <summary>
        /// MOVW: move a 2-byte word
        /// </summary>
        public void MOVW(string from, string to)
        {
            os.WriteLine("\tmovw\tword" + from + ",\tword " + to);
        }

        public void MOVW(Reg from, int imm, Reg to)
        {
            MOVW(RegToString(from), imm + "(" + RegToString(to) + ")");
        }

        /// <summary>
        /// MOVZWL: move a 2-byte word and zero-extend to a 4-byte long
        /// </summary>
        public void MOVZWL(string from, string to)
        {
            os.WriteLine("\tmov\t" + from + ",\t" + to);
        }

        public void MOVZWL(string from, Reg to)
        {
            MOVZWL(from, RegToString(to));
        }

        public void MOVZWL(int offset, Reg from, Reg to)
        {
            MOVZWL(offset + RegToString(from), RegToString(to));
        }

        public void MOVZWL(Reg src, Reg dst) => MOVZWL(RegToString(src), RegToString(dst));

        /// <summary>
        /// MOVSWL: move a 2-byte word and sign-extend to a 4-byte long
        /// </summary>
        public void MOVSWL(string from, string to)
        {
            os.WriteLine("    movswl " + from + ", " + to);
        }

        public void MOVSWL(string from, Reg to)
        {
            MOVSWL(from, RegToString(to));
        }

        public void MOVSWL(int offset, Reg from, Reg to)
        {
            MOVSWL(offset + RegToString(from), RegToString(to));
        }

        public void MOVSWL(Reg src, Reg dst) => MOVSWL(RegToString(src), RegToString(dst));

        // LEA
        // ===
        // 
        public void LEA(string addr, string dst) => os.WriteLine($"    lea {addr}, {dst}");

        public void LEA(string addr, Reg dst) => LEA(addr, RegToString(dst));

        public void LEA(int offset, Reg src, Reg dst) => LEA($"[{RegToString(src)} {offset}]", RegToString(dst));

        // CALL
        // ====
        // 
        public void CALL(string addr)
        {
            os.WriteLine($"\tcall\t[{addr}]");
            StackSize += 3;
        }

        // CGenExpandStack
        // ===============
        // 
        public void CGenExpandStackTo(int size, string comment = "")
        {
            if (size > StackSize)
            {
                SUBL(RegToString(Reg.SP), size - StackSize, comment);
                StackSize = size;
            }
        }

        public void CGenExpandStackBy(int nbytes)
        {
            StackSize += nbytes;
            SUBL(Reg.SP, nbytes);
        }

        public void CGenExpandStackWithAlignment(int nbytes, int align)
        {
            nbytes = ABT.Utils.RoundUp(StackSize + nbytes, align) - StackSize;
            CGenExpandStackBy(nbytes);
        }

        public void CGenForceStackSizeTo(int nbytes)
        {
            StackSize = nbytes;
            //LEA(-nbytes, Reg.BP, Reg.SP);
        }

        public void CGenShrinkStackBy(int nbytes)
        {
            StackSize -= nbytes;
            ADDL(Reg.SP, nbytes);
        }

        public void CGenExpandStackBy4Bytes(string comment = "")
        {
            StackSize += 4;
            SUBL(Reg.SP, 4);
        }

        public void CGenExpandStackBy8Bytes(string comment = "")
        {
            StackSize += 8;
            SUBL(Reg.SP, 8);
        }

        public void CGenShrinkStackBy4Bytes(string comment = "")
        {
            StackSize -= 4;
            ADDL(Reg.SP, 4);
        }

        public void CGenShrinkStackBy8Bytes(string comment = "")
        {
            StackSize -= 8;
            ADDL(Reg.SP, 8);
        }

        public void LEAVE()
        {
            //os.WriteLine("    leave # pop frame, restore %ebp");
            os.WriteLine("\tleave");
            StackSize -= 2;
        }

        public void ENTER()
        {
            //os.WriteLine("    leave # pop frame, restore %ebp");
            os.WriteLine("\tenter");
            StackSize += 2;
        }

        public void RET(FunctionType type)
        {
            //os.WriteLine("    ret # pop old %eip, jump");
            if (type.Args.Count > 0)
            {
                os.WriteLine("\tret\t" + type.ArgCount());
                StackSize -= type.ArgCount();
            }
            else
            {
                os.WriteLine("\tretz");
            }

            StackSize -= 3;
        }

        public void NEWLINE()
        {
            os.WriteLine();
        }

        public void COMMENT(string comment)
        {
            os.WriteLine("    ; " + comment);
        }

        /// <summary>
        /// NEG addr: addr = -addr
        /// </summary>
        public void NEG(string addr) => os.WriteLine($"\tneg\t{addr}");

        public void NEG(Reg dst) => NEG(RegToString(dst));

        /// <summary>
        /// NOT: bitwise not
        /// </summary>
        public void NOT(string addr) => os.WriteLine($"\tnot\t{addr}");

        public void NOT(Reg dst) => NOT(RegToString(dst));

        public void INC(Reg reg) => os.WriteLine($"\tinc\t{RegToString(reg)}");
        public void DEC(Reg reg) => os.WriteLine($"\tdec\t{RegToString(reg)}");

        /// <summary>
        /// ADDL: add long
        /// </summary>
        public void ADDL(string src, string dst, string comment = "")
        {
            os.Write($"\tadd\t{src},\t{dst}");
            if (comment == "")
            {
                os.WriteLine();
            }
            else
            {
                COMMENT(comment);
            }
        }


        public void ADDL(Reg dst, int src, string comment = "") => ADDL(RegToString(dst), $"0x{Convert.ToString(src, 16)}", comment);
        public void ADDL(Reg dst, Reg src, string comment = "") => ADDL(RegToString(dst), RegToString(src), comment);

        /// <summary>
        /// SUBL: subtract long
        /// </summary>
        public void SUBL(string dst, string src, string comment = "")
        {
            os.Write($"\tsub\t{dst},\t{src}");
            if (comment == "")
            {
                os.WriteLine();
            }
            else
            {
                COMMENT(comment);
            }
        }

        private void SUBL(string dst, int src, string comment = "") => SUBL(dst, $"0x{Convert.ToString(src, 16)}", comment);

        public void SUBL(Reg dst, int src, string comment = "") => SUBL(RegToString(dst), $"0x{Convert.ToString(src, 16)}", comment);

        public void SUBL(Reg dst, Reg src, string comment = "") => SUBL(RegToString(dst), RegToString(src), comment);

        public override string ToString()
        {
            return os.ToString() + rodata;
        }

        /// <summary>
        /// ANDL er, ee
        /// ee = er & ee
        /// </summary>
        public void ANDL(string dst, string src) => os.WriteLine($"\tand\t{dst},\t{src}");

        public void ANDL(Reg dst, Reg src) => ANDL(RegToString(dst), RegToString(src));
        public void ANDL(Reg dst, int src) => ANDL(RegToString(dst), $"0x{Convert.ToString(src, 16)}");

        /// <summary>
        /// ORL er, ee
        ///     ee = ee | er
        /// </summary>
        public void ORL(string dst, string src, string comment = "")
        {
            os.Write($"\tor\t{dst},\t{src}");
            if (comment == "")
            {
                os.WriteLine();
            }
            else
            {
                COMMENT(comment);
            }
        }

        public void ORL(Reg dst, Reg src, string comment = "")
        {
            ORL(RegToString(dst), RegToString(src), comment);
        }

        /// <summary>
        /// SALL er, ee
        /// ee = ee << er
        /// Note that there is only one Kind of lshift.
        /// </summary>
        public void SALL(string dst, string src)
        {
            os.WriteLine($"\tshl\t{dst},\t{src}");
        }

        public void SALL(Reg dst, Reg src)
        {
            SALL(RegToString(dst), RegToString(src));
        }

        /// <summary>
        /// SARL er, ee (arithmetic shift)
        /// ee = ee >> er (append sign bit)
        /// </summary>
        public void SARL(string dst, string src)
        {
            os.WriteLine($"\tshr\t{dst},\t{src}");
        }

        public void SARL(Reg dst, Reg src) => SARL(RegToString(dst), RegToString(src));

        /// <summary>
        /// SHRL er, ee (logical shift)
        /// ee = ee >> er (append 0)
        /// </summary>
        public void SHRL(string dst, string src)
        {
            os.WriteLine($"\tshr\t{dst},\t{src}");
        }

        public void SHRL(Reg dst, Reg src) => SHRL(RegToString(dst), RegToString(src));

        public void SHRL(Reg dst, int src) => SHRL(RegToString(dst), $"0x{Convert.ToString(src, 16)}");

        /// <summary>
        /// XORL er, ee
        /// ee = ee ^ er
        /// </summary>
        public void XORL(string er, string ee)
        {
            os.WriteLine($"\txor\t{er},\t{ee}");
        }

        public void XORL(Reg er, Reg ee)
        {
            XORL(RegToString(er), RegToString(ee));
        }

        /// <summary>
        /// IMUL: signed multiplication. %edx:%eax = %eax * {addr}.
        /// </summary>
        public void IMUL(string addr)
        {
            os.WriteLine($"    uml {addr}");
        }

        public void IMUL(Reg er)
        {
            IMUL(RegToString(er));
        }

        /// <summary>
        /// MUL: unsigned multiplication. %edx:%eax = %eax * {addr}.
        /// </summary>
        public void MUL(string er, string ee)
        {
            os.WriteLine($"\tmul\t{er},\t{ee}");
        }

        public void MUL(Reg er, Reg ee)
        {
            MUL(RegToString(er), RegToString(ee));
        }

        /// <summary>
        /// CLTD: used before division. clear %edx.
        /// </summary>
        public void CLTD() => os.WriteLine("    cltd");

        /// <summary>
        /// IDIVL: signed division. %eax = %edx:%eax / {addr}.
        /// </summary>
        public void IDIVL(string addr)
        {
            os.WriteLine($"    div {addr}");
        }

        public void IDIVL(Reg er) => IDIVL(RegToString(er));

        /// <summary>
        /// IDIVL: unsigned division. %eax = %edx:%eax / {addr}.
        /// </summary>
        public void DIVL(string addr)
        {
            os.WriteLine($"    duv {addr}");
        }

        public void DIVL(Reg er) => DIVL(RegToString(er));

        /// <summary>
        /// CMPL: compare based on subtraction.
        /// Note that the order is reversed, i.e. ee comp er.
        /// </summary>
        public void CMPL(string er, string ee)
        {
            os.WriteLine($"\tcmp\t{er},\t{ee}");
        }

        public void CMPL(Reg er, Reg ee) => CMPL(RegToString(er), RegToString(ee));

        public void CMPL(int imm, Reg ee) => CMPL($"${imm}", RegToString(ee));

        /// <summary>
        /// TESTL: used like testl %eax, %eax: compare %eax with zero.
        /// </summary>
        public void TESTL(string er)
        {
            os.WriteLine($"\ttest\t{er}");
        }

        public void TESTL(Reg er) => TESTL(RegToString(er));

        /// <summary>
        /// SETE: set if equal to.
        /// </summary>
        public void SETE(string dst)
        {
            os.WriteLine($"    sete {dst}");
        }

        public void SETE(Reg dst) => SETE(RegToString(dst));

        /// <summary>
        /// SETNE: set if not equal to.
        /// </summary>
        public void SETNE(string dst) => os.WriteLine($"    setne {dst}");
        public void SETNE(Reg dst) => SETNE(RegToString(dst));

        /// <summary>
        /// SETG: set if greater than (signed).
        /// </summary>
        public void SETG(string dst)
        {
            os.WriteLine($"    setg {dst}");
        }

        public void SETG(Reg dst) => SETG(RegToString(dst));

        /// <summary>
        /// SETGE: set if greater or equal to (signed).
        /// </summary>
        public void SETGE(string dst)
        {
            os.WriteLine($"    setge {dst}");
        }

        public void SETGE(Reg dst) => SETGE(RegToString(dst));

        /// <summary>
        /// SETL: set if less than (signed).
        /// </summary>
        public void SETL(string dst)
        {
            os.WriteLine($"    setl {dst}");
        }

        public void SETL(Reg dst) => SETL(RegToString(dst));

        /// <summary>
        /// SETLE: set if less than or equal to (signed).
        /// </summary>
        public void SETLE(string dst)
        {
            os.WriteLine($"    setle {dst}");
        }

        public void SETLE(Reg dst) => SETLE(RegToString(dst));

        /// <summary>
        /// SETB: set if below (unsigned).
        /// </summary>
        public void SETB(string dst)
        {
            os.WriteLine($"    setb {dst}");
        }

        public void SETB(Reg dst) => SETB(RegToString(dst));

        /// <summary>
        /// SETNB: set if not below (unsigned).
        /// </summary>
        public void SETNB(string dst)
        {
            os.WriteLine($"    setnb {dst}");
        }

        public void SETNB(Reg dst) => SETNB(RegToString(dst));

        /// <summary>
        /// SETA: set if above (unsigned).
        /// </summary>
        public void SETA(string dst)
        {
            os.WriteLine($"    seta {dst}");
        }

        public void SETA(Reg dst) => SETA(RegToString(dst));

        /// <summary>
        /// SETNA: set if not above (unsigned).
        /// </summary>
        public void SETNA(string dst)
        {
            os.WriteLine($"    setna {dst}");
        }

        public void SETNA(Reg dst) => SETNA(RegToString(dst));

        /// <summary>
        /// FUCOMIP: unordered comparison: %st(0) vs %st(1).
        /// </summary>
        public void FUCOMIP() => os.WriteLine("    fucomip %st(1), %st");

        public void JMP(int label) => os.WriteLine($"\tjmp\t[L{label}]");

        public void JZ(int label) => os.WriteLine($"\tjz\t[L{label}]");

        public void JNZ(int label) => os.WriteLine($"\tjz\t[L{label}]");

        public void CLD() => os.WriteLine("    cld");

        public void STD() => os.WriteLine("    std");

        public int CGenPushLong(Reg src)
        {
            PUSHL(src);
            StackSize += 4;
            return StackSize;
        }

        public int CGenPushLong(int imm)
        {
            PUSHL(imm);
            StackSize += 4;
            return StackSize;
        }

        public void CGenPopLong(int saved_size, Reg dst)
        {
            if (StackSize == saved_size)
            {
                POPL(dst);
                StackSize -= 4;
            }
            else
            {
                MOV(-saved_size, Reg.BP, dst);
            }
        }

        public int CGenPushFloat()
        {
            CGenExpandStackBy4Bytes();
            FSTS(0, Reg.SP);
            return StackSize;
        }

        public int CGenPushFloatP()
        {
            CGenExpandStackBy4Bytes();
            FSTPS(0, Reg.SP);
            return StackSize;
        }

        public int CGenPushDouble()
        {
            CGenExpandStackBy8Bytes();
            FSTL(0, Reg.SP);
            return StackSize;
        }

        public int CGenPushDoubleP()
        {
            CGenExpandStackBy8Bytes();
            FSTPL(0, Reg.SP);
            return StackSize;
        }

        public void CGenPopDouble(int saved_size)
        {
            FLDL(-saved_size, Reg.BP);
            if (saved_size == StackSize)
            {
                CGenShrinkStackBy8Bytes();
            }
        }

        public void CGenPopFloat(int saved_size)
        {
            FLDL(-saved_size, Reg.BP);
            if (saved_size == StackSize)
            {
                CGenShrinkStackBy4Bytes();
            }
        }

        private void FISTL(string dst) => os.WriteLine($"    fistl {dst}");

        private void FISTL(int offset, Reg dst) => FISTL($"{offset}({RegToString(dst)})");

        private void FILDL(string dst) => os.WriteLine($"    fildl {dst}");

        private void FILDL(int offset, Reg dst) => FILDL($"{offset}({RegToString(dst)})");

        public void CGenConvertFloatToLong()
        {
            CGenExpandStackBy4Bytes();
            FISTL(0, Reg.SP);
            MOV(0, Reg.SP, Reg.AX);
            CGenShrinkStackBy4Bytes();
        }

        public void CGenConvertLongToFloat()
        {
            CGenExpandStackBy4Bytes();
            MOV(Reg.AX, 0, Reg.SP);
            FILDL(0, Reg.SP);
            CGenShrinkStackBy4Bytes();
        }

        /// <summary>
        /// Fast Memory Copy using assembly.
        /// Make sure that
        /// 1) %esi = source address
        /// 2) %edi = destination address
        /// 3) %ecx = number of bytes
        /// </summary>
        public void CGenMemCpy()
        {
            MOVB(Reg.CL, Reg.AL);
            SHRL(Reg.CX, 2);
            CLD();
            os.WriteLine("    rep movsl");
            MOVB(Reg.AL, Reg.CL);
            ANDL(Reg.CL, 3);
            os.WriteLine("    rep movsb");
        }

        /// <summary>
        /// Fast Memory Copy using assembly.
        /// Make sure that
        /// 1) %esi = source address
        /// 2) %edi = destination address
        /// 3) %ecx = number of bytes
        /// </summary>
        public void CGenMemCpyReversed()
        {
            ADDL(Reg.ESI, Reg.CX);
            ADDL(Reg.EDI, Reg.CX);
            MOV(Reg.CX, Reg.AX);

            ANDL(Reg.CX, 3); // now %ecx = 0, 1, 2, or 3
            STD();
            os.WriteLine("    rep movsb");

            MOV(Reg.AX, Reg.CX);
            ANDL(Reg.CX, ~3);
            SHRL(Reg.CX, 2);
            os.WriteLine("    rep movsl");

            CLD();
        }

        public string CGenLongConst(int val)
        {
            string name = ".LC" + rodata_idx;
            rodata.WriteLine(name + ":");
            rodata.WriteLine("    .dd " + val);
            rodata_idx++;
            return name;
        }

        public string CGenLongLongConst(int lo, int hi)
        {
            string name = ".LC" + rodata_idx;
            rodata.WriteLine(name + ":");
            rodata.WriteLine("    .dd " + lo);
            rodata.WriteLine("    .dd " + hi);
            rodata_idx++;
            return name;
        }

        public string CGenstring(string str)
        {
            string name = ".LC" + rodata_idx;
            rodata.WriteLine(name + ":");
            rodata.WriteLine("    .db \"" + str + "\", 0");
            rodata_idx++;
            return name;
        }
        */

        public void CGenLabel(string label) => os.WriteLine($"{label}:");

        public void CGenLabel(int label) => CGenLabel($"L{label}");

        private StringWriterBES os;
        private readonly System.IO.StringWriter rodata;
        private int rodata_idx;
        public int label_idx;

        private Status status;

        public int StackSize { get; private set; }

        public int RequestLabel()
        {
            return label_idx++;
        }

        //private Stack<int> _continue_labels;
        //private Stack<int> _break_labels;

        private struct LabelPack
        {
            public LabelPack(int continue_label, int break_label, int default_label, Dictionary<int, int> value_to_label)
            {
                this.continue_label = continue_label;
                this.break_label = break_label;
                this.default_label = default_label;
                this.value_to_label = value_to_label;
            }
            public readonly int continue_label;
            public readonly int break_label;
            public readonly int default_label;
            public readonly Dictionary<int, int> value_to_label;
        }

        private readonly Stack<LabelPack> label_packs;

        public int ContinueLabel => label_packs.First(_ => _.continue_label != -1).continue_label;

        public int BreakLabel => label_packs.First(_ => _.break_label != -1).break_label;

        public int DefaultLabel
        {
            get
            {
                int ret = label_packs.First().default_label;
                if (ret == -1)
                {
                    throw new InvalidOperationException("Not in a switch statement.");
                }
                return ret;
            }
        }

        public int CaseLabel(int value) => label_packs.First(_ => _.value_to_label != null).value_to_label[value];
        // label_packs.First().value_to_label[Value];

        public void InLoop(int continue_label, int break_label)
        {
            label_packs.Push(new LabelPack(continue_label, break_label, -1, null));
            //_continue_labels.Push(continue_label);
            //_break_labels.Push(break_label);
        }

        public void InSwitch(int break_label, int default_label, Dictionary<int, int> value_to_label)
        {
            label_packs.Push(new LabelPack(-1, break_label, default_label, value_to_label));
        }

        public void OutLabels()
        {
            label_packs.Pop();
            //_continue_labels.Pop();
            //_break_labels.Pop();
        }

        private readonly Dictionary<string, int> _goto_labels = new Dictionary<string, int>();

        public int GotoLabel(string label)
        {
            return _goto_labels[label];
        }

        private int return_label;
        public int ReturnLabel
        {
            get
            {
                if (return_label == -1)
                {
                    throw new InvalidOperationException("Not inside a function.");
                }
                return return_label;
            }
        }

        public void InFunction(IReadOnlyList<string> goto_labels)
        {
            return_label = RequestLabel();
            _goto_labels.Clear();
            foreach (string goto_label in goto_labels)
            {
                _goto_labels.Add(goto_label, RequestLabel());
            }
        }

        public void OutFunction()
        {
            return_label = -1;
            _goto_labels.Clear();
        }
    }
}