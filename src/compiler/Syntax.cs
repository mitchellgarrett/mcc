namespace FTG.Studios.MCC
{
	
	public static class Syntax {
		
		public const char semicolon = ';';
		public const char open_parenthesis = '(';
		public const char close_parenthesis = ')';
		public const char open_brace = '{';
		public const char close_brace = '}';
		
		// Unary Operators
		public const char operator_negation = '-';
		public const char operator_bitwise_complement = '~';
		public const string operator_decrement = "--";
		
		// Binary Operators
		public const char operator_addition = '+';
		public const char operator_subtraction = '-';
		public const char operator_multiplication = '*';
		public const char operator_division = '/';
		public const char operator_remainder = '%';
		
		public const string identifier = @"[a-zA-Z]\w*\b";
		public const string integer_literal = @"[0-9]+\b";
		
		public static readonly string[] keywords = new string[] { "void", "return", "int" };
		
		public enum Keyword { Void, Return, Integer };
		public enum UnaryOperator { Negation, BitwiseComplement, Decrement };
		public enum BinaryOperator { Addition, Subtraction, Multiplication, Division, Remainder };
		
		static readonly int[] operator_precedence = new int[] { 0, 0, 1, 1, 1 };
		
		public static int GetPrecedence(this BinaryOperator op) {
			return operator_precedence[(int)op];
		}
	}
}