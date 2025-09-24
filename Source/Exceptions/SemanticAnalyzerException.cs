using System;

namespace FTG.Studios.MCC.SemanticAnalysis;

public class SemanticAnalzyerException : Exception
{
	public readonly string Identifier;

	public SemanticAnalzyerException(string message, string identifier)
	: base($"\x1b[1;91mERROR:\x1b[39m {message}\x1b[0m")
	{
		Identifier = identifier;
	}
}