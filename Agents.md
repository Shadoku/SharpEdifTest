Developing Clickteam Fusion 2.5 extensions with SharpEDIF
This document summarises the architecture of Clickteam Fusion 2.5, explains how actions, conditions and expressions work, and describes important data structures and low‑level details relevant to developing extensions using the SharpEDIF SDK (a C# SDK for writing Fusion 2.5 extensions).

1 Fusion 2.5 event system and object selection
1.1 Event lines, conditions, actions and object selection
Events in Clickteam Fusion 2.5 are rows in the Event Editor. Each event contains one or more conditions and a list of actions. Conditions are evaluated from left to right; if all conditions in an event evaluate to true, Fusion executes the actions in that event.

Developers create gameplay by adding events; each event row is associated with particular object types (e.g., a “Player” active object). An event can include multiple object types or qualifiers.

Object selection (scoping) determines which instances of an object will run the actions. Initially all instances of each object type are selected. When conditions are present, instances are filtered: only those instances satisfying every condition remain selected and execute the actions. Bartek Biszkont explains that Fusion keeps separate linked lists to manage object selection: one list contains all instances of an object type, and a second list contains only the selected instances
bartekbiszkont.myportfolio.com
. Conditions modify the selected‑objects list but must not modify the underlying “all objects” list
bartekbiszkont.myportfolio.com
.

Qualifiers are groups of object types; internally they are linked lists of object types
bartekbiszkont.myportfolio.com
. When a qualifier is used in a condition or action, Fusion iterates through each object type in the qualifier and applies the condition or action to each selected instance.

Actions execute on the selected instances. For example, if a condition filters on the A value of an object, only objects whose A value matches remain selected and will perform the subsequent actions
bartekbiszkont.myportfolio.com
.

1.2 Immediate (triggered) conditions and event loop
Most conditions are tested conditions—they are evaluated once per event loop (roughly once per frame). Examples include comparisons (Value = 1) or collisions.

Immediate conditions (sometimes called triggered conditions) occur only at the moment the specified event happens. Examples include On Mouse Click or On Timer Event. They do not repeat each frame; instead, they immediately push an event into the event queue. In an event containing an immediate condition, the condition must be at the start of the event; mixing triggered and non‑triggered conditions can lead to unexpected behaviour.

Fusion processes events in order, performing short‑circuit evaluation; if a condition is false, later conditions and actions in that event are skipped. This short‑circuit evaluation improves performance (especially with many conditions).

1.3 Objects, movement and properties
Each object has a properties panel containing general settings, movement settings and values/flags. These properties can be exposed through extensions. Fusion 2.5 includes a range of built‑in movement types (eight‑direction, platform, path, etc.) and value/flag storage for up to 26 alterable values and 10 flags.

The edit time representation of an object is referred to as EditData, and the runtime representation is referred to as RunData. An extension’s run‑time data contains instance‑specific state (e.g., an internal string or counter). Extensions can expose properties through the properties panel using the Fusion property system described later.

2 Overview of SharpEDIF
SharpEDIF is a C# extension SDK for Clickteam Fusion 2.5 created by Kostya. It allows developers to write fully‑operational Fusion extensions in C# rather than C/C++. The README explains that SharpEDIF makes it easy to create an extension with little knowledge of the underlying EDIF SDK
github.com
. Key points:

Dependencies: SharpEDIF requires the .NET 4.7.2 development kit, and the SharpEDIF Builder project requires the .NET 7.0 SDK
github.com
.

Project set‑up: Clone the repository into Visual Studio 2022. Modify the example Extension.cs file to define the extension’s metadata (name, author, copyright, description and web site) and define actions, conditions and expressions.

Compiling: Build both the SharpEdif.Builder and SharpEdif projects (Control + Shift + B). After building, copy the .mfx file from the CompiledExtension folder into Fusion’s Extensions/Unicode folder. For the runtime version, change the build type to Runtime and copy the compiled file into Fusion’s Data/Runtime/Unicode folder
github.com
.

Releasing: To distribute the extension, create a folder with the required structure (as shown in the README) containing the .mfx for the editor and the runtime; zip and share this folder
github.com
.

SharpEDIF hides much of the complexity of EDIF by using C# attributes to register actions, conditions and expressions. It also exposes Fusion structures and functions through interop, enabling your C# code to interact with the Fusion runtime.

3 Actions, conditions and expressions (ACE) in SharpEDIF
In Fusion, an extension provides Actions, Conditions, and Expressions (often called ACE). SharpEDIF uses C# attributes to declare these. The attributes carry the names shown in the Event Editor and specify parameter names.

3.1 Actions
Role: Actions are commands executed when the conditions in an event line evaluate to true. An action has no return value but can modify the extension’s runtime state, create objects, play sounds, etc.

Declaration: Use the [Action] attribute on a static method. The attribute takes a menu name, editor name and optionally an array of parameter names. For example:

csharp
Copy
Edit
[Action("Set string", "Set string to %0", new[]{ "The parameter name", "Second parameter name" })]
public static void ActionExample1(LPRDATA* rdPtr, string exampleString, string anotherParam)
{
    rdPtr->runData.ExampleString = exampleString;
}
The LPRDATA* parameter points to the extension’s run‑time data; you can modify runData to store per‑instance state
raw.githubusercontent.com
. Additional parameters correspond to the user‑specified values in the Event Editor.

Callbacks: Actions are registered at runtime by SharpEDIF. They are associated with a callback delegate of type ActionCallback which returns a short (0 to continue event processing, non‑zero to interrupt). The SDK automatically handles calling your method when the action is executed.

Parameter access: SharpEDIF automatically marshals parameters to C# types. For actions receiving object references or complex parameter types, you can call SDK.CNC_GetParameter(rdPtr) and related methods (described later) to manually retrieve parameters.

3.2 Conditions
Role: Conditions are boolean tests. Fusion evaluates conditions to decide whether to execute actions. A condition must return true or false (in SharpEDIF it returns bool or an integer 0/1).

Declaration: Use the [Condition] attribute on a static method. The attribute takes a menu name, an editor name and optionally parameter names. Example:

csharp
Copy
Edit
[Condition("String is equal to", "String is equal to %0", new[]{ "String to compare" })]
public static bool ConditionExample1(LPRDATA* rdPtr, string testString)
{
    return rdPtr->runData.ExampleString == testString;
}
raw.githubusercontent.com

Immediate vs normal conditions: Immediate conditions (triggered conditions) are marked by Fusion (green in the Event Editor) and only fire at specific events (e.g., a timer tick or mouse click). Normal conditions are evaluated once per event loop. SharpEDIF registers both types of conditions; however, the SDK’s low‑level event flags must be set appropriately when exposing new immediate conditions (this functionality is part of SharpEDIF’s to‑do list
github.com
).

Parameter types: Conditions can accept parameters of various types. SharpEDIF automatically converts primitive types; for other types use the SDK.CNC_GetIntParameter / CNC_GetStringParameter functions.

3.3 Expressions
Role: Expressions evaluate to a value (integer, float or string) and are used in the Expression Editor. They can return internal state or calculated results.

Declaration: Use the [Expression] attribute on a static method. Example:

csharp
Copy
Edit
[Expression("Get string", "GetStr$(")]
public static string ExpressionExample1(LPRDATA* rdPtr, string test)
{
    return test;
}
raw.githubusercontent.com

Return types: A string expression must call SDK.ReturnString(rdPtr) before returning, and a float expression should call SDK.ReturnFloat(rdPtr) to inform Fusion of the correct return type. An integer expression returns by default. The enumerated type ExpReturnType defines valid return types (Int, String, Double)
raw.githubusercontent.com
.

Parameters for expressions: Expressions can take parameters; they are retrieved using SDK.CNC_GetFirstExpressionParameter(rdPtr, index, type) and SDK.CNC_GetNextExpressionParameter. There are helper methods for retrieving ints, floats or strings (CNC_GetFirstExpressionParameterInt, CNC_GetFirstExpressionParameterFloat, etc.)
raw.githubusercontent.com
.

3.4 Parameter types (general)
SharpEDIF defines a ParamType enumeration to specify parameter kinds: integers, strings, objects, directions, colours and many others
raw.githubusercontent.com
. When building an extension you typically rely on the SDK’s automatic parameter marshaling, but understanding these parameter types helps when retrieving raw parameters.

The ExpParamType enumeration classifies parameters passed to expressions (Int, Float, String, AltValue, Flag)
raw.githubusercontent.com
.

4 Important data structures
SharpEDIF mirrors many of Fusion’s internal C structures in C#. Understanding these structures helps when interacting with the runtime.

4.1 Run‑time and edit‑time structures
LPEDATA – the edit data structure passed to CreateRunObject at runtime. It holds the extension’s edit‑time header (extHeader) and a pointer to user data. In SharpEDIF it exposes a props property of type FusionProperties for extension properties
raw.githubusercontent.com
.

LPRDATA – the run data structure passed to actions, conditions and expressions. It contains a headerObject (rHo) describing the object instance and a _userData pointer to the user‑defined RunData class. SharpEDIF exposes a runData property to access your managed RunData instance
raw.githubusercontent.com
.

RunData / EditData – user‑defined C# classes (in UserStructs.cs). RunData stores per‑instance state such as strings, counters or pointers. The example sets an ExampleString field
raw.githubusercontent.com
. You can add additional fields, properties or methods. EditData currently holds a FusionProperties instance for storing properties presented in the Fusion properties panel
raw.githubusercontent.com
.

extHeader and kpxRunInfos – structures describing the extension in the compiled .mfx. extHeader contains the extension’s size, version and ID; kpxRunInfos holds pointers to arrays of condition, action and expression info and the counts of each
raw.githubusercontent.com
.

headerObject – part of LPRDATA, contains handles and state for the object (including position, size, animations, flags and references to the application and frame). It links to runHeader and runHeader4, which provide access to global application state such as the event queue, random seed, display coordinates and function pointers for runtime operations
raw.githubusercontent.com
.

4.2 Property types and the properties panel
Fusion’s properties panel supports various control types for extension properties. The PropType enumeration lists the available types, such as static text, folders, edit boxes for strings and numbers, combo boxes, colour pickers, font dialogs and custom property controls
raw.githubusercontent.com
. SharpEDIF uses these when building the property array for the editor. For instance, the example FillProperties method creates two properties—a static text and an editable string—using FusionProp.CreateStatic and FusionProp.CreateEditString
raw.githubusercontent.com
. FusionProperties.ObtainData() allocates a native array of property descriptors that Fusion reads
raw.githubusercontent.com
.

5 Low‑level runtime functions
SharpEDIF exposes wrappers around Fusion’s internal functions through the SDK class. These allow you to retrieve parameters, call runtime routines and signal return types.

Parameter retrieval – Use CNC_GetIntParameter, CNC_GetFloatParameter, CNC_GetStringParameter or the generic CNC_GetParameter to obtain action and condition parameters
raw.githubusercontent.com
. For expressions, use CNC_GetFirstExpressionParameterInt/Float/String and CNC_GetNextExpressionParameter… to iterate through parameters
raw.githubusercontent.com
.

Calling runtime functions – The method CallRuntimeFunction(rdPtr, index, wParam, lParam) calls internal functions pointed to by rh4KpxFunctions in runHeader4
raw.githubusercontent.com
. Index values correspond to functions like retrieving expression parameters or playing sounds. The API file (API.cs) contains constants and wrappers for many runtime operations (e.g., drawing, playing samples, changing animations) — developers can inspect this file for more details.

Return types – When returning a string or floating‑point value from an expression, call ReturnString(rdPtr) or ReturnFloat(rdPtr) before returning
raw.githubusercontent.com
.

Memory management – SharpEDIF functions such as AllocBytes, GetStringPtr, PtrToString and StringToPtr allocate and convert unmanaged memory. Use these carefully to avoid leaks.

6 Extension development workflow
Plan your extension’s capabilities. Decide which actions, conditions and expressions are required. Identify what per‑instance state needs to be stored in RunData.

Define EditData and RunData. In UserStructs.cs, add fields/properties to RunData for runtime state and to EditData if you need edit‑time data.

Declare ACE methods. In Extension.cs, add static methods for each action (void), condition (bool) and expression (returning int, float or string). Decorate each method with the appropriate [Action], [Condition] or [Expression] attribute. Provide meaningful menu names, editor names and parameter names to make the event editor intuitive.

Initialize runtime state. Override CreateRunObject in UserMethods.cs to initialize fields of RunData when an object instance is created
raw.githubusercontent.com
. Use FillProperties to populate your extension’s properties panel
raw.githubusercontent.com
.

Compile and test. Build both the builder and extension projects. Copy the .mfx into the appropriate Fusion directories and test in Clickteam Fusion. When debugging, you can call SharpEdif.AllocConsole() to open a console for logging.

Release. Compile both the editor and runtime versions of the extension and distribute them with the correct folder structure
github.com
.

7 Best practices
Keep actions side‑effect‑free where possible. Actions should operate only on the selected objects and should not depend on global state unless necessary. Avoid actions that produce unexpected side effects outside of their object instances.

Use conditions efficiently. Because conditions are evaluated every event loop, write simple and inexpensive tests. Use immediate conditions for events triggered by user input or timers to avoid per‑frame checks.

Be mindful of object selection. Understand that conditions filter the selected instances; subsequent actions only apply to the filtered list
bartekbiszkont.myportfolio.com
. If you need to operate on all instances regardless of previous conditions, insert a “Pick all” action (available from built‑in objects or qualifiers) or structure events accordingly.

Support qualifiers. Qualifiers allow your extension to operate on groups of objects. When retrieving parameters, handle object and qualifier references (via ParamType.Object and ParamType.Group) appropriately.

Handle memory carefully. When allocating unmanaged memory (e.g., for strings or byte arrays), ensure you free it when no longer needed. Use Marshal.FreeHGlobal when appropriate, or rely on Fusion to free pointers you obtained through its runtime functions.

Avoid modifying internal lists. Never modify the grey “all objects” list described by Biszkont; only modify the selected‑objects list. Modifying the wrong list can corrupt the runtime
bartekbiszkont.myportfolio.com
.

Test both editor and runtime. Some bugs surface only in the runtime version. Always compile and test both versions before releasing your extension.

8 Summary
Clickteam Fusion 2.5 uses an event‑driven system where events consist of conditions and actions. Conditions filter object instances and decide when to execute actions; expressions compute values used in conditions, actions and the expression editor. Object selection is central to Fusion’s logic—only selected instances execute the actions for an event
bartekbiszkont.myportfolio.com
. Internally, Fusion manages linked lists of objects, qualifiers and selected instances
bartekbiszkont.myportfolio.com
.

SharpEDIF is a C# SDK for writing Fusion 2.5 extensions. It wraps complex C/C++ structures (such as LPRDATA, LPEDATA, headerObject and kpxRunInfos) in managed code, provides enumerations for property and parameter types, and supplies functions for interacting with Fusion’s runtime (retrieving parameters, setting return types and calling runtime functions). Extensions define actions, conditions and expressions using attributes and static methods, store per‑instance state in a RunData class, and populate the properties panel using FusionProp and FusionProperties. The SDK simplifies extension development while still exposing low‑level capabilities for advanced use.
