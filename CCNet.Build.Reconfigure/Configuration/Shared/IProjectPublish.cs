﻿namespace CCNet.Build.Reconfigure
{
	public interface IProjectPublish
	{
		string Name { get; }
		string UniqueName { get; }
		string WorkingDirectory { get; }
	}
}
