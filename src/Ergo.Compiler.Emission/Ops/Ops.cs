﻿using Ergo.Lang.Ast;
using System.Text;

namespace Ergo.Compiler.Emission;
public static class Ops
{
    #region Types
    #endregion

    #region Fields
    public static readonly Func<__WORD, __WORD, Op> get_constant = (__WORD c, __WORD Ai) => new Op(OpCode.get_constant, () => c, () => Ai);
    public static readonly Func<__WORD, __WORD, Op> get_value = (__WORD Vn, __WORD Ai) => new Op(OpCode.get_value, () => Vn, () => Ai);
    public static readonly Func<__WORD, __WORD, Op> get_variable = (__WORD Vn, __WORD Ai) => new Op(OpCode.get_variable, () => Vn, () => Ai);
    public static readonly Func<__WORD, __WORD, Op> get_structure = (__WORD Pn, __WORD Xi) => new Op(OpCode.get_structure, () => Pn, () => Xi);

    public static readonly Func<__WORD, __WORD, Op> put_constant = (__WORD c, __WORD Ai) => new Op(OpCode.put_constant, () => c, () => Ai);
    public static readonly Func<__WORD, __WORD, Op> put_value = (__WORD Vn, __WORD Ai) => new Op(OpCode.put_value, () => Vn, () => Ai);
    public static readonly Func<__WORD, __WORD, Op> put_variable = (__WORD Vn, __WORD Ai) => new Op(OpCode.put_variable, () => Vn, () => Ai);
    public static readonly Func<__WORD, __WORD, Op> put_structure = (__WORD Pn, __WORD Xi) => new Op(OpCode.put_structure, () => Pn, () => Xi);

    public static readonly Func<__WORD, Op> unify_variable = (__WORD Vn) => new Op(OpCode.unify_variable, () => Vn);
    public static readonly Func<__WORD, Op> unify_value = (__WORD Vn) => new Op(OpCode.unify_value, () => Vn);
    public static readonly Func<__WORD, Op> unify_local_value = (__WORD Vn) => new Op(OpCode.unify_local_value, () => Vn);
    public static readonly Func<__WORD, Op> unify_constant = (__WORD c) => new Op(OpCode.unify_constant, () => c);
    public static readonly Func<__WORD, Op> unify_void = (__WORD n) => new Op(OpCode.unify_void, () => n);

    public static readonly Op allocate = new (OpCode.allocate);
    public static readonly Op deallocate = new (OpCode.deallocate);
    public static readonly Op proceed = new (OpCode.proceed);
    public static readonly Func<__WORD, Op> call = (__WORD P) => new Op(OpCode.call, () => P);
    public static readonly Func<__WORD, Op> cut = (__WORD L) => new Op(OpCode.cut, () => L);
    public static readonly Func<__WORD, Op> get_level = (__WORD L) => new Op(OpCode.get_level, () => L);

    public static readonly Func<__WORD, Op> try_me_else = (__WORD L) => new Op(OpCode.try_me_else, () => L);
    public static readonly Func<__WORD, Op> retry_me_else = (__WORD L) => new Op(OpCode.retry_me_else, () => L);
    public static readonly Op trust_me = new (OpCode.trust_me);
    #endregion
}

