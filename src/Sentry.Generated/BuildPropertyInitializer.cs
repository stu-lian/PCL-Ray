using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sentry.CompilerServices;

namespace Sentry.Generated;

[GeneratedCode("Sentry.Compiler.Extensions", "6.6.0.0")]
internal static class BuildPropertyInitializer
{
	[ModuleInitializer]
	internal static void Initialize()
	{
		BuildProperties.Initialize(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "includeallcontentforselfextract", "" },
			{ "enabletrimanalyzer", "" },
			{ "verifyreferencetrimcompatibility", "" },
			{ "rootnamespace", "PCL" },
			{ "_mvvmtoolkitisusingwindowsruntimepack", "false" },
			{ "publishselfcontained", "" },
			{ "sentrydisablesourcegenerator", "" },
			{ "projecttypeguids", "" },
			{ "entrypointfilepath", "" },
			{ "usedotnetnativetoolchain", "" },
			{ "verifyreferenceaotcompatibility", "" },
			{ "enforceextendedanalyzerrules", "" },
			{ "cswinrtaotwarninglevel", "" },
			{ "enablesinglefileanalyzer", "true" },
			{ "targetframeworkidentifier", ".NETCoreApp" },
			{ "configuration", "Release" },
			{ "cswinrtaotoptimizerenabled", "" },
			{ "targetframework", "net10.0-windows" },
			{ "targetplatformminversion", "7.0" },
			{ "targetframeworkversion", "v10.0" },
			{ "enablecodestyleseverity", "" },
			{ "invariantglobalization", "" },
			{ "platformneutralassembly", "" },
			{ "effectiveanalysislevelstyle", "10.0" },
			{ "mvvmtoolkitenableinotifypropertychangingsupport", "true" },
			{ "projectdir", "" },
			{ "_ispublishing", "true" },
			{ "enablecomhosting", "" },
			{ "enablegeneratedcominterfacecomimportinterop", "" },
			{ "publishtrimmed", "" },
			{ "_supportedplatformlist", "Linux,macOS,Windows" },
			{ "cswinrtusewindowsuixamlprojections", "false" },
			{ "usingmicrosoftnetsdkweb", "" },
			{ "cswinrtcomponent", "" },
			{ "enableaotanalyzer", "" },
			{ "publishaot", "" }
		});
	}
}
