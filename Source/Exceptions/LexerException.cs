using System;

namespace FTG.Studios.MCC.Lexer;

public class LexerException : Exception
{
	public readonly string Lexeme;

	public LexerException(string message, string lexeme)
	: base($"\x1b[1;91mERROR:\x1b[39m {message}\x1b[0m")
	{
		Lexeme = lexeme;
	}
}