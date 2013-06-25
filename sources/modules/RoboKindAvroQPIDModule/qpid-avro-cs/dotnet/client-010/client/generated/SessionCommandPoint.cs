/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 */

using System;
using org.apache.qpid.transport.codec;
using System.Collections.Generic;
using org.apache.qpid.transport.util;
using org.apache.qpid.transport.network;
using System.IO;

namespace org.apache.qpid.transport
{



public sealed class SessionCommandPoint : Method {

    public const int TYPE = 519;

    public override int GetStructType() {
        return TYPE;
    }

    public override int GetSizeWidth() {
        return 0;
    }

    public override int GetPackWidth() {
        return 2;
    }

    public override bool HasPayload() {
        return false;
    }

    public override byte EncodedTrack 
    {
       get{ return Frame.L3; }
       set { throw new NotImplementedException(); }
    }

    private int packing_flags = 0;
    private int _CommandId;
    private long _CommandOffset;


    public SessionCommandPoint() {}


    public SessionCommandPoint(int CommandId, long CommandOffset, params Option[] options) {
        SetCommandId(CommandId);
        SetCommandOffset(CommandOffset);

        for (int i=0; i < options.Length; i++) {
            switch (options[i]) {
            case Option.SYNC: Sync = true; break;
            case Option.BATCH: Batch = true; break;
            case Option.NONE: break;
            default: throw new Exception("invalid option: " + options[i]);
            }
        }

    }

    public override void Dispatch<C>(C context, MethodDelegate<C> mdelegate) {
        mdelegate.SessionCommandPoint(context, this);
    }


    public bool HasCommandId() {
        return (packing_flags & 256) != 0;
    }

    public SessionCommandPoint ClearCommandId() {
        packing_flags = (byte) (packing_flags & ~256);       
        _CommandId =  0;
        Dirty = true;
        return this;
    }

    public int GetCommandId() {
        return _CommandId;
    }

    public SessionCommandPoint SetCommandId(int value) {
        _CommandId = value;
        packing_flags |=  256;
        Dirty = true;
        return this;
    }


    public bool HasCommandOffset() {
        return (packing_flags & 512) != 0;
    }

    public SessionCommandPoint ClearCommandOffset() {
        packing_flags = (byte) (packing_flags & ~512);       
        _CommandOffset =  0;
        Dirty = true;
        return this;
    }

    public long GetCommandOffset() {
        return _CommandOffset;
    }

    public SessionCommandPoint SetCommandOffset(long value) {
        _CommandOffset = value;
        packing_flags |=  512;
        Dirty = true;
        return this;
    }





    public override void Write(IEncoder enc)
    {
        enc.WriteUint16(packing_flags);
        if ((packing_flags & 256) != 0)
            enc.WriteSequenceNo(_CommandId);
        if ((packing_flags & 512) != 0)
            enc.WriteUint64(_CommandOffset);

    }

    public override void Read(IDecoder dec)
    {
        packing_flags = (int) dec.ReadUint16();
        if ((packing_flags & 256) != 0)
            _CommandId = dec.ReadSequenceNo();
        if ((packing_flags & 512) != 0)
            _CommandOffset = dec.ReadUint64();

    }

    public override Dictionary<String,Object> Fields
    {
		get
		{
			Dictionary<String,Object> result = new Dictionary<String,Object>();

        	if ((packing_flags & 256) != 0)
            	result.Add("_CommandId", GetCommandId());
        	if ((packing_flags & 512) != 0)
            	result.Add("_CommandOffset", GetCommandOffset());

			return result;
        }
    }

}
}