// This file is used by Code Analysis to maintain SuppressMessage attributes.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1401:P/Invokes should not be visible", Justification = "NativeMethods is intentionally public for Win32 interop", Scope = "module")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Win32 API naming conventions", Scope = "module")]
[assembly: SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments", Justification = "LPStr marshaling is correct for these Win32 APIs", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Dispose handled via OnExit in App code-behind", Scope = "module")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "CommunityToolkit.Mvvm requires instance methods for source generators", Scope = "module")]
[assembly: SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Interface abstraction is intentional for testability", Scope = "module")]
