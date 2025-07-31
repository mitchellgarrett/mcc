using System;

namespace FTG.Studios.MCC
{

	public class IntermediateGeneratorException : Exception
	{
		public readonly Type Got;
		public readonly Type[] Expected;

		public IntermediateGeneratorException(string message, Type got, object value, params Type[] expected)
		: base($"{message} expected: [{string.Join<Type>(", ", expected)}], got: {got} ({value})")
		{
			Got = got;
			Expected = expected;
		}
	}
}