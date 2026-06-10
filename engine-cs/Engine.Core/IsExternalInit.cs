// Polyfill: abilita `record` e proprietà `init` quando si compila per netstandard2.1.
// Il compilatore C# 9+ richiede questo tipo per gli init-only setter; .NET 5+ lo include
// gia, netstandard2.1 no. È internal: non inquina l'API pubblica.
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
