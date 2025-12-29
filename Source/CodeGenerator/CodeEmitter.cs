using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using FTG.Studios.MCC.Assembly;

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
			if (definition is AssemblyNode.StaticVariable variable) EmitStaticVariable(variable, file);
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
		
		file.WriteLine($"// {(variable.IsGlobal ? "extern" : "static")} int {variable.Identifier} = {variable.InitialValue.Value}");
		if (variable.IsGlobal) file.WriteLine($".globl {identifier}");
		
		// Zero-initialized variables go to the bss section
		bool is_zero = variable.InitialValue.Value == 0;
		if (is_zero) file.WriteLine(".bss");
		else file.WriteLine(".data");
		
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) file.WriteLine($".balign {variable.Alignment}");
		else file.WriteLine($".align {variable.Alignment}");
		
		file.WriteLine($"{identifier}:");
		
		if (is_zero) file.WriteLine($".zero {variable.Alignment}");
		else {
			BigInteger value = variable.InitialValue.Value;
			value = variable.Alignment switch
			{
				4 => value > uint.MaxValue ? uint.CreateTruncating(value) : value,
				8 => value > ulong.MaxValue ? ulong.CreateTruncating(value) : value,
				_ => throw new Exception(),
			};
			file.WriteLine($"{variable.Alignment.GetInitializer()} {value}");
		}
	}
}