namespace Ergo.Compiler.Emission;

public enum OpCode : byte
{
    put_variable_heap,
    put_variable,
    put_value,
    put_unsafe_value,
    put_structure,
    put_list,
    put_constant,

    set_variable,
    set_value,
    set_local_value,
    set_constant,
    set_void,

    get_variable,
    get_value,
    get_structure,
    get_list,
    get_constant,

    unify_variable,
    unify_value,
    unify_local_value,
    unify_constant,
    unify_void,

    allocate,
    deallocate,
    call,
    execute,
    proceed,

    try_me_else,
    retry_me_else,
    trust_me,
    @try,
    retry,
    trust,

    switch_on_term,
    switch_on_constant,
    switch_on_structure,

    neck_cut,
    get_level,
    cut
}
