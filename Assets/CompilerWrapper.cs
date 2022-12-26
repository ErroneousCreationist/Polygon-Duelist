using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.CSharp;

public class CompilerWrapper
{
    private Evaluator _evaluator;
    private CompilerContext _context;
    private StringBuilder _report;

    public int ErrorsCount { get { return _context.Report.Printer.ErrorsCount; } }
    public int WarningsCount { get { return _context.Report.Printer.WarningsCount; } }
    public string GetReport() { return _report.ToString(); }

    public CompilerWrapper()
    {
        // create new settings that will *not* load up all of standard lib by default
        // see: https://github.com/mono/mono/blob/master/mcs/mcs/settings.cs

        CompilerSettings settings = new CompilerSettings //dont import all types
        { LoadDefaultReferences = false, StdLib = false };
        this._report = new StringBuilder();
        this._context = new CompilerContext(settings,
          new StreamReportPrinter(new StringWriter(_report)));

        this._evaluator = new Evaluator(_context);
        this._evaluator.ReferenceAssembly(Assembly.GetExecutingAssembly());

        ImportAllowedTypes(BuiltInTypes, AdditionalTypes, QuestionableTypes);
    }

    private void ImportAllowedTypes(params Type[][] allowedTypeArrays)
    {
        // expose Evaluator.importer and Evaluator.module
        var evtype = typeof(Evaluator);
        var importer = (ReflectionImporter)evtype
            .GetField("importer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_evaluator);
        var module = (ModuleContainer)evtype
            .GetField("module", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_evaluator);

        // expose MetadataImporter.ImportTypes(Type[], RootNamespace, bool)
        var importTypes = importer.GetType().GetMethod(
           "ImportTypes", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any,
           new Type[] { typeof(Type[]), typeof(Namespace), typeof(bool) }, null);

        foreach (Type[] types in allowedTypeArrays)
        {
            importer.ImportTypes(types, module.GlobalRootNamespace, false );
        }
    }

    /// <summary> Creates new instances of types that are children of the specified type. </summary>
    public IEnumerable<T> CreateInstancesOf<T>()
    {
        var parent = typeof(T);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies.SelectMany(assembly => {
            return assembly.GetTypes().Where(type => {
                return !(type.IsAbstract || type.IsInterface) && parent.IsAssignableFrom(type);
            });
        });
        return types.Select(type => (T)Activator.CreateInstance(type));
    }

    /// Loads user code. Returns true on successful evaluation, or false on errors.
    public bool Execute(string path)
    {
        _report.Length = 0;
        var code = File.ReadAllText(path);
        return _evaluator.Run(code);
    }

    /// Basic built-in system types
    private static Type[] BuiltInTypes = new Type[] { //all the important cool types which we really need
        typeof(void),
        typeof(System.Type),
        typeof(System.Object),
        typeof(System.ValueType),
        typeof(System.Array),
        typeof(UnityEngine.GameObject),
        typeof(UnityEngine.Color),
        typeof(UnityEngine.AssetBundle),
        typeof(UnityEngine.AssetBundleCreateRequest),
        typeof(UnityEngine.Application),
        typeof(UnityEngine.SystemInfo),
        typeof(UnityEngine.Camera),
        typeof(Test_DamageTrigger),
        typeof(LocalGamemodeController),
        typeof(GamemodeManager),
        typeof(bossAI_Fish),
        typeof(BossAI_Satan),
        typeof(BossAI_StrangeThing),
        typeof(BossAI_Supercannon),
        typeof(BossAI_Worm),
        typeof(DeleteWithoutChildren),
        typeof(MineAI),
        typeof(TurretAI),
        typeof(BossAI_StrangeThing),
        typeof(BuildingHealth),
        typeof(ModularTrigger),
        typeof(PlayerMovement),
        typeof(GamemodeManager),
        typeof(TeamStatus),
        typeof(GameModeEnum),
        typeof(NetworkManagerUI),
        typeof(MissileTargetTracker),
        typeof(Projectile),
        typeof(NetworkManagerUI),
        typeof(System.Linq.Enumerable),
        typeof(Unity.Netcode.NetworkBehaviour),
        typeof(Unity.Netcode.NetworkObject),
        typeof(Unity.Netcode.NetworkClient),
        typeof(Unity.Netcode.NetworkManager),
        typeof(Unity.Netcode.NetworkTransport),
        typeof(Unity.Netcode.NetworkTimeSystem),
        typeof(Unity.Netcode.NetworkVariable<>),
        typeof(Unity.Netcode.Components.NetworkRigidbody2D),
        typeof(Unity.Netcode.Components.NetworkTransform),
        typeof(Unity.Netcode.Components.NetworkAnimator),
        typeof(UnityEngine.Animator),
        typeof(UnityEngine.Experimental.Rendering.Universal.Light2D),
        typeof(FileStream),
        typeof(UnityEngine.KeyCode),
        typeof(UnityEngine.Input),
        typeof(Targeted),
        typeof(ChatController),
        typeof(UnityEngine.Collider2D),
        typeof(UnityEngine.Rigidbody2D),
        typeof(UnityEngine.SpriteRenderer),
        typeof(UnityEngine.Sprite),
        typeof(UnityEngine.UI.Image),
        typeof(UnityEngine.UI.Slider),
        typeof(TMPro.TMP_Text),
        typeof(UnityEngine.UI.Button),
        typeof(UnityEngine.UI.InputField),
        typeof(TMPro.TMP_InputField),
        typeof(UnityEngine.Transform),
        typeof(UnityEngine.Vector2),
        typeof(UnityEngine.Vector3),
        typeof(UnityEngine.RectTransform),
        typeof(UnityEngine.Canvas),
        typeof(UnityEngine.CanvasGroup),
        typeof(System.SByte),
        typeof(System.Byte),
        typeof(System.Int16),
        typeof(System.UInt16),
        typeof(System.Int32),
        typeof(System.UInt32),
        typeof(System.Int64),
        typeof(System.UInt64),
        typeof(System.Single),
        typeof(System.Double),
        typeof(System.Char),
        typeof(System.String),
        typeof(System.Boolean),
        typeof(System.Decimal),
        typeof(System.IntPtr),
        typeof(System.UIntPtr),
        typeof(System.Enum),
        typeof(System.Attribute),
        typeof(System.Delegate),
        typeof(System.MulticastDelegate),
        typeof(System.IDisposable),
        typeof(System.Exception),
        typeof(System.RuntimeFieldHandle),
        typeof(System.RuntimeTypeHandle),
        typeof(System.ParamArrayAttribute),
        typeof(System.Runtime.InteropServices.OutAttribute),
    };

    //not as useful but still good to have types
    private static Type[] AdditionalTypes = new Type[] { //some important stuff but not ass useful (random is super cool tho why isnt that important)

        // mscorlib System

        typeof(System.Action),
        typeof(System.Action<>),
        typeof(System.Action<,>),
        typeof(System.Action<,,>),
        typeof(System.Action<,,,>),
        typeof(System.ArgumentException),
        typeof(System.ArgumentNullException),
        typeof(System.ArgumentOutOfRangeException),
        typeof(System.ArithmeticException),
        typeof(System.ArraySegment<>),
        typeof(System.ArrayTypeMismatchException),
        typeof(System.Comparison<>),
        typeof(System.Convert),
        typeof(System.Converter<,>),
        typeof(System.DivideByZeroException),
        typeof(System.FlagsAttribute),
        typeof(System.FormatException),
        typeof(System.Func<>),
        typeof(System.Func<,>),
        typeof(System.Func<,,>),
        typeof(System.Func<,,,>),
        typeof(System.Func<,,,,>),
        typeof(System.Guid),
        typeof(System.IAsyncResult),
        typeof(System.ICloneable),
        typeof(System.IComparable),
        typeof(System.IComparable<>),
        typeof(System.IConvertible),
        typeof(System.ICustomFormatter),
        typeof(System.IEquatable<>),
        typeof(System.IFormatProvider),
        typeof(System.IFormattable),
        typeof(System.IndexOutOfRangeException),
        typeof(System.InvalidCastException),
        typeof(System.InvalidOperationException),
        typeof(System.InvalidTimeZoneException),
        typeof(System.Math),
        typeof(System.MidpointRounding),
        typeof(System.NonSerializedAttribute),
        typeof(System.NotFiniteNumberException),
        typeof(System.NotImplementedException),
        typeof(System.NotSupportedException),
        typeof(System.Nullable),
        typeof(System.Nullable<>),
        typeof(System.NullReferenceException),
        typeof(System.ObjectDisposedException),
        typeof(System.ObsoleteAttribute),
        typeof(System.OverflowException),
        typeof(System.Predicate<>),
        typeof(System.Random),
        typeof(System.RankException),
        typeof(System.SerializableAttribute),
        typeof(System.StackOverflowException),
        typeof(System.StringComparer),
        typeof(System.StringComparison),
        typeof(System.StringSplitOptions),
        typeof(System.SystemException),
        typeof(System.TimeoutException),
        typeof(System.TypeCode),
        typeof(System.Version),
        typeof(System.WeakReference),
        
        // mscorlib System.Collections
        
        typeof(System.Collections.BitArray),
        typeof(System.Collections.ICollection),
        typeof(System.Collections.IComparer),
        typeof(System.Collections.IDictionary),
        typeof(System.Collections.IDictionaryEnumerator),
        typeof(System.Collections.IEqualityComparer),
        typeof(System.Collections.IList),

        // mscorlib System.Collections.Generic

        typeof(System.Collections.IEnumerator),
        typeof(System.Collections.IEnumerable),
        typeof(System.Collections.Generic.Comparer<>),
        typeof(System.Collections.Generic.Dictionary<,>),
        typeof(System.Collections.Generic.EqualityComparer<>),
        typeof(System.Collections.Generic.ICollection<>),
        typeof(System.Collections.Generic.IComparer<>),
        typeof(System.Collections.Generic.IDictionary<,>),
        typeof(System.Collections.Generic.IEnumerable<>),
        typeof(System.Collections.Generic.IEnumerator<>),
        typeof(System.Collections.Generic.IEqualityComparer<>),
        typeof(System.Collections.Generic.IList<>),
        typeof(System.Collections.Generic.KeyNotFoundException),
        typeof(System.Collections.Generic.KeyValuePair<,>),
        typeof(System.Collections.Generic.List<>),
        
        // mscorlib System.Collections.ObjectModel

        typeof(System.Collections.ObjectModel.Collection<>),
        typeof(System.Collections.ObjectModel.KeyedCollection<,>),
        typeof(System.Collections.ObjectModel.ReadOnlyCollection<>),

        // System System.Collections.Generic

        typeof(System.Collections.Generic.LinkedList<>),
        typeof(System.Collections.Generic.LinkedListNode<>),
        typeof(System.Collections.Generic.Queue<>),
        typeof(System.Collections.Generic.SortedDictionary<,>),
        typeof(System.Collections.Generic.SortedList<,>),
        typeof(System.Collections.Generic.Stack<>),

        // System System.Collections.Specialized

        typeof(System.Collections.Specialized.BitVector32),

        // System.Core System.Collections.Generic

        typeof(System.Collections.Generic.HashSet<>),

        // System.Core System.Linq

        typeof(System.Linq.IGrouping<,>),
        typeof(System.Linq.ILookup<,>),
        typeof(System.Linq.IOrderedEnumerable<>),
        typeof(System.Linq.IOrderedQueryable),
        typeof(System.Linq.IOrderedQueryable<>),
        typeof(System.Linq.IQueryable),
        typeof(System.Linq.IQueryable<>),
        typeof(System.Linq.IQueryProvider),
        typeof(System.Linq.Lookup<,>),
        typeof(System.Linq.Queryable),
        
        // UnityEngine
        typeof(UnityEngine.Random),
        typeof(UnityEngine.Debug),
    };

    //questionable types that might be dangerous
    private static Type[] QuestionableTypes = new Type[] {
        
        //// mscorlib System
        
        //typeof(System.AsyncCallback),
        //typeof(System.BitConverter),
        //typeof(System.Buffer),
        typeof(System.DateTime), //idk datetime might be useful
        typeof(System.DateTimeKind),
        typeof(System.DateTimeOffset),
        typeof(System.DayOfWeek),
        typeof(System.EventArgs),
        typeof(System.EventHandler),
        typeof(System.EventHandler<>),
        typeof(System.TimeSpan),
        typeof(System.TimeZone),
        typeof(System.TimeZoneInfo),
        typeof(System.TimeZoneNotFoundException),

        //// mscorlib System.IO FILE ACCESS IS DANGEROUS SO IT IS BANNED
        
        //typeof(System.IO.BinaryReader),
        //typeof(System.IO.BinaryWriter),
        //typeof(System.IO.BufferedStream),
        //typeof(System.IO.EndOfStreamException),
        //typeof(System.IO.FileAccess),
        //typeof(System.IO.FileMode),
        //typeof(System.IO.FileNotFoundException),
        //typeof(System.IO.IOException),
        //typeof(System.IO.MemoryStream),
        //typeof(System.IO.Path),
        //typeof(System.IO.PathTooLongException),
        //typeof(System.IO.SeekOrigin),
        //typeof(System.IO.Stream),
        //typeof(System.IO.StringReader),
        //typeof(System.IO.StringWriter),
        //typeof(System.IO.TextReader),
        //typeof(System.IO.File),

        //// mscorlib System.Text
         
        typeof(System.Text.ASCIIEncoding), //binary encoding might be useful
        typeof(System.Text.Decoder),
        typeof(System.Text.Encoder),
        typeof(System.Text.Encoding),
        typeof(System.Text.EncodingInfo),
        typeof(System.Text.StringBuilder),
        typeof(System.Text.UnicodeEncoding),
        typeof(System.Text.UTF32Encoding),
        typeof(System.Text.UTF7Encoding),
        typeof(System.Text.UTF8Encoding),

        //// mscorlib System.Globalization
        
        //typeof(System.Globalization.CharUnicodeInfo),
        //typeof(System.Globalization.CultureInfo),
        //typeof(System.Globalization.DateTimeFormatInfo),
        //typeof(System.Globalization.DateTimeStyles),
        //typeof(System.Globalization.NumberFormatInfo),
        //typeof(System.Globalization.NumberStyles),
        //typeof(System.Globalization.RegionInfo),
        //typeof(System.Globalization.StringInfo),
        //typeof(System.Globalization.TextElementEnumerator),
        //typeof(System.Globalization.TextInfo),
        //typeof(System.Globalization.UnicodeCategory),
       
        //// System System.IO.Compression
        
        //typeof(System.IO.Compression.CompressionMode),
        //typeof(System.IO.Compression.DeflateStream),
        //typeof(System.IO.Compression.GZipStream),
        
        //// System System.Text.RegularExpressions

        //typeof(System.Text.RegularExpressions.Capture),
        //typeof(System.Text.RegularExpressions.CaptureCollection),
        //typeof(System.Text.RegularExpressions.Group),
        //typeof(System.Text.RegularExpressions.GroupCollection),
        //typeof(System.Text.RegularExpressions.Match),
        //typeof(System.Text.RegularExpressions.MatchCollection),
        //typeof(System.Text.RegularExpressions.MatchEvaluator),
        //typeof(System.Text.RegularExpressions.Regex),
        //typeof(System.Text.RegularExpressions.RegexCompilationInfo),
        //typeof(System.Text.RegularExpressions.RegexOptions),

    };
}