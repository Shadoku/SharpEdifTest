﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;


namespace SharpEdif.Builder.ILStuff;
using static TypesAndMethods;
public class ConditionGenerator
{
    public static MethodDef MakeCondition(MethodDef met,ModuleDef mod)
    {
        Console.WriteLine($"Generating wrapper for condition \"{met.Name}\"");

        var wrapper = new MethodDefUser($"{met.Name}_wrapper",
            MethodSig.CreateStatic(intType, lprdata, intType, intType),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Public | MethodAttributes.Static);
        wrapper.DeclaringType = met.DeclaringType;
        wrapper.Body = new CilBody();
        // allow dnlib to calculate max stack
        wrapper.Body.KeepOldMaxStack = false;
        var insts = wrapper.Body.Instructions;

        int currentIndex = 0;
        Local[] paramLocals = new Local[met.Parameters.Count];
        foreach (var param in met.Parameters)
        {
            if (param.Type == intType)
            {
                var local = new Local(intType);
                insts.Add(new Instruction(OpCodes.Ldarg_0));
                insts.Add(new Instruction(OpCodes.Call, cncGetParamInt));
                insts.Add(new Instruction(OpCodes.Stloc, local));
                paramLocals[currentIndex] = local;
            }

            if (param.Type == floatType)
            {
                var local = new Local(floatType);
                insts.Add(new Instruction(OpCodes.Ldarg_0));
                insts.Add(new Instruction(OpCodes.Call, cncGetParamFloat));
                insts.Add(new Instruction(OpCodes.Stloc, local));
                paramLocals[currentIndex] = local;
            }

            if (param.Type == stringType)
            {
                var local = new Local(stringType);
                insts.Add(new Instruction(OpCodes.Ldarg_0));
                insts.Add(new Instruction(OpCodes.Call, cncGetParamString));
                insts.Add(new Instruction(OpCodes.Stloc, local));
                paramLocals[currentIndex] = local;
            }

            currentIndex++;
        }

        var index2 = 0;
        for (int j = 0; j < currentIndex; j++)
        {

            if (index2 == 0 && paramLocals[0] == null)
            {
                insts.Add(new Instruction(OpCodes.Ldarg_0));

            }
            else
            {
                var local = paramLocals[index2];
                wrapper.Body.Variables.Add(local);
                insts.Add(new Instruction(OpCodes.Ldloc, local));
            }

            index2++;
        }

        insts.Add(new Instruction(OpCodes.Call, met));
        insts.Add(new Instruction(OpCodes.Ret));
        return wrapper;
    }

    public static MethodDef MakeConditionGetter(MethodDef met,ModuleDef mod)
    {
        var infosField = new FieldDefUser($"{met.Name}_infos",new FieldSig(new PtrSig(shortType)),FieldAttributes.Public| FieldAttributes.Static);
        infosField.IsStatic = true;
        infosField.DeclaringType = met.DeclaringType;
        var wrapper = new MethodDefUser($"{met.Name}_makeInfos",
            MethodSig.CreateStatic(voidType),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Public | MethodAttributes.Static );
        wrapper.DeclaringType = met.DeclaringType;
        wrapper.Body = new CilBody();
        wrapper.Body.KeepOldMaxStack = false;
        var insts = wrapper.Body.Instructions;
        int sizeToAllocate = 6;
        List<string> parameterTypes = new List<string>();
        foreach (var param in met.Parameters)
        {
            if (param.Type.FullName != lprdata.FullName)
            {
                sizeToAllocate += 4;
                parameterTypes.Add(param.Type.FullName);
            }
        }
        
        var allochGlobal = sdk.FindMethod("AllocBytes");
        var firstPointer = new Local(new PtrSig(shortType));
        wrapper.Body.Variables.Add(firstPointer);
        
        insts.Add(new Instruction(OpCodes.Ldc_I4,sizeToAllocate));
        insts.Add(new Instruction(OpCodes.Call,allochGlobal));
        insts.Add(new Instruction(OpCodes.Stsfld,infosField));
        insts.Add(new Instruction(OpCodes.Ldsfld,infosField));

        insts.putElementInArrayShort(0,Program.currentConditionCode);
        insts.putElementInArrayShort(1,0x20);
        insts.putElementInArrayShort(2,parameterTypes.Count,parameterTypes.Count==0);
        for (int i = 0; i < parameterTypes.Count; i++)
        {
            insts.putElementInArrayShort(3+i,Utils.GetParamFromType(parameterTypes[i]));
            insts.putElementInArrayShort(parameterTypes.Count+3+i,0,i==parameterTypes.Count-1);
        }
        
        insts.Add(new Instruction(OpCodes.Ret));
        
        
        return wrapper;
    }

}