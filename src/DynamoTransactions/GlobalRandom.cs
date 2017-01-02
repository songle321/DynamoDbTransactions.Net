//---------------------------------------------------------------------------------------------------------
//	Copyright © 2007 - 2016 Tangible Software Solutions Inc.
//	This class can be used by anyone provided that the copyright notice remains intact.
//
//	This class is used to replace calls to the static java.lang.Math.random method.
//---------------------------------------------------------------------------------------------------------
internal static class GlobalRandom
{
	private static System.Random _randomInstance = null;

	internal static double NextDouble
	{
		get
		{
			if (_randomInstance == null)
				_randomInstance = new System.Random();

			return _randomInstance.NextDouble();
		}
	}
}