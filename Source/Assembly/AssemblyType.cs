namespace FTG.Studios.MCC.Assembly;

public enum AssemblyType { LongWord, QuadWord, Double };

public static class AssemblyTypeExtensions
{
	public static int GetSize(this AssemblyType type)
	{
		return type switch
		{
			AssemblyType.LongWord => 4,
			AssemblyType.QuadWord => 8,
			AssemblyType.Double => 8,
			_ => throw new System.Exception(),
		};
	}
	
	public static string GetSuffix(this AssemblyType type)
	{
		return type switch
		{
			AssemblyType.LongWord => "l",
			AssemblyType.QuadWord => "q",
			AssemblyType.Double => "sd",
			_ => throw new System.Exception(),
		};
	}
	
	public static string GetInitializer(this int size)
	{
		return size switch
		{
			4 => ".long",
			8 => ".quad",
			_ => throw new System.Exception(),
		};
	}
}