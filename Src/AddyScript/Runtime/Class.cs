using System.Collections.Generic;

using AddyScript.Ast.Expressions;
using AddyScript.Ast.Statements;
using AddyScript.Runtime.Dynamics;
using AddyScript.Runtime.Frames;


namespace AddyScript.Runtime
{
    /// <summary>
    /// Represents the definition of a class.
    /// </summary>
    public class Class : IFrameItem
    {
        #region Predefined classes

        #region Primitive types

        /// <summary>
        /// Maps the <b>void</b> primitive type.
        /// </summary>
        public static readonly Class Void = new Class(ClassID.Void, "void", Modifier.Final);

        /// <summary>
        /// Maps the <b>bool</b> primitive type.
        /// </summary>
        public static readonly Class Boolean = new Class(ClassID.Boolean, "bool", Modifier.Final);

        /// <summary>
        /// Maps the <b>int</b> primitive type.
        /// </summary>
        public static readonly Class Integer = new Class(ClassID.Integer, "int", Modifier.Final);

        /// <summary>
        /// Maps the <b>long</b> primitive type.
        /// </summary>
        public static readonly Class Long = new Class(ClassID.Long, "long", Modifier.Final);

        /// <summary>
        /// Maps the <b>rational</b> primitive type.
        /// </summary>
        public static readonly Class Rational = new Class(ClassID.Rational, "rational", Modifier.Final);

        /// <summary>
        /// Maps the <b>float</b> primitive type.
        /// </summary>
        public static readonly Class Float = new Class(ClassID.Float, "float", Modifier.Final);

        /// <summary>
        /// Maps the <b>decimal</b> primitive type.
        /// </summary>
        public static readonly Class Decimal = new Class(ClassID.Decimal, "decimal", Modifier.Final);

        /// <summary>
        /// Maps the <b>complex</b> primitive type.
        /// </summary>
        public static readonly Class Complex = new Class(ClassID.Complex, "complex", Modifier.Final);

        /// <summary>
        /// Maps the <b>date</b> primitive type.
        /// </summary>
        public static readonly Class Date = new Class(ClassID.Date, "date", Modifier.Final);

        /// <summary>
        /// Maps the <b>string</b> primitive type.
        /// </summary>
        public static readonly Class String = new Class(ClassID.String, "string", Modifier.Final);

        /// <summary>
        /// Maps the <b>list</b> primitive type.
        /// </summary>
        public static readonly Class List = new Class(ClassID.List, "list", Modifier.Final);

        /// <summary>
        /// Maps the <b>map</b> primitive type.
        /// </summary>
        public static readonly Class Map = new Class(ClassID.Map, "map", Modifier.Final);

        /// <summary>
        /// Maps the <b>set</b> primitive type.
        /// </summary>
        public static readonly Class Set = new Class(ClassID.Set, "set", Modifier.Final);

        /// <summary>
        /// Maps the <b>queue</b> primitive type.
        /// </summary>
        public static readonly Class Queue = new Class(ClassID.Queue, "queue", Modifier.Final);

        /// <summary>
        /// Maps the <b>stack</b> primitive type.
        /// </summary>
        public static readonly Class Stack = new Class(ClassID.Stack, "stack", Modifier.Final);

        /// <summary>
        /// Maps the <b>object</b> primitive type. This is the base class of all user defined classes.
        /// </summary>
        public static readonly Class Object = new Class(ClassID.Object, "object", Modifier.Default);

        /// <summary>
        /// Maps the <b>resource</b> primitive type.
        /// </summary>
        public static readonly Class Resource = new Class(ClassID.Resource, "resource", Modifier.Final);

        /// <summary>
        /// Maps the <b>closure</b> primitive type.
        /// </summary>
        public static readonly Class Closure = new Class(ClassID.Closure, "closure", Modifier.Final);

        #endregion

        #region Exception

        /// <summary>
        /// Exception type.
        /// </summary>
        public static readonly Class Exception =
            new Class(Object, "Exception", Modifier.Default,
                      GetExceptionConstructor(),
                      GetExceptionFields(),
                      GetExceptionProperties(),
                      GetExceptionMethods(),
                      GetExceptionEvents());

        #endregion

        #region Reflection

        /// <summary>
        /// TypeInfo type.
        /// </summary>
        public static readonly Class TypeInfo =
            new Class(Object, "TypeInfo", Modifier.Final,
                      CreateDefaultConstructor("TypeInfo", Scope.Private),
                      GetTypeInfoFields(),
                      GetTypeInfoProperties(),
                      GetTypeInfoMethods(),
                      GetTypeInfoEvents());

        /// <summary>
        /// MemberInfo type.
        /// </summary>
        public static readonly Class MemberInfo =
            new Class(Object, "MemberInfo", Modifier.Default,
                      CreateDefaultConstructor("MemberInfo", Scope.Private),
                      GetMemberInfoFields(),
                      GetMemberInfoProperties(),
                      GetMemberInfoMethods(),
                      GetMemberInfoEvents());

        /// <summary>
        /// FieldInfo type.
        /// </summary>
        public static readonly Class FieldInfo =
            new Class(MemberInfo, "FieldInfo", Modifier.Final,
                      CreateDefaultConstructor("FieldInfo", Scope.Private),
                      GetFieldInfoFields(),
                      GetFieldInfoProperties(),
                      GetFieldInfoMethods(),
                      GetFieldInfoEvents());

        /// <summary>
        /// PropertyInfo type.
        /// </summary>
        public static readonly Class PropertyInfo =
            new Class(MemberInfo, "PropertyInfo", Modifier.Final,
                      CreateDefaultConstructor("PropertyInfo", Scope.Private),
                      GetPropertyInfoFields(),
                      GetPropertyInfoProperties(),
                      GetPropertyInfoMethods(),
                      GetPropertyInfoEvents());

