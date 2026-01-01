using System.Numerics;
using System.Runtime.InteropServices;
using FTG.Studios.MCC.CodeGeneration;

namespace FTG.Studios.MCC.Assembly;

public static partial class AssemblyNode
{

	public abstract class Operand : Node
	{
		public abstract string Emit(AssemblyType type);
	}

	public class Register(RegisterType value) : Operand
	{
		public readonly RegisterType Value = value;

		public override string Emit(AssemblyType type)
		{
			return type switch
			{
				AssemblyType.LongWord => Value.Emit32(),
				AssemblyType.QuadWord => Value.Emit64(),
				AssemblyType.Double => Value.Emit64(),
				_ => throw new System.Exception(),
			};
		}

		public override string ToString()
		{
			return $"Register({Value})";
		}
	}

	public class Immediate(BigInteger value) : Operand
	{
		public readonly BigInteger Value = value;

		public override string Emit(AssemblyType type)
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
	
	public static Immediate ToAssemblyImmediate(this uint value)
	{
		return new Immediate(value);
	}
	
	public static Immediate ToAssemblyImmediate(this long value)
	{
		return new Immediate(value);
	}
	
	public static Immediate ToAssemblyImmediate(this ulong value)
	{
		return new Immediate(value);
	}

	public class PseudoRegister(string identifier) : Operand
	{
		public readonly string Identifier = identifier;

		public override string Emit(AssemblyType type)
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

		public override string Emit(AssemblyType type)
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

		public override string Emit(AssemblyType type)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return $"{CodeEmitter.macos_function_prefix}{Identifier}(%rip)";
			return $"{Identifier}(%rip)";
		}

		public override string ToString()
		{
			return $"Data(\"{Identifier}\")";
		}
	}
	
	public class ConstantAccess(string identifier) : MemoryAccess
	{
		public readonly string Identifier = identifier;

		public override string Emit(AssemblyType type)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return $"L{Identifier}(%rip)";
			return $".L{Identifier}(%rip)";
		}

		public override string ToString()
		{
			return $"Constant(\"{Identifier}\")";
		}
	}
}