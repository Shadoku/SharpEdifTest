using System;
using System.Runtime.InteropServices;
using SharpEdif;

namespace SharpEdif.User
{
    public unsafe class Extension
    {
        public const string ExtensionName = "My new C# extension";
        public const string ExtensionAuthor = "The extension's author";
        public const string ExtensionCopyright = "Copyright © 2023 Extension Author";
        public const string ExtensionComment = "Quick description displayed in the Insert Object dialog box and in the object's About properties.";
        public const string ExtensionHttp = "http://www.authorswebpage.com";

        private const int ConditionEventCode = 1;

        [Action("Set string", "Set string to %0",new []{"The parameter name", "Second parameter name"})]
        public static void ActionExample1(LPRDATA* rdPtr, string exampleString, string anotherParam)
        {
            rdPtr->runData.ExampleString = exampleString;
        }

        [Action("Trigger event", "Trigger event")]
        public static void TriggerEvent(LPRDATA* rdPtr)
        {
            SDK.GenerateEvent(rdPtr, ConditionEventCode);
        }
 
        [Condition("String is equal to", "String is equal to %0", new[]{"String to compare"})]
        public static bool ConditionExample1(LPRDATA* rdPtr,string testString)
        {
            return rdPtr->runData.ExampleString == testString;
        }

        [Condition("On event", "On event")]
        public static bool OnEvent(LPRDATA* rdPtr)
        {
            return true;
        }

        [Expression("Get string", "GetStr$(", new[]{"String to get"})]
        public static string ExpressionExample1(LPRDATA* rdPtr, string test)
        {
            return test;
        }
    }
}