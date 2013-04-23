using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Inedo.BuildMaster.Extensibility;

[assembly: AssemblyTitle("Amazon")]
[assembly: AssemblyDescription("Contains actions to interface with Amazon Web Services.")]

[assembly: ComVisible(false)]
[assembly: AssemblyCompany("Inedo, LLC")]
[assembly: AssemblyProduct("BuildMaster")]
[assembly: AssemblyCopyright("Copyright © 2008 - 2012")]
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyFileVersion("0.0")]
[assembly: BuildMasterAssembly]
[assembly: CLSCompliant(false)]
[assembly: RequiredBuildMasterVersion("3.0.0")]

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("CloudFormationTests")]