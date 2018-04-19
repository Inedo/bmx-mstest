using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.MsTest.Operations;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.MsTest.ActionImporters
{
    internal sealed class VsTestImporter : IActionOperationConverter<VsTestUnitTestAction, VsTestOperation>
    {
        public ConvertedOperation<VsTestOperation> ConvertActionToOperation(VsTestUnitTestAction action, IActionConverterContext context)
        {
            return new VsTestOperation
            {
                ClearExistingTestResults = action.ClearExistingTestResults,
                AdditionalArguments = AH.NullIf(context.ConvertLegacyExpression(action.AdditionalArguments), string.Empty),
                TestContainer = context.ConvertLegacyExpression(!string.IsNullOrEmpty(action.OverriddenSourceDirectory) ? PathEx.Combine(action.OverriddenSourceDirectory, action.TestContainer) : action.TestContainer),
                TestGroup = context.ConvertLegacyExpression(action.GroupName),
                VsTestPath = "$VSTestExePath"
            };
        }
    }
}