        /// <summary>
        /// MethodInfo type.
        /// </summary>
        public static readonly Class MethodInfo =
            new Class(MemberInfo, "MethodInfo", Modifier.Final,
                      CreateDefaultConstructor("MethodInfo", Scope.Private),
                      GetMethodInfoFields(),
                      GetMethodInfoProperties(),
                      GetMethodInfoMethods(),
                      GetMethodInfoEvents());

        /// <summary>
        /// EventInfo type.
        /// </summary>
        public static readonly Class EventInfo =
            new Class(MemberInfo, "EventInfo", Modifier.Final,
                      CreateDefaultConstructor("EventInfo", Scope.Private),
                      GetEventInfoFields(),
                      GetEventInfoProperties(),
                      GetEventInfoMethods(),
                      GetEventInfoEvents());

        /// <summary>
        /// ParameterInfo type.
        /// </summary>
        public static readonly Class ParameterInfo =
            new Class(Object, "ParameterInfo", Modifier.Final,
                      CreateDefaultConstructor("ParameterInfo", Scope.Private),
                      GetParameterInfoFields(),
                      GetParameterInfoProperties(),
                      GetParameterInfoMethods(),
                      GetParameterInfoEvents());

        #endregion

        /// <summary>
        /// The list of predefined classes.
        /// </summary>
        public static readonly List<Class> Predefined;

        #endregion

        #region Class Initializer

        /// <summary>
        /// Registers predefined classes and defines iteration methods.
        /// </summary>
        static Class()
        {
            Predefined = new List<Class>
                             {
                                 Void,
                                 Boolean,
                                 Integer,
                                 Long,
                                 Rational,
                                 Float,
                                 Decimal,
                                 Complex,
                                 Date,
                                 String,
                                 List,
                                 Map,
                                 Set,
                                 Queue,
                                 Stack,
                                 Object,
                                 Resource,
                                 Closure,
                                 Exception,
                                 TypeInfo,
                                 MemberInfo,
                                 FieldInfo,
                                 PropertyInfo,
                                 MethodInfo,
                                 EventInfo,
                                 ParameterInfo
                             };

            // Create the int::times and long::times method
            var timesFunction = new Function(new[] { new Parameter("__action") },
                                             new Block(new ForLoop(new Statement[] { VariableDecl.Single("__index", new Literal(new Integer(0))) },
                                                                   new BinaryExpression(BinaryOperator.LessThan, new VariableRef("__index"), new ThisReference()),
                                                                   new Expression[] { new UnaryExpression(UnaryOperator.PreIncrement, new VariableRef("__index")) },
                                                                   new FunctionCall("__action", new VariableRef("__index"))),
                                                       new Return(new ThisReference())));

            Integer.RegisterMethod(new ClassMethod("times", Scope.Public, Modifier.Final, timesFunction));
            Long.RegisterMethod(new ClassMethod("times", Scope.Public, Modifier.Final, timesFunction));

            // Create the string::each, list::each, set::each, queue::each and stack::each methods
            var eachFunction = new Function(new[] { new Parameter("__action") },
                                            new Block(new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                      "__value",
                                                                      new ThisReference(),
                                                                      new FunctionCall("__action", new VariableRef("__value"))),
                                                      new Return(new ThisReference())));

            String.RegisterMethod(new ClassMethod("each", Scope.Public, Modifier.Final, eachFunction));
            List.RegisterMethod(new ClassMethod("each", Scope.Public, Modifier.Final, eachFunction));
            Set.RegisterMethod(new ClassMethod("each", Scope.Public, Modifier.Final, eachFunction));
            Queue.RegisterMethod(new ClassMethod("each", Scope.Public, Modifier.Final, eachFunction));
            Stack.RegisterMethod(new ClassMethod("each", Scope.Public, Modifier.Final, eachFunction));

            // Create the list::eachIndex methods
            var eachIndexFunction = new Function(new[] { new Parameter("__action") },
                                                         new Block(new ForLoop(new Statement[] { VariableDecl.Single("__i", new Literal(new Integer(0))) },
                                                                               new BinaryExpression(BinaryOperator.LessThan, new VariableRef("__i"), MethodCall.OfThis("count")),
                                                                               new Expression[] { new UnaryExpression(UnaryOperator.PreIncrement, new VariableRef("__i")) },
                                                                               new FunctionCall("__action", new VariableRef("__i"))),
                                                                   new Return(new ThisReference())));

            List.RegisterMethod(new ClassMethod("eachIndex", Scope.Public, Modifier.Final, eachIndexFunction));

            // Create the map::each method
            var mapEachFunction = new Function(new[] { new Parameter("__action") },
                                               new Block(new ForEachLoop("__key",
                                                                         "__value",
                                                                         new ThisReference(),
                                                                         new FunctionCall("__action",
                                                                                          new VariableRef("__key"),
                                                                                          new VariableRef("__value"))),
                                                         new Return(new ThisReference())));

            Map.RegisterMethod(new ClassMethod("each", Scope.Public, Modifier.Final, mapEachFunction));

            // Create the map::eachKey method
            var mapEachKeyFunction = new Function(new[] { new Parameter("__action") },
                                                  new Block(new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                            "__value",
                                                                            PropertyRef.This("keys"),
                                                                            new FunctionCall("__action", new VariableRef("__value"))),
                                                            new Return(new ThisReference())));

            Map.RegisterMethod(new ClassMethod("eachKey", Scope.Public, Modifier.Final, mapEachKeyFunction));

