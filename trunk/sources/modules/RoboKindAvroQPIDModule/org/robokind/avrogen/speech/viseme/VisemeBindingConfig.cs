// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.speech.viseme
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public interface VisemeBindingConfig
	{
		Schema Schema
		{
			get;
		}
		int bindingId
		{
			get;
		}
		IList<org.robokind.avrogen.speech.viseme.VisemePositionRecord> visemeBindings
		{
			get;
		}
	}
}