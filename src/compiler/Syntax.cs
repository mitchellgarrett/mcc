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
		public const char operator_not = '!';
		public const char operator_bitwise_complement = '~';
		public const string operator_decrement = "--";
		
		// Binary Operators
		public const char operator_addition = '+';
		public const char operator_subtraction = '-';
		public const char operator_multiplication = '*';
		public const char operator_division = '/';
		public const char operator_remainder = '%';
		public const char operator_assignment = '=';
		public const string operator_logical_and = "&&";
		public const string operator_logical_or = "||";
		public const string operator_logical_equal = "==";
		public const string operator_logical_not_equal = "!=";
		public const string operator_logical_less = "<";
		public const string operator_logical_greater = ">";
		public const string operator_logical_less_equal = "<=";
		public const string operator_logical_greater_equal = ">=";
		
		public const string identifier = @"[a-zA-Z]\w*\b";
		public const string integer_literal = @"[0-9]+\b";
		
		public static readonly string[] keywords = new string[] { "void", "return", "int" };
		
		public enum Keyword { Void, Return, Integer };
		public enum UnaryOperator { Negation, Not, BitwiseComplement, Decrement };
		public enum BinaryOperator { Addition, Subtraction, Multiplication, Division, Remainder, Assignment, LogicalAnd, LogicalOr, LogicalEqual, LogicalNotEqual, LogicalLess, LogicalGreather, LogicalLessEqual, LogicalGreatherEqual };
		
		static readonly string[] unary_operators = new string[] { operator_negation.ToString(), operator_not.ToString(), operator_bitwise_complement.ToString(), operator_decrement };
		public static string GetOperator(this UnaryOperator op) {
			return unary_operators[(int)op];
		}
		
		static readonly string[] binary_operators = new string[] { operator_addition.ToString(), operator_subtraction.ToString(), operator_multiplication.ToString(), operator_division.ToString(), operator_remainder.ToString(), operator_assignment.ToString(), operator_logical_and, operator_logical_or, operator_logical_equal, operator_logical_not_equal, operator_logical_less, operator_logical_greater, operator_logical_less_equal, operator_logical_greater_equal };
		public static string GetOperator(this BinaryOperator op) {
			return binary_operators[(int)op];
		}
		
		static readonly int[] operator_precedence = new int[] { 5, 5, 6, 6, 6, 0, 2, 1, 3, 3, 4, 4, 4, 4 };
		public static int GetPrecedence(this BinaryOperator op) {
			return operator_precedence[(int)op];
		}
	}
}