            // Create the map::eachValue method
            var mapEachValueFunction = new Function(new[] { new Parameter("__action") },
                                                    new Block(new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                              "__value",
                                                                              PropertyRef.This("values"),
                                                                              new FunctionCall("__action", new VariableRef("__value"))),
                                                              new Return(new ThisReference())));

            Map.RegisterMethod(new ClassMethod("eachValue", Scope.Public, Modifier.Final, mapEachValueFunction));

            // Create the list::where method
            var lstWhereFunction = new Function(new[] { new Parameter("__predicate") },
                                                new Block(VariableDecl.Single("l", new ListInitializer()),
                                                          new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                          "__value",
                                                                          new ThisReference(),
                                                                          new IfThenElse(new FunctionCall("__predicate", new VariableRef("__value")),
                                                                                         new MethodCall(new VariableRef("l"), "add", new VariableRef("__value")))),
                                                          new Return(new VariableRef("l"))));

            List.RegisterMethod(new ClassMethod("where", Scope.Public, Modifier.Final, lstWhereFunction));

            // Create the set::where method
            var setWhereFunction = new Function(new[] { new Parameter("__predicate") },
                                                new Block(VariableDecl.Single("s", new SetInitializer()),
                                                          new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                          "__value",
                                                                          new ThisReference(),
                                                                          new IfThenElse(new FunctionCall("__predicate", new VariableRef("__value")),
                                                                                         new MethodCall(new VariableRef("s"), "add", new VariableRef("__value")))),
                                                          new Return(new VariableRef("s"))));

            Set.RegisterMethod(new ClassMethod("where", Scope.Public, Modifier.Final, setWhereFunction));

            // Create the list::all and set::all methods
            var allFunction = new Function(new[] { new Parameter("__predicate") },
                                           new Block(new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                     "__value",
                                                                     new ThisReference(),
                                                                     new IfThenElse(new UnaryExpression(UnaryOperator.Not, new FunctionCall("__predicate", new VariableRef("__value"))),
                                                                                    new Return(new Literal(Dynamics.Boolean.False)))),
                                                     new Return(new Literal(Dynamics.Boolean.True))));

            List.RegisterMethod(new ClassMethod("all", Scope.Public, Modifier.Final, allFunction));
            Set.RegisterMethod(new ClassMethod("all", Scope.Public, Modifier.Final, allFunction));

            // Create the list::any and set::any methods
            var anyFunction = new Function(new[] { new Parameter("__predicate") },
                                           new Block(new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                     "__value",
                                                                     new ThisReference(),
                                                                     new IfThenElse(new FunctionCall("__predicate", new VariableRef("__value")),
                                                                                    new Return(new Literal(Dynamics.Boolean.True)))),
                                                     new Return(new Literal(Dynamics.Boolean.False))));

            List.RegisterMethod(new ClassMethod("any", Scope.Public, Modifier.Final, anyFunction));
            Set.RegisterMethod(new ClassMethod("any", Scope.Public, Modifier.Final, anyFunction));

            // Create the list::first and set::first methods
            var firstFunction = new Function(new[] { new Parameter("__predicate") },
                                             new Block(new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                       "__value",
                                                                       new ThisReference(),
                                                                       new IfThenElse(new FunctionCall("__predicate", new VariableRef("__value")),
                                                                                      new Return(new VariableRef("__value")))),
                                                       new Return(new Literal())));

            List.RegisterMethod(new ClassMethod("first", Scope.Public, Modifier.Final, firstFunction));
            Set.RegisterMethod(new ClassMethod("first", Scope.Public, Modifier.Final, firstFunction));

            // Create the list::select method
            var lstSelectFunction = new Function(new[] { new Parameter("__selector") },
                                                 new Block(VariableDecl.Single("l", new ListInitializer()),
                                                           new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                           "__value",
                                                                           new ThisReference(),
                                                                           new MethodCall(new VariableRef("l"),
                                                                                          "add",
                                                                                          new FunctionCall("__selector", new VariableRef("__value")))),
                                                           new Return(new VariableRef("l"))));

            List.RegisterMethod(new ClassMethod("select", Scope.Public, Modifier.Final, lstSelectFunction));

            // Create the set::select method
            var setSelectFunction = new Function(new[] { new Parameter("__selector") },
                                                 new Block(VariableDecl.Single("s", new SetInitializer()),
                                                           new ForEachLoop(ForEachLoop.DEFAULT_KEY_NAME,
                                                                           "__value",
                                                                           new ThisReference(),
                                                                           new MethodCall(new VariableRef("s"),
                                                                                          "add",
                                                                                          new FunctionCall("__selector", new VariableRef("__value")))),
                                                           new Return(new VariableRef("s"))));

            Set.RegisterMethod(new ClassMethod("select", Scope.Public, Modifier.Final, setSelectFunction));
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Class.
        /// </summary>
        /// <param name="classID">The class identifier</param>
        /// <param name="name">The name of this class</param>
        /// <param name="modifier">Determines the way this class supports inheritance</param>
        /// <param name="superClass">The superclass of this one</param>
        /// <param name="constructor">The constructor of this class</param>
        /// <param name="fields">The fields of this class</param>
        /// <param name="properties">The properties of this class</param>
        /// <param name="methods">The methods of this class</param>
        /// <param name="events">The events of this class</param>
        private Class(ClassID classID, string name, Modifier modifier,
                      Class superClass, ClassMethod constructor,
                      IEnumerable<ClassField> fields,
                      IEnumerable<ClassProperty> properties,
                      IEnumerable<ClassMethod> methods,
                      IEnumerable<ClassEvent> events)
        {
            ClassID = classID;
            Name = name;
            Modifier = modifier;
            SuperClass = superClass;

            Fields = new ClassMemberSet<ClassField>();
            Properties = new ClassMemberSet<ClassProperty>();
            Methods = new ClassMemberSet<ClassMethod>();
            Events = new ClassMemberSet<ClassEvent>();

            RegisterConstructor(constructor);
            RegisterFields(fields);
            RegisterProperties(properties);
            RegisterMethods(methods);
            RegisterEvents(events);

            GenerateReflector();
        }

        /// <summary>
        /// Initializes a new instance of Class.
        /// </summary>
        /// <param name="classID">The class identifier</param>
        /// <param name="name">The name of this class</param>
        /// <param name="modifier">Determines the way this class supports inheritance</param>
        private Class(ClassID classID, string name, Modifier modifier)
            : this(classID, name, modifier, null,
                   CreateDefaultConstructor(name, Scope.Private),
                   null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of Class.
        /// </summary>
        /// <param name="superClass">The superclass of this one</param>
        /// <param name="name">The name of this class</param>
        /// <param name="modifier">Determines the way this class supports inheritance</param>
        /// <param name="constructor">The constructor of this class</param>
        /// <param name="fields">The fields of this class</param>
        /// <param name="properties">The properties of this class</param>
        /// <param name="methods">The methods of this class</param>
        /// <param name="events">The events of this class</param>
        public Class(Class superClass, string name, Modifier modifier,
                     ClassMethod constructor,
                     IEnumerable<ClassField> fields,
                     IEnumerable<ClassProperty> properties,
                     IEnumerable<ClassMethod> methods,
                     IEnumerable<ClassEvent> events)
            : this(superClass.ClassID, name, modifier, superClass,
                   constructor, fields, properties, methods, events)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the kind of frame's item a class is.
        /// </summary>
        public FrameItemKind Kind
        {
            get { return FrameItemKind.Class; }
        }

        /// <summary>
        /// A member of the <see cref="Runtime.ClassID"/> that quickly identifies the class.
        /// </summary>
        public ClassID ClassID { get; private set; }

        /// <summary>
        /// The class's canonical name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// One between <b>abstract</b>, <b>final</b> and <b>static</b>.
        /// </summary>
        public Modifier Modifier { get; private set; }

        /// <summary>
        /// The superclass of the calling one.
        /// </summary>
        public Class SuperClass { get; private set; }

        /// <summary>
        /// The class's constructor.
        /// </summary>
        public ClassMethod Constructor { get; private set; }

        /// <summary>
        /// The class's fields.
        /// </summary>
        public ClassMemberSet<ClassField> Fields { get; private set; }

        /// <summary>
        /// The class's properties.
        /// </summary>
        public ClassMemberSet<ClassProperty> Properties { get; private set; }

        /// <summary>
        /// The class's methods.
        /// </summary>
        public ClassMemberSet<ClassMethod> Methods { get; private set; }

        /// <summary>
        /// The class's events.
        /// </summary>
        public ClassMemberSet<ClassEvent> Events { get; private set; }

        /// <summary>
        /// The attributes of the class.
        /// </summary>
        public Attribute[] Attributes { get; set; }

        #endregion

        #region Static utility methods

        /// <summary>
        /// Generates a default constructor for a class.
        /// </summary>
        /// <param name="className">The name of the owning class</param>
        /// <param name="scope">The scope of the constructor</param>
        /// <returns>A <see cref="ClassMethod"/></returns>
        private static ClassMethod CreateDefaultConstructor(string className, Scope scope)
        {
            return new ClassMethod(className, scope, Modifier.Default, Function.Empty);
        }

        /// <summary>
        /// Generates the logic of the <i>TypeInfo::__read_superType</i> and  <i>MemberInfo::__read_definer</i> methods.
        /// </summary>
        /// <param name="fieldName">The name of the field that holds the class's name</param>
        /// <returns>A <see cref="Function"/></returns>
        public static Function CreateTypeInfoEvaluator(string fieldName)
        {
            return new Function(Parameter.EmptyArray,
                                new Block(VariableDecl.Single("__super", PropertyRef.This(fieldName)),
                                          new IfThenElse(new BinaryExpression(BinaryOperator.Equal, new VariableRef("__super"), new Literal()),
                                                         new Return(new Literal())),
                                          new Return(new FunctionCall("eval", new BinaryExpression(BinaryOperator.Plus,
                                                                                                   new BinaryExpression(BinaryOperator.Plus,
                                                                                                                        new Literal(new String("typeof(")),
                                                                                                                        new VariableRef("__super")),
                                                                                                   new Literal(new String(")")))))));
        }

        /// <summary>
        /// Gets the constructor of the <i>Exception</i> class.
        /// </summary>
        /// <returns>A <see cref="ClassMethod"/></returns>
        private static ClassMethod GetExceptionConstructor()
        {
            var ctorFunc = new Function(new[] { new Parameter("msg") },
                                        new Block(new Assignment(PropertyRef.This("_message"),
                                                                 new VariableRef("msg")),
                                                  new Return()));

            return new ClassMethod("Exception", Scope.Public, Modifier.Default, ctorFunc);
        }

        /// <summary>
        /// Gets the fields of the <i>Exception</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassField"/>s</returns>
        private static IEnumerable<ClassField> GetExceptionFields()
        {
            return new []
                        {
                            new ClassField("_name", Scope.Private, Modifier.Default, new Literal(new String("Exception"))),
                            new ClassField("_message", Scope.Private, Modifier.Default, new Literal(new String(""))),
                            new ClassField("_source", Scope.Private, Modifier.Default, new Literal(new String(""))),
                            new ClassField("_line", Scope.Private, Modifier.Default, new Literal(new Integer(0)))
                        };
        }

        /// <summary>
        /// Gets the properties of the <i>Exception</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassProperty"/>s</returns>
        private static IEnumerable<ClassProperty> GetExceptionProperties()
        {
            return new[]
                       {
                           new ClassProperty("name", Scope.Public, Modifier.Default, "_name", PropertyAccess.Read),
                           new ClassProperty("message", Scope.Public, Modifier.Default, "_message", PropertyAccess.Read),
                           new ClassProperty("source", Scope.Public, Modifier.Default, "_source", PropertyAccess.Read),
                           new ClassProperty("line", Scope.Public, Modifier.Default, "_line", PropertyAccess.Read)
                       };
        }

        /// <summary>
        /// Gets the methods of the <i>Exception</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassMethod"/>s</returns>
        private static IEnumerable<ClassMethod> GetExceptionMethods()
        {
            var toStringFunc = new Function(new[] { new Parameter("format", new String("")) },
                                            Block.Return(PropertyRef.This("name")));
            return new []
                        {
                            new ClassMethod("toString", Scope.Public, Modifier.Default, toStringFunc)
                        };
        }

        /// <summary>
        /// Gets the events of the <i>Exception</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassEvent"/>s</returns>
        private static IEnumerable<ClassEvent> GetExceptionEvents()
        {
            return null;
        }

        /// <summary>
        /// Gets the fields of the <i>TypeInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassField"/>s</returns>
        private static IEnumerable<ClassField> GetTypeInfoFields()
        {
            return new[]
                       {
                           new ClassField("_superType", Scope.Private, Modifier.Default, null),
                           new ClassField("_modifier", Scope.Private, Modifier.Default, null),
                           new ClassField("_name", Scope.Private, Modifier.Default, null),
                           new ClassField("_constructor", Scope.Private, Modifier.Default, null),
                           new ClassField("_fields", Scope.Private, Modifier.Default, null),
                           new ClassField("_properties", Scope.Private, Modifier.Default, null),
                           new ClassField("_methods", Scope.Private, Modifier.Default, null),
                           new ClassField("_events", Scope.Private, Modifier.Default, null)
                       };
        }

        /// <summary>
        /// Gets the properties of the <i>TypeInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassProperty"/>s</returns>
        private static IEnumerable<ClassProperty> GetTypeInfoProperties()
        {
            var getSuperTypeMethod = new ClassMethod(ClassProperty.GetReaderName("superType"), Scope.Public, Modifier.Default, CreateTypeInfoEvaluator("_superType"));

            return new[]
                       {
                           new ClassProperty("superType", Scope.Public, Modifier.Default, getSuperTypeMethod, null),
                           new ClassProperty("modifier", Scope.Public, Modifier.Default, "_modifier", PropertyAccess.Read),
                           new ClassProperty("name", Scope.Public, Modifier.Default, "_name", PropertyAccess.Read),
                           new ClassProperty("constructor", Scope.Public, Modifier.Default, "_constructor", PropertyAccess.Read),
                           new ClassProperty("fields", Scope.Public, Modifier.Default, "_fields", PropertyAccess.Read),
                           new ClassProperty("properties", Scope.Public, Modifier.Default, "_properties", PropertyAccess.Read),
                           new ClassProperty("methods", Scope.Public, Modifier.Default, "_methods", PropertyAccess.Read),
                           new ClassProperty("events", Scope.Public, Modifier.Default, "_events", PropertyAccess.Read)
                       };
        }

        /// <summary>
        /// Gets the methods of the <i>TypeInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassMethod"/>s</returns>
        private static IEnumerable<ClassMethod> GetTypeInfoMethods()
        {
            var toStringFunc = new Function(new[] { new Parameter("format", new String("")) },
                                            Block.Return(PropertyRef.This("name")));
            return new[]
                       {
                           new ClassMethod("toString", Scope.Public, Modifier.Default, toStringFunc)
                       };
        }

        /// <summary>
        /// Gets the events of the <i>TypeInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassEvent"/>s</returns>
        private static IEnumerable<ClassEvent> GetTypeInfoEvents()
        {
            return null;
        }

        /// <summary>
        /// Gets the fields of the <i>MemberInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassField"/>s</returns>
        private static IEnumerable<ClassField> GetMemberInfoFields()
        {
            return new []
                        {
                            new ClassField("_scope", Scope.Private, Modifier.Default, null),
                            new ClassField("_modifier", Scope.Private, Modifier.Default, null),
                            new ClassField("_name", Scope.Private, Modifier.Default, null),
                            new ClassField("_definer", Scope.Private, Modifier.Default, null)
                        };
        }

        /// <summary>
        /// Gets the properties of the <i>MemberInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassProperty"/>s</returns>
        private static IEnumerable<ClassProperty> GetMemberInfoProperties()
        {
            var fullNameFunc = new Function(Parameter.EmptyArray,
                                            Block.Return(new BinaryExpression(BinaryOperator.Plus,
                                                                              new PropertyRef(PropertyRef.This("definer"), "name"),
                                                                              new BinaryExpression(BinaryOperator.Plus,
                                                                                                   new Literal(new String(".")),
                                                                                                   PropertyRef.This("name")))));

            var fullNameGetter = new ClassMethod(ClassProperty.GetReaderName("fullName"), Scope.Public, Modifier.Default, fullNameFunc);
            var definerGetter = new ClassMethod(ClassProperty.GetReaderName("definer"), Scope.Public, Modifier.Default, CreateTypeInfoEvaluator("_definer"));

            return new[]
                        {
                            new ClassProperty("scope", Scope.Public, Modifier.Default, "_scope", PropertyAccess.Read),
                            new ClassProperty("modifier", Scope.Public, Modifier.Default, "_modifier", PropertyAccess.Read),
                            new ClassProperty("name", Scope.Public, Modifier.Default, "_name", PropertyAccess.Read),
                            new ClassProperty("fullName", Scope.Public, Modifier.Default, fullNameGetter, null),
                            new ClassProperty("definer", Scope.Public, Modifier.Default, definerGetter, null)
                        };
        }

        /// <summary>
        /// Gets the methods of the <i>MemberInfo</i> class.
        /// </summary>
        /// <returns><b>null</b></returns>
        private static IEnumerable<ClassMethod> GetMemberInfoMethods()
        {
            return null;
        }

        /// <summary>
        /// Gets the events of the <i>MemberInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassEvent"/>s</returns>
        private static IEnumerable<ClassEvent> GetMemberInfoEvents()
        {
            return null;
        }

        /// <summary>
        /// Gets the fields of the <i>FieldInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassField"/>s</returns>
        private static IEnumerable<ClassField> GetFieldInfoFields()
        {
            return new[]
                       {
                           new ClassField("_sharedValue", Scope.Private, Modifier.Default, null)
                       };
        }

        /// <summary>
        /// Gets the properties of the <i>FieldInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassProperty"/>s</returns>
        private static IEnumerable<ClassProperty> GetFieldInfoProperties()
        {
            return new[]
                       {
                           new ClassProperty("sharedValue", Scope.Public, Modifier.Default, "_sharedValue", PropertyAccess.Read)
                       };
        }


        /// <summary>
        /// Gets the methods of the <i>FieldInfo</i> class.
        /// </summary>
        /// <returns><b>null</b></returns>
        private static IEnumerable<ClassMethod> GetFieldInfoMethods()
        {
            return null;
        }

        /// <summary>
        /// Gets the events of the <i>FieldInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassEvent"/>s</returns>
        private static IEnumerable<ClassEvent> GetFieldInfoEvents()
        {
            return null;
        }

        /// <summary>
        /// Gets the fields of the <i>PropertyInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassField"/>s</returns>
        private static IEnumerable<ClassField> GetPropertyInfoFields()
        {
            return new[]
                       {
                           new ClassField("_reader", Scope.Private, Modifier.Default, null),
                           new ClassField("_writer", Scope.Private, Modifier.Default, null)
                       };
        }

        /// <summary>
        /// Gets the properties of the <i>PropertyInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassProperty"/>s</returns>
        private static IEnumerable<ClassProperty> GetPropertyInfoProperties()
        {
            var canReadFunc = new Function(Parameter.EmptyArray,
                                           Block.Return(new BinaryExpression(BinaryOperator.NotIdentical,
                                                                             PropertyRef.This("_reader"),
                                                                             new Literal())));
            var canReadMethod = new ClassMethod(ClassProperty.GetReaderName("canRead"), Scope.Public, Modifier.Default, canReadFunc);

            var canWriteFunc = new Function(Parameter.EmptyArray,
                                           Block.Return(new BinaryExpression(BinaryOperator.NotIdentical,
                                                                             PropertyRef.This("_writer"),
                                                                             new Literal())));
            var canWriteMethod = new ClassMethod(ClassProperty.GetReaderName("canWrite"), Scope.Public, Modifier.Default, canWriteFunc);

            return new[]
                       {
                           new ClassProperty("reader", Scope.Public, Modifier.Default, "_reader", PropertyAccess.Read),
                           new ClassProperty("writer", Scope.Public, Modifier.Default, "_writer", PropertyAccess.Read),
                           new ClassProperty("canRead", Scope.Public, Modifier.Default, canReadMethod, null),
                           new ClassProperty("canWrite", Scope.Public, Modifier.Default, canWriteMethod, null)
                       };
        }


        /// <summary>
        /// Gets the methods of the <i>PropertyInfo</i> class.
        /// </summary>
        /// <returns><b>null</b></returns>
        private static IEnumerable<ClassMethod> GetPropertyInfoMethods()
        {
            return null;
        }

        /// <summary>
        /// Gets the events of the <i>PropertyInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassEvent"/>s</returns>
        private static IEnumerable<ClassEvent> GetPropertyInfoEvents()
        {
            return null;
        }

        /// <summary>
        /// Gets the fields of the <i>MethodInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassField"/>s</returns>
        private static IEnumerable<ClassField> GetMethodInfoFields()
        {
            return new[]
                       {
                           new ClassField("_parameters", Scope.Private, Modifier.Default, null)
                       };
        }

        /// <summary>
        /// Gets the properties of the <i>MethodInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassProperty"/>s</returns>
        private static IEnumerable<ClassProperty> GetMethodInfoProperties()
        {
            return new[]
                       {
                           new ClassProperty("parameters", Scope.Public, Modifier.Default, "_parameters", PropertyAccess.Read)
                       };
        }

        /// <summary>
        /// Gets the methods of the <i>MethodInfo</i> class.
        /// </summary>
        /// <returns><b>null</b></returns>
        private static IEnumerable<ClassMethod> GetMethodInfoMethods()
        {
            return null;
        }

        /// <summary>
        /// Gets the events of the <i>MethodInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassEvent"/>s</returns>
        private static IEnumerable<ClassEvent> GetMethodInfoEvents()
        {
            return null;
        }

        /// <summary>
        /// Gets the fields of the <i>EventInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassField"/>s</returns>
        private static IEnumerable<ClassField> GetEventInfoFields()
        {
            return new[]
                       {
                           new ClassField("_parameters", Scope.Private, Modifier.Default, null)
                       };
        }

        /// <summary>
        /// Gets the properties of the <i>EventInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassProperty"/>s</returns>
        private static IEnumerable<ClassProperty> GetEventInfoProperties()
        {
            return new[]
                       {
                           new ClassProperty("parameters", Scope.Public, Modifier.Default, "_parameters", PropertyAccess.Read)
                       };
        }

        /// <summary>
        /// Gets the methods of the <i>EventInfo</i> class.
        /// </summary>
        /// <returns><b>null</b></returns>
        private static IEnumerable<ClassMethod> GetEventInfoMethods()
        {
            return null;
        }

        /// <summary>
        /// Gets the events of the <i>EventInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassEvent"/>s</returns>
        private static IEnumerable<ClassEvent> GetEventInfoEvents()
        {
            return null;
        }

        /// <summary>
        /// Gets the fields of the <i>ParameterInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassField"/>s</returns>
        private static IEnumerable<ClassField> GetParameterInfoFields()
        {
            return new[]
                       {
                           new ClassField("_name", Scope.Private, Modifier.Default, null),
                           new ClassField("_byRef", Scope.Private, Modifier.Default, null),
                           new ClassField("_vaArgs", Scope.Private, Modifier.Default, null),
                           new ClassField("_defaultValue", Scope.Private, Modifier.Default, null)
                       };
        }

        /// <summary>
        /// Gets the properties of the <i>ParameterInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassProperty"/>s</returns>
        private static IEnumerable<ClassProperty> GetParameterInfoProperties()
        {
            return new[]
                       {
                           new ClassProperty("name", Scope.Public, Modifier.Default, "_name", PropertyAccess.Read),
                           new ClassProperty("byRef", Scope.Public, Modifier.Default, "_byRef", PropertyAccess.Read),
                           new ClassProperty("vaArgs", Scope.Public, Modifier.Default, "_vaArgs", PropertyAccess.Read),
                           new ClassProperty("defaultValue", Scope.Public, Modifier.Default, "_defaultValue", PropertyAccess.Read)
                       };
        }

        /// <summary>
        /// Gets the methods of the <i>ParameterInfo</i> class.
        /// </summary>
        /// <returns><b>null</b></returns>
        private static IEnumerable<ClassMethod> GetParameterInfoMethods()
        {
            return null;
        }

        /// <summary>
        /// Gets the events of the <i>ParameterInfo</i> class.
        /// </summary>
        /// <returns>An array of <see cref="ClassEvent"/>s</returns>
        private static IEnumerable<ClassEvent> GetParameterInfoEvents()
        {
            return null;
        }

        #endregion

        #region Instance utility methods

        /// <summary>
        /// Gets an attribute declared in this class by its name.
        /// </summary>
        /// <param name="name">The attribute's name</param>
        /// <returns><see cref="Attribute"/></returns>
        public Attribute GetDeclaredAttribute(string name)
        {
            if (Attributes != null)
                foreach (Attribute attribute in Attributes)
                    if (attribute.Name == name)
                        return attribute;

            return null;
        }

        /// <summary>
        /// Gets an attribute by its name.
        /// </summary>
        /// <param name="name">The attribute's name</param>
        /// <returns><see cref="Attribute"/></returns>
        public Attribute GetAttribute(string name)
        {
            Attribute attribute = GetDeclaredAttribute(name);
            if (attribute == null && SuperClass != null)
                attribute = SuperClass.GetAttribute(name);

            return attribute;
        }

        /// <summary>
        /// Gets a collection of all the members declared in a class
        /// </summary>
        /// <param name="kind">The kind of member to get</param>
        /// <returns>A collection of <see cref="ClassMember"/>s</returns>
        public ClassMemberSet<ClassMember> GetDeclaredMembers(MemberKind kind)
        {
            var memberSet = new ClassMemberSet<ClassMember>();

            if ((kind & MemberKind.Constructor) != MemberKind.None)
                memberSet.Add(Constructor);

            if ((kind & MemberKind.Field) != MemberKind.None)
                foreach (ClassField field in Fields)
                    memberSet.Add(field);

            if ((kind & MemberKind.Property) != MemberKind.None)
                foreach (ClassProperty property in Properties)
                    memberSet.Add(property);

            if ((kind & MemberKind.Method) != MemberKind.None)
                foreach (ClassMethod method in Methods)
                    memberSet.Add(method);

            if ((kind & MemberKind.Event) != MemberKind.None)
                foreach (ClassEvent method in Events)
                    memberSet.Add(method);

            return memberSet;
        }

        /// <summary>
        /// Gets a collection of all the members declared in a class
        /// </summary>
        /// <returns>A collection of <see cref="ClassMember"/>s</returns>
        public ClassMemberSet<ClassMember> GetDeclaredMembers()
        {
            return GetDeclaredMembers(MemberKind.All);
        }

        /// <summary>
        /// Gets a collection of all the members of a class
        /// </summary>
        /// <param name="kind">The kind of member to get</param>
        /// <returns>A collection of <see cref="ClassMember"/>s</returns>
        public ClassMemberSet<ClassMember> GetMembers(MemberKind kind)
        {
            var memberSet = new ClassMemberSet<ClassMember>();
            if ((kind & MemberKind.Constructor) != MemberKind.None)
                memberSet.Add(Constructor);

            MemberKind noCtor = kind & ~MemberKind.Constructor;
            Class klass = this;

            while (klass != null)
            {
                var members = klass.GetDeclaredMembers(noCtor);

                foreach (ClassMember member in members)
                    if (!memberSet.Contains(member.Name))
                        memberSet.Add(member);

                klass = klass.SuperClass;
            }

            return memberSet;
        }

        /// <summary>
        /// Gets a collection of all the members of a class
        /// </summary>
        /// <returns>A collection of <see cref="ClassMember"/>s</returns>
        public ClassMemberSet<ClassMember> GetMembers()
        {
            return GetMembers(MemberKind.All);
        }

        /// <summary>
        /// Finds a member declared in this class with the given name.
        /// </summary>
        /// <param name="memberName">The name of the member to find</param>
        /// <param name="kind">The kind of member to get</param>
        /// <returns>A <see cref="ClassMember"/></returns>
        public ClassMember GetDeclaredMember(string memberName, MemberKind kind)
        {
            if (((kind & MemberKind.Constructor) != MemberKind.None) &&
                (memberName == Name))
                return Constructor;

            if (((kind & MemberKind.Field) != MemberKind.None) &&
                Fields.Contains(memberName))
                return Fields[memberName];

            if (((kind & MemberKind.Property) != MemberKind.None) &&
                Properties.Contains(memberName))
                return Properties[memberName];

            if (((kind & MemberKind.Method) != MemberKind.None) &&
                Methods.Contains(memberName))
                return Methods[memberName];

            if (((kind & MemberKind.Event) != MemberKind.None) &&
                Events.Contains(memberName))
                return Events[memberName];

            return null;
        }

        /// <summary>
        /// Gets a member by its name.
        /// </summary>
        /// <param name="memberName">The name of the member to find</param>
        /// <param name="kind">The kind of member to get</param>
        /// <returns>A <see cref="ClassMember"/></returns>
        public ClassMember GetMember(string memberName, MemberKind kind)
        {
            ClassMember member = GetDeclaredMember(memberName, kind);
            if (member == null && SuperClass != null)
                member = SuperClass.GetMember(memberName, kind);

            return member;
        }

        /// <summary>
        /// Gets a member by its name.
        /// </summary>
        /// <param name="memberName">The name of the member to find</param>
        /// <returns>A <see cref="ClassMember"/></returns>
        public ClassMember GetMember(string memberName)
        {
            return GetMember(memberName, MemberKind.All);
        }

        /// <summary>
        /// Finds the field that has the given name.
        /// </summary>
        /// <param name="fieldName">The name of the field to find</param>
        /// <returns><see cref="ClassField"/></returns>
        public ClassField GetField(string fieldName)
        {
            Class klass = this;

            while (!(klass == null || klass.Fields.Contains(fieldName)))
                klass = klass.SuperClass;

            return klass == null ? null : klass.Fields[fieldName];
        }

        /// <summary>
        /// Finds the property that has the given name.
        /// </summary>
        /// <param name="propertyName">The name of the property to find</param>
        /// <returns><see cref="ClassProperty"/></returns>
        public ClassProperty GetProperty(string propertyName)
        {
            Class klass = this;

            while (!(klass == null || klass.Properties.Contains(propertyName)))
                klass = klass.SuperClass;

            return klass == null ? null : klass.Properties[propertyName];
        }

        /// <summary>
        /// Finds the method that has the given name.
        /// </summary>
        /// <param name="methodName">The name of the method to find</param>
        /// <returns><see cref="ClassMethod"/></returns>
        public ClassMethod GetMethod(string methodName)
        {
            Class klass = this;

            while (!(klass == null || klass.Methods.Contains(methodName)))
                klass = klass.SuperClass;

            return klass == null ? null : klass.Methods[methodName];
        }

        /// <summary>
        /// Finds the method that has the given name.
        /// </summary>
        /// <param name="eventName">The name of the method to find</param>
        /// <returns><see cref="ClassEvent"/></returns>
        public ClassEvent GetEvent(string eventName)
        {
            Class klass = this;

            while (!(klass == null || klass.Events.Contains(eventName)))
                klass = klass.SuperClass;

            return klass == null ? null : klass.Events[eventName];
        }

        /// <summary>
        /// Determines if this class is a subclass of <param name="other"/>
        /// </summary>
        /// <param name="other">The supposed superclass</param>
        /// <returns>A boolean</returns>
        public bool Inherits(Class other)
        {
            Class klass = SuperClass;

            while (klass != null && klass != other)
                klass = klass.SuperClass;

            return klass == other;
        }

        /// <summary>
        /// Registers the constructor of a class.
        /// </summary>
        /// <param name="constructor">The provided constructor</param>
        public void RegisterConstructor(ClassMethod constructor)
        {
            Constructor = constructor ?? CreateDefaultConstructor(Name, Scope.Public);
            Constructor.Definer = this;
        }

        /// <summary>
        /// Registers a field in the class.
        /// </summary>
        /// <param name="field">The provided field</param>
        public void RegisterField(ClassField field)
        {
            Fields.Add(field);
            field.Definer = this;
        }

        /// <summary>
        /// Registers the fields of a class.
        /// </summary>
        /// <param name="fields">The provided set of fields</param>
        public void RegisterFields(IEnumerable<ClassField> fields)
        {
            if (fields != null)
                foreach (ClassField field in fields)
                    RegisterField(field);
        }

        /// <summary>
        /// Registers a property in the class.
        /// </summary>
        /// <param name="property">The provided property</param>
        public void RegisterProperty(ClassProperty property)
        {
            Properties.Add(property);
            property.Definer = this;

            if (!string.IsNullOrEmpty(property.BackingFieldName))
                property.GenerateAccessors();

            if (property.CanRead)
                RegisterMethod(property.Reader);

            if (property.CanWrite)
                RegisterMethod(property.Writer);

            if (property.IsAuto)
            {
                var bfm = property.Modifier == Modifier.Static ? Modifier.Static : Modifier.Default;
                var backingField = new ClassField(property.BackingFieldName, Scope.Private, bfm, null);
                RegisterField(backingField);
            }
        }

        /// <summary>
        /// Registers the properties of a class.
        /// </summary>
        /// <param name="properties">The provided set of properties</param>
        public void RegisterProperties(IEnumerable<ClassProperty> properties)
        {
            if (properties != null)
                foreach (ClassProperty property in properties)
                    RegisterProperty(property);
        }

        /// <summary>
        /// Registers a method in the class.
        /// </summary>
        /// <param name="method">The provided method</param>
        public void RegisterMethod(ClassMethod method)
        {
            Methods.Add(method);
            method.Definer = this;
        }

        /// <summary>
        /// Registers the methods of a class.
        /// </summary>
        /// <param name="methods">The provided set of methods</param>
        public void RegisterMethods(IEnumerable<ClassMethod> methods)
        {
            if (methods != null)
                foreach (ClassMethod method in methods)
                    RegisterMethod(method);
        }

        /// <summary>
        /// Registers an event in the class.
        /// </summary>
        /// <param name="_event">The provided event</param>
        public void RegisterEvent(ClassEvent _event)
        {
            Events.Add(_event);
            _event.Definer = this;

            RegisterField(_event.CreateHandlerSetField());
            RegisterMethod(_event.CreateAddHandlerMethod());
            RegisterMethod(_event.CreateRemoveHandlerMethod());
            RegisterMethod(_event.CreateTriggerEventMethod());
        }

        /// <summary>
        /// Registers the events of a class.
        /// </summary>
        /// <param name="events">The provided set of events</param>
        public void RegisterEvents(IEnumerable<ClassEvent> events)
        {
            if (events != null)
                foreach (ClassEvent _event in events)
                    RegisterEvent(_event);
        }

        /// <summary>
        /// Generates a <i>type</i> property for reflection support.
        /// </summary>
        private void GenerateReflector()
        {
            var typeFunc = new Function(Parameter.EmptyArray, Block.Return(new TypeOfExpression(Name)));
            var typeReader = new ClassMethod(ClassProperty.GetReaderName("type"), Scope.Public, Modifier.Final, typeFunc);
            var typeProperty = new ClassProperty("type", Scope.Public, Modifier.Final, typeReader, null);

            RegisterProperty(typeProperty);
        }

        #endregion
    }
}