// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.vision
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public partial class ImageRegionListRecord : ISpecificRecord, ImageRegionList
	{
		private static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""ImageRegionListRecord"",""namespace"":""org.robokind.avrogen.vision"",""fields"":[{""name"":""imageSourceId"",""type"":""string""},{""name"":""imageId"",""type"":""long""},{""name"":""imageTimestampMillisecUTC"",""type"":""long""},{""name"":""imageProcessorId"",""type"":""string""},{""name"":""imageRegionsId"",""type"":""long""},{""name"":""processorStartTimestampMillisecUTC"",""type"":""long""},{""name"":""processorCompleteTimestampMillisecUTC"",""type"":""long""},{""name"":""regions"",""type"":{""type"":""array"",""items"":{""type"":""record"",""name"":""ImageRegionRecord"",""namespace"":""org.robokind.avrogen.vision"",""fields"":[{""name"":""regionId"",""type"":""int""},{""name"":""x"",""type"":""int""},{""name"":""y"",""type"":""int""},{""name"":""width"",""type"":""int""},{""name"":""height"",""type"":""int""}]}}}]}");
		private string _imageSourceId;
		private long _imageId;
		private long _imageTimestampMillisecUTC;
		private string _imageProcessorId;
		private long _imageRegionsId;
		private long _processorStartTimestampMillisecUTC;
		private long _processorCompleteTimestampMillisecUTC;
		private IList<org.robokind.avrogen.vision.ImageRegionRecord> _regions;
		public virtual Schema Schema
		{
			get
			{
				return ImageRegionListRecord._SCHEMA;
			}
		}
		public string imageSourceId
		{
			get
			{
				return this._imageSourceId;
			}
			set
			{
				this._imageSourceId = value;
			}
		}
		public long imageId
		{
			get
			{
				return this._imageId;
			}
			set
			{
				this._imageId = value;
			}
		}
		public long imageTimestampMillisecUTC
		{
			get
			{
				return this._imageTimestampMillisecUTC;
			}
			set
			{
				this._imageTimestampMillisecUTC = value;
			}
		}
		public string imageProcessorId
		{
			get
			{
				return this._imageProcessorId;
			}
			set
			{
				this._imageProcessorId = value;
			}
		}
		public long imageRegionsId
		{
			get
			{
				return this._imageRegionsId;
			}
			set
			{
				this._imageRegionsId = value;
			}
		}
		public long processorStartTimestampMillisecUTC
		{
			get
			{
				return this._processorStartTimestampMillisecUTC;
			}
			set
			{
				this._processorStartTimestampMillisecUTC = value;
			}
		}
		public long processorCompleteTimestampMillisecUTC
		{
			get
			{
				return this._processorCompleteTimestampMillisecUTC;
			}
			set
			{
				this._processorCompleteTimestampMillisecUTC = value;
			}
		}
		public IList<org.robokind.avrogen.vision.ImageRegionRecord> regions
		{
			get
			{
				return this._regions;
			}
			set
			{
				this._regions = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.imageSourceId;
			case 1: return this.imageId;
			case 2: return this.imageTimestampMillisecUTC;
			case 3: return this.imageProcessorId;
			case 4: return this.imageRegionsId;
			case 5: return this.processorStartTimestampMillisecUTC;
			case 6: return this.processorCompleteTimestampMillisecUTC;
			case 7: return this.regions;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.imageSourceId = (System.String)fieldValue; break;
			case 1: this.imageId = (System.Int64)fieldValue; break;
			case 2: this.imageTimestampMillisecUTC = (System.Int64)fieldValue; break;
			case 3: this.imageProcessorId = (System.String)fieldValue; break;
			case 4: this.imageRegionsId = (System.Int64)fieldValue; break;
			case 5: this.processorStartTimestampMillisecUTC = (System.Int64)fieldValue; break;
			case 6: this.processorCompleteTimestampMillisecUTC = (System.Int64)fieldValue; break;
			case 7: this.regions = (IList<org.robokind.avrogen.vision.ImageRegionRecord>)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}
