using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using FTG.Studios.MCC.Assembly;
using FTG.Studios.MCC.Parser;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.CodeGeneration;

public static class CodeEmitter
{

	public const string macos_function_prefix = "_";
	public const string linux_plt_suffix = "@PLT";
	const string linux_program_footer = ".section .note.GNU-stack,\"\",@progbits";

	public static void Emit(AssemblyTree program, StreamWriter file)
	{
		EmitProgram(program.Program, file);
	}

	static void EmitProgram(AssemblyNode.Program program, StreamWriter file)
	{
		foreach (var definition in program.TopLevelDefinitions)
		{
				 if (definition is AssemblyNode.Function function) EmitFunction(function, file);
			else if (definition is AssemblyNode.StaticVariable variable) EmitStaticVariable(variable, file);
			else if (definition is AssemblyNode.StaticConstant constant) EmitStaticConstant(constant, file);
		}

		// Write Linux-specific code at end of program
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) file.WriteLine(linux_program_footer);
	}

	static void EmitFunction(AssemblyNode.Function function, StreamWriter file)
	{
		// Add '_' to front of function names if on MacOS
		string identifier = function.Identifier;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) identifier = macos_function_prefix + identifier;
		
		// TODO: Spit out correct types in comments
		// Function header
		if (function.Parameters.Length == 0) file.WriteLine($"// return_type {function.Identifier}(void)");
		else file.WriteLine($"// return_type {function.Identifier}(type * {function.Parameters.Length})");
		if (function.IsGlobal) file.WriteLine($".globl {identifier}");
		file.WriteLine(".text");
		file.WriteLine($"{identifier}:");

		// Function prologue
		file.WriteLine("\tpushq %rbp");
		file.WriteLine("\tmovq %rsp, %rbp");

		// Emit the body of the function
		foreach (var instruction in function.Body) file.WriteLine('\t' + instruction.Emit().Replace("\n", "\n\t"));

		file.WriteLine();
	}

	static void EmitStaticVariable(AssemblyNode.StaticVariable variable, StreamWriter file)
	{
		// Add '_' to front of variable names if on MacOS
		string identifier = variable.Identifier;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) identifier = macos_function_prefix + identifier;
		
		file.WriteLine($"// {(variable.IsGlobal ? "extern" : "static")} {variable.InitialValue.Type.ToShortString()} {variable.Identifier} = {variable.InitialValue.ToCommentString()}");
		if (variable.IsGlobal) file.WriteLine($".globl {identifier}");
		
		bool is_zero = variable.InitialValue is InitialValue.IntegerConstant integer && integer.Value == 0;
		// Zero-initialized integer variables go to the bss section
		if (is_zero) EmitZeroInitializer(variable.Alignment, identifier, file);
		// Non-zero integer and all double variables go in the data section
		else EmitNonZeroInitializer(variable.InitialValue, variable.Alignment, identifier, file);
		file.WriteLine();
	}
	
	static void EmitZeroInitializer(int alignment, string identifier, StreamWriter file)
	{
		file.WriteLine(".bss");
		
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) file.WriteLine($".balign {alignment}");
		else file.WriteLine($".align {alignment}");
		
		file.WriteLine($"{identifier}:");
		file.WriteLine($".zero {alignment}");
	}
	
	static void EmitNonZeroInitializer(InitialValue.Constant constant, int alignment, string identifier, StreamWriter file)
	{
		file.WriteLine(".data");
		
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) file.WriteLine($".balign {alignment}");
		else file.WriteLine($".align {alignment}");
		
		file.WriteLine($"{identifier}:");
		EmitValue(constant, alignment, file);
	}
	
	static void EmitStaticConstant(AssemblyNode.StaticConstant constant, StreamWriter file)
	{
		string identifier = constant.Identifier;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			identifier = $"L{identifier}";
			file.WriteLine($".literal{constant.Alignment}");
			file.WriteLine($".balign {constant.Alignment}");
		}
		else
		{
			identifier = $".L{identifier}";
			file.WriteLine(".section .rodata");
			file.WriteLine($".align {constant.Alignment}");
		}
		
		file.WriteLine($"{identifier}:");
		EmitValue(constant.InitialValue, constant.Alignment, file);
		
		// Print out 8 zero bytes at end of MacOS .literal16 section to ensure it is 16-byte aligned
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && constant.Alignment == 16)
			file.WriteLine(".quad 0");
		file.WriteLine();
	}
	
	static void EmitValue(InitialValue.Constant constant, int alignment, StreamWriter file)
	{
		if (constant is InitialValue.FloatingPointConstant @double)
		{
			if (double.IsPositiveInfinity(@double.Value)) file.WriteLine($"// infinity\n.quad 0x7FF0000000000000");
			else if (double.IsNegativeInfinity(@double.Value)) file.WriteLine($"// negative infinity\n.quad 0xFFF0000000000000");
			else file.WriteLine($".double {@double.Value:G19}");
		} else
		{
			var integer = constant as InitialValue.IntegerConstant;
			BigInteger value = integer.Value;
			value = alignment switch
			{
				4 => value > uint.MaxValue ? uint.CreateTruncating(value) : value,
				8 => value > ulong.MaxValue ? ulong.CreateTruncating(value) : value,
				_ => throw new Exception(),
			};
			file.WriteLine($"{alignment.GetInitializer()} {value}");
		}
	}
}