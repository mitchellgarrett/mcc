using System;
using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC;

public class ParserException : Exception
{
	public readonly Token Token;

	public ParserException(string message, Token token)
	: base($"\x1b[1;91mERROR:\x1b[39m Line: {token.Line}, {message}\x1b[0m")
	{
		Token = token;
	}
}