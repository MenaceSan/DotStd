// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1845:Use span-based 'string.Concat'", Justification = "ugly", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0056:Use index operator", Justification = "ugly", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "ugly", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "ugly", Scope = "module")]
