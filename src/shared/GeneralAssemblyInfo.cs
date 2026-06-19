#region U S A G E S

using System.Reflection;

#if NETSTANDARD2_0_OR_GREATER
using System.Resources;
#endif

#endregion

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("RzR SOFT & TECH ®")]
[assembly: AssemblyProduct("PipelineFlowEngine")]
[assembly: AssemblyCopyright("Copyright © 2022-2025 RzR All rights reserved.")]
[assembly: AssemblyTrademark("RzR SOFT Solution™")]
[assembly: AssemblyDescription("The repository implements a pipeline composed of multiple interdependent methods. The execution flows in a single, ordered direction—step by step—with support for enabling or disabling individual steps, customizing execution order, performing pre-execution validation, applying retry policies, collecting execution logs for each step, and more.")]

[assembly: AssemblyMetadata("TermsOfService", "")]
[assembly: AssemblyMetadata("ContactUrl", "")]
[assembly: AssemblyMetadata("ContactName", "RzR")]
[assembly: AssemblyMetadata("ContactEmail", "ddpRzR@hotmail.com")]
#if NETSTANDARD2_0_OR_GREATER
[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)]
#endif
[assembly: AssemblyVersion("2.0.0.8471")]
[assembly: AssemblyFileVersion("2.0.0.8471")]
[assembly: AssemblyInformationalVersion("2.0.0.8471")]
