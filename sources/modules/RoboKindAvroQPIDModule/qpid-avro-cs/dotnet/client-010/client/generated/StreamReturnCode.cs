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
namespace org.apache.qpid.transport
{
   public enum StreamReturnCode : int
   {
    CONTENT_TOO_LARGE= 311,
    NO_ROUTE= 312,
    NO_CONSUMERS= 313
    }

   public struct StreamReturnCodeGetter
   {
    public static StreamReturnCode Get(int value)
    {
        switch (value)
        {
          case 311: return StreamReturnCode.CONTENT_TOO_LARGE;
          case 312: return StreamReturnCode.NO_ROUTE;
          case 313: return StreamReturnCode.NO_CONSUMERS;
        default: throw new Exception("no such value: " + value);
        }
    }
 }
}