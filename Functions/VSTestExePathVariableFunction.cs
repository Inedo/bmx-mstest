using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;

namespace Inedo.BuildMasterExtensions.MsTest.Functions
{
    [ScriptAlias("VSTestExePath")]
    [Description("Placeholder. Will be implemented in a future version.")]
    [VariableFunctionProperties(
        Category = "Server",
        Scope = VariableFunctionScope.Server)]
    public sealed class VSTestExePathVariableFunction : ScalarVariableFunctionBase
    {
        protected override object EvaluateScalar(IGenericBuildMasterContext context) => string.Empty;
    }
}
