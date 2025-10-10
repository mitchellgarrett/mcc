using System.Runtime.InteropServices;
using FTG.Studios.MCC.CodeGeneration;

namespace FTG.Studios.MCC.Assembly;

public static partial class AssemblyNode
{

	public abstract class Operand : Node
	{
		public abstract string Emit();
	}

	public class Register(RegisterType value) : Operand
	{
		public readonly RegisterType Value = value;

		// TODO: This changes depending on 32 or 64 bit
		public override string Emit()
		{
			return Value.Emit32();
		}

		public override string ToString()
		{
			return $"Register({Value})";
		}
	}

	public class Immediate(int value) : Operand
	{
		public readonly int Value = value;

		public override string Emit()
		{
			return $"${Value}";
		}

		public override string ToString()
		{
			return $"Immediate({Value})";
		}
	}

	public static Immediate ToAssemblyImmediate(this int value)
	{
		return new Immediate(value);
	}

	public class PseudoRegister(string identifier) : Operand
	{
		public readonly string Identifier = identifier;

		public override string Emit()
		{
			return $"{Identifier}";
		}

		public override string ToString()
		{
			return $"Pseudo(\"{Identifier}\")";
		}
	}
	
	public abstract class MemoryAccess : Operand;

	public class StackAccess(int offset) : MemoryAccess
	{
		public readonly int Offset = offset;

		public override string Emit()
		{
			return $"{Offset}(%rbp)";
		}

		public override string ToString()
		{
			return $"Stack({Offset})";
		}
	}
	
	public class DataAccess(string identifier) : MemoryAccess
	{
		public readonly string Identifier = identifier;

		public override string Emit()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return $"{CodeEmitter.macos_function_prefix}{Identifier}(%rip)";
			return $"{Identifier}(%rip)";
		}

		public override string ToString()
		{
			return $"Data(\"{Identifier}\")";
		}
	}
}