using System;
using System.IO;
using System.Collections.Generic;
using FTG.Studios.MCC;

class Application {
	
	/// Command Line Arguments
	/// --lex - stop before parsing
	/// --parse - stop before code generation
	/// --codegen - stop before code emission
	/// -S - generate assembly but not executeable
	
	public static void Main(string[] args) {
		string input_path = args[0];
		string output_path = args[1];
		
		string source = File.ReadAllText(input_path);
		
		Console.WriteLine("------");
		Console.WriteLine("Source");
		Console.WriteLine("------");
		Console.WriteLine(source);
		Console.WriteLine("------\n");
		
		Console.WriteLine("------");
		Console.WriteLine("Tokens");
		Console.WriteLine("------");

		List<Token> tokens = null;
		try
		{
			tokens = Lexer.Tokenize(source);
			foreach (var token in tokens) Console.WriteLine(token);
		}
		catch (LexerException e)
		{
			Console.Error.WriteLine(e.Message);
			Environment.Exit(1);
		}
		
		Console.WriteLine("------\n");
		
		Console.WriteLine("----------");
		Console.WriteLine("Parse Tree");
		Console.WriteLine("----------");

		ParseTree parse_tree = null;
		try
		{
			parse_tree = Parser.Parse(tokens);
			Console.WriteLine(parse_tree);
		}
		catch (ParserException e)
		{
			Console.Error.WriteLine(e.Message);
			Environment.Exit(1);
		}
		
		Console.WriteLine("----------\n");

		Console.WriteLine("-------------------");
		Console.WriteLine("Resolved Parse Tree");
		Console.WriteLine("-------------------");

		SemanticAnalzyer.ResolveVariables(parse_tree);
		Console.WriteLine(parse_tree);
		
		Console.WriteLine("-------------------\n");
		
		Console.WriteLine("-----------------");
		Console.WriteLine("Intermediate Tree");
		Console.WriteLine("-----------------");
		
		IntermediateTree intermediate_tree = IntermediateGenerator.Generate(parse_tree);
		Console.WriteLine(intermediate_tree);
		
		Console.WriteLine("-----------------\n");
		
		Console.WriteLine("-------------");
		Console.WriteLine("Assembly Tree");
		Console.WriteLine("-------------");
		
		AssemblyTree assembly_tree = CodeGenerator.Generate(intermediate_tree);
		Console.WriteLine(assembly_tree);
		
		Console.WriteLine("-------------");
		
		Console.WriteLine("---------");
		Console.WriteLine("Optimizer");
		Console.WriteLine("---------");
		
		CodeOptimizer.AssignVariables(assembly_tree);
		CodeOptimizer.FixVariableAccesses(assembly_tree);
		Console.WriteLine(assembly_tree);
		
		Console.WriteLine("--------");

		using StreamWriter output_file = new StreamWriter(output_path);
		CodeEmitter.Emit(assembly_tree, output_file);
	}
}