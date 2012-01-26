using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Serialization;
using ikvm.extensions;
using IKVM.Internal;
using ikvm.runtime;
using java.net;
using java.util;
//using jpl;
using jpl;
using SbsSW.SwiPlCs.Callback;
using SbsSW.SwiPlCs.Exceptions;
using SbsSW.SwiPlCs.Streams;
using System.Windows.Forms;
using Hashtable = java.util.Hashtable;
using ClassLoader = java.lang.ClassLoader;
using Class = java.lang.Class;
using sun.reflect.misc;
using ArrayList=System.Collections.ArrayList;
using Util = ikvm.runtime.Util;
using CycFort = SbsSW.SwiPlCs.PlTerm;
using PrologCli = SbsSW.SwiPlCs.PrologClient;

namespace SbsSW.SwiPlCs
{
    public partial class PrologClient
    {
        [ThreadStatic] 
        public static bool PreserveObjectType;

        [ThreadStatic]
        static List<List<string>> _locallyTrackedObjects;
        static List<List<string>> LocallyTrackedObjects
        {
            get
            {
                if (_locallyTrackedObjects == null)
                {
                    _locallyTrackedObjects = new List<List<string>>();
                }
                return _locallyTrackedObjects;
            }
        }

        readonly static private Dictionary<object, string> ObjToTag = new Dictionary<object, string>();
        readonly static private Dictionary<string, object> TagToObj = new Dictionary<string, object>();
        public static object tag_to_object(string s)
        {
            if (string.IsNullOrEmpty(s) || s == "void" || !s.StartsWith("C#"))
            {
                Warn("tag_to_object: {0} ", s);
                return null;
            }
            lock (ObjToTag)
            {
                object o;
                if (TagToObj.TryGetValue(s, out o))
                {
                    return o;
                }
                Warn("tag_to_object: {0}", s);
                return jpl.fli.Prolog.tag_to_object(s);
            }
        }
        public static string object_to_tag(object o)
        {
            if (o == null)
            {
                Warn("object_to_tag: NULL");
                return null;
            }

            Type t = o.GetType();
            if (IsStructRecomposable(t) || t.IsPrimitive)
            {
                Debug(string.Format("object_to_tag:{0} from {1}", t, o));
            }

            lock (ObjToTag)
            {
                string s;
                if (ObjToTag.TryGetValue(o, out s))
                {
                    return s;
                }
                GCHandle gch = GCHandle.Alloc(o, GCHandleType.Normal);
                IntPtr iptr = (IntPtr)gch;
                s = "C#" + iptr.ToInt64();
                ObjToTag[o] = s;
                TagToObj[s] = o;
                lock (LocallyTrackedObjects)
                {
                    if (LocallyTrackedObjects.Count > 0)
                    {
                        var tc = LocallyTrackedObjects[0];
                        tc.Add(s);
                    }
                }
                if (ObjToTag.Count % 10000 == 0)
                {
                    Console.WriteLine("ObjToTag=" + ObjToTag);
                }

                return s;
            }
            //return jpl.fli.Prolog.object_to_tag(o);
        }

        private static bool IsTaggedObject(PlTerm info)
        {
            return info.IsCompound && info.Name == "@";
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliToTagged(PlTerm obj, PlTerm str)
        {
            if (!str.IsVar)
            {
                var plvar = PlTerm.PlVar();
                cliToTagged(obj, plvar);
                return SpecialUnify(str, plvar);
            }
            //if (obj.IsString) return str.Unify(obj);
            if (obj.IsVar) return str.Unify(obj);
            object o = GetInstance(obj);
            return UnifyTagged(o, str);
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliImmediateObject(PlTerm valueIn, PlTerm valueOut)
        {
            if (valueIn.IsVar)
            {
                return Warn("Cant find instance {0}", valueIn);
            }
            if (!valueOut.IsVar)
            {
                var plvar = PlTerm.PlVar();
                cliImmediateObject(valueIn, plvar);
                return SpecialUnify(valueOut, plvar);
            }
            object retval = GetInstance(valueIn);
            return valueOut.FromObject(retval);
        }

        public static bool UnifyTagged(object c, PlTerm term2)
        {
            string tag = object_to_tag(c);
            var t1 = term2;
            if (t1.IsCompound)
            {
                t1 = t1[1];
            }
            else if (t1.IsVar)
            {
                return 0 != AddTagged(t1.TermRef, tag);
            }
            //var t2 = new PlTerm(t1.TermRef + 1);

            //libpl.PL_put_atom_chars(t1.TermRef + 1, tag);
            bool ret = t1.UnifyAtom(tag); // = t1;
            return ret;
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliTrackerBegin(PlTerm tracker)
        {
            lock (ObjToTag)
            {
                List<string> newTracking = new List<string>();
                LocallyTrackedObjects.Insert(0, newTracking);
                return UnifyTagged(newTracking, tracker);
            }
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliTrackerFree(PlTerm tracker)
        {
            lock (ObjToTag)
            {
                List<string> tc0 = (List<string>)GetInstance(tracker);
                if (tc0 == null)
                {
                    tc0 = LocallyTrackedObjects[0];
                }
                LocallyTrackedObjects.Remove(tc0);
                foreach (var s in tc0)
                {
                    RemoveTaggedObject(s);
                }
            }
            return true;
        }

        [PrologVisible(ModuleName = ExportModule)]
        static public bool cliFree(PlTerm taggedObject)
        {
            if (taggedObject.IsVar)
            {
                return false;
            }
            string tag;
            if (taggedObject.IsCompound)
            {
                tag = taggedObject[1].Name;
            }
            else if (taggedObject.IsAtom)
            {
                tag = taggedObject.Name;
            }
            else if (taggedObject.IsString)
            {
                tag = taggedObject.Name;
            }
            else
            {
                return true;
            }
            return RemoveTaggedObject(tag);
        }

        private static bool RemoveTaggedObject(string tag)
        {
            lock (TagToObj)
            {
                object obj;
                if (TagToObj.TryGetValue(tag, out obj))
                {
                    TagToObj.Remove(tag);
                    if (obj is IDisposable)
                    {
                        try
                        {
                            ((IDisposable)obj).Dispose();
                        }
                        catch (Exception e)
                        {
                            Warn("Dispose of {0} had problem {1}", obj, e);
                        }
                    }
                    return ObjToTag.Remove(obj);
                }
                return false;
            }
        }
    }
}