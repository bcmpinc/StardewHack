﻿using Harmony;
using StardewModdingAPI;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace StardewHack
{
    internal delegate IEnumerable<CodeInstruction> TranspilerSignature(IEnumerable<CodeInstruction> instructions);

    public abstract class Hack : Mod
    {
        /** The harmony instance used for patching. */
        public HarmonyInstance harmony { get; private set; }

        /** The code that is being patched. 
         * Use only within methods annotated with BytecodePatch. 
         */
        public List<CodeInstruction> codes { get; private set; }

        /** Provides simpliied API's for writing mods. */
        public IModHelper helper { get; private set; }

        #pragma warning disable 414
        /** Reference used by dynamic proxy static methods. */
        private static Hack instance;
        #pragma warning restore 414

        /** Applies the methods annotated with BytecodePatch defined in this class. */
        public override void Entry(IModHelper helper) {
            this.helper = helper;
            Hack.instance = this;
            var prepare = AccessTools.Method(typeof(Hack), "Prepare");
            var instance = AccessTools.Field(typeof(Hack), "instance");
            var codes = AccessTools.Property(typeof(Hack), "codes");

            // Use the Mod's UniqueID to create the harmony instance.
            string UniqueID = helper.ModRegistry.ModID;
            Monitor.Log($"Applying bytecode patches for {UniqueID}.", LogLevel.Info);
            this.harmony = HarmonyInstance.Create(UniqueID);

            // Iterate all methods in this class and search for those that have a BytecodePatch annotation.
            var methods = this.GetType().GetMethods(AccessTools.all);
            foreach (MethodInfo patch in methods) {
                var bytecode_patches = patch.GetCustomAttributes<BytecodePatch>();
                foreach (var bp in bytecode_patches) {
                    // Apply the patch to the method specified in the annotation.
                    MethodInfo method = bp.GetMethod();
                    //Monitor.Log($"Create Patch {patch.Name} for {method}.");

                    // Create patch proxy static method
                    DynamicMethod proxy = new DynamicMethod(
                        $"proxy<{patch.Name}> for {method}", 
                        typeof(IEnumerable<CodeInstruction>), 
                        new Type[]{typeof(IEnumerable<CodeInstruction>)},
                        typeof(Hack),
                        true
                    );
                    ILGenerator il = proxy.GetILGenerator();
                    // hack.Prepare(instructions, info);
                    il.Emit(OpCodes.Ldsfld, instance);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, $"Applying patch {patch.Name} to {method}.");
                    il.Emit(OpCodes.Callvirt, prepare);
                    // hack.{patch}();
                    il.Emit(OpCodes.Ldsfld, instance);
                    il.Emit(OpCodes.Callvirt, patch);
                    // return hack.codes;
                    il.Emit(OpCodes.Ldsfld, instance);
                    il.Emit(OpCodes.Callvirt, codes.GetGetMethod());
                    il.Emit(OpCodes.Ret);

                    // Register patch.
                    harmony.Patch(method, null, null, new HarmonyMethod(proxy.CreateDelegate(typeof(TranspilerSignature)).Method));
                }
            }
        }

        /** Called from dynamic proxy method to prepare for patching. */ 
        private void Prepare(IEnumerable<CodeInstruction> instructions, string info) {
            codes = new List<CodeInstruction>(instructions);
            Monitor.Log(info);
        }

        /** Find the first occurance of the given sequence of instructions that follows this range.
         * The performed matching depends on the type:
         *  - String: is it contained in the string representation of the instruction
         *  - MemberReference (including MethodDefinition): is the instruction's operand equal to this reference.
         *  - OpCode: is this the instruction's OpCode.
         *  - CodeInstruction: are the instruction's OpCode and Operand equal.
         *  - null: always matches.
         */
        public InstructionRange FindCode(params Object[] contains) {
            return new InstructionRange(codes, contains);
        }

        /** Find the last occurance of the given sequence of instructions that follows this range.
         * See InstructionHelpers.Find() for how the matching is performed.
         */
        public InstructionRange FindCodeLast(params Object[] contains) {
            return new InstructionRange(codes, contains, codes.Count, -1);
        }
    }
}

