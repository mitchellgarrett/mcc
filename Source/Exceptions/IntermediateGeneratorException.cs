using System;

namespace FTG.Studios.MCC.Intermediate;

public class IntermediateGeneratorException : Exception
{
	public readonly Type Got;
	public readonly Type[] Expected;

	public IntermediateGeneratorException(string message, Type got, object value, params Type[] expected)
	: base($"\x1b[1;91mERROR:\x1b[39m {message} expected: [{string.Join<Type>(", ", expected)}], got: {got} ({value})\x1b[0m")
	{
		Got = got;
		Expected = expected;
	}
}