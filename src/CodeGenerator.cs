namespace FTG.Studios.MCC {
	
	public static class CodeGenerator {
		
		public static AssemblyTree Generate(ParseTree ast) {
			AssemblyNode.Program program = GenerateProgram(ast.Program);
			return new AssemblyTree(program);
		}
		
		static AssemblyNode.Program GenerateProgram(ParseNode.Program program) {
			AssemblyNode.Function function = GenerateFunction(program.Function);
			return new AssemblyNode.Program(function);
		}
		
		static AssemblyNode.Function GenerateFunction(ParseNode.Function function) {
			string identifier = function.Identifier.Value;
			AssemblyNode.Instruction[] body = GenerateStatement(function.Body);
			return new AssemblyNode.Function(identifier, body);
		}
		
		static AssemblyNode.Instruction[] GenerateStatement(ParseNode.Statement statement) {
			if (statement is ParseNode.ReturnStatement) return GenerateReturnStatement(statement as ParseNode.ReturnStatement);
			return null;
		}
		
		static AssemblyNode.Instruction[] GenerateReturnStatement(ParseNode.ReturnStatement statement) {
			AssemblyNode.Instruction[] body = new AssemblyNode.Instruction[2];
			
			AssemblyNode.Operand source = GenerateExpression(statement.Expression);
			AssemblyNode.Operand destination = new AssemblyNode.Register("eax");
			
			body[0] = new AssemblyNode.MOV(source, destination);
			body[1] = new AssemblyNode.RET();
			
			return body;
		}
		
		static AssemblyNode.Operand GenerateExpression(ParseNode.Expression expression) {
			if (expression is ParseNode.ConstantExpression) return GenerateConstantExpression(expression as ParseNode.ConstantExpression);
			return null;
		}
		
		static AssemblyNode.Operand GenerateConstantExpression(ParseNode.ConstantExpression expression) {
			return new AssemblyNode.Immediate(expression.Value);
		}
	}
}