using System.ComponentModel;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.MsTest.Functions
{
    [ScriptAlias("VSTestExePath")]
    [Description("The path to vstest.console.exe; if unspecified, the operation will attempt to find it in the Visual Studio installation path.")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class VSTestExePathVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context) => string.Empty;
    }
}
