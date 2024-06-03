namespace FTG.Studios.MCC
{
	
	public static class Syntax {
		
		public const char semicolon = ';';
		public const char open_parenthesis = '(';
		public const char close_parenthesis = ')';
		public const char open_brace = '{';
		public const char close_brace = '}';
		
		public const string identifier = @"[a-zA-Z]\w*\b";
		public const string integer_literal = @"[0-9]+\b";
		
		public static readonly string[] keywords = new string[] { "void", "return", "int" };
		public enum Keyword { Void, Return, Integer };
	}
}