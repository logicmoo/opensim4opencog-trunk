// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.motion
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public interface SynchronizedJointConfig
	{
		Schema Schema
		{
			get;
		}
		int jointId
		{
			get;
		}
		string name
		{
			get;
		}
		System.Nullable<double> defaultPosition
		{
			get;
		}
	}
}